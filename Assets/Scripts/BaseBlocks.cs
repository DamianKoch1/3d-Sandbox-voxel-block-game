using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class Block
{
    public Block(Vector3 pos)
    {
        Pos = pos;
    }

    public Vector3 Pos { get; private set; }

    public BlockType Type { get; protected set; }

    public virtual bool CanPlaceInEntity => true;

    protected virtual float TickInterval => 0.5f;

    public virtual float BlastResistance => 0.3f;

    protected async Task<bool> Tick()
    {
        await Task.Delay((int)(TickInterval * 1000));
        if (destroyed) return false;
        //don't tick if exiting playmode
        return !ThreadingUtils.QuitToken.IsCancellationRequested;
    }

    private bool destroyed;


    /// <summary>
    /// Should a face be drawn when given block is next to it?
    /// </summary>
    /// <param name="neighbour">neighbour block</param>
    /// <returns></returns>
    public abstract bool DrawFaceNextTo(Direction dir, Block neighbour);

    public virtual Vector3[] GetVertices(Direction dir, Block neighbour)
    {
        return BlockDictionary.GetDefaultVertices(dir);
    }

    public Vector2[] GetUVs(Direction dir) => BlockDictionary.GetBlockUVs(Type, dir);

    /// <summary>
    /// Called when block is placed
    /// </summary>
    public virtual void OnPlaced()
    {
        OnBlockUpdate();
        UpdateNeighbours();
    }

    /// <summary>
    /// Called before block is destroyed
    /// </summary>
    public virtual void OnDestroyed()
    {
        UpdateNeighbours();
        destroyed = true;
    }

    /// <summary>
    /// Usually called when placed / adjacent block is placed / destroyed
    /// </summary>
    public virtual void OnBlockUpdate()
    { }

    public List<Block> GetNeighbours()
    {
        List<Block> neighbours = new List<Block>();
        neighbours.Add(TerrainGenerator.Instance.GetBlock(Pos + Vector3.left));
        neighbours.Add(TerrainGenerator.Instance.GetBlock(Pos + Vector3.right));
        neighbours.Add(TerrainGenerator.Instance.GetBlock(Pos + Vector3.up));
        neighbours.Add(TerrainGenerator.Instance.GetBlock(Pos + Vector3.down));
        neighbours.Add(TerrainGenerator.Instance.GetBlock(Pos + Vector3.forward));
        neighbours.Add(TerrainGenerator.Instance.GetBlock(Pos - Vector3.forward));
        return neighbours;
    }

    public void UpdateNeighbours()
    {
        foreach (var block in GetNeighbours())
        {
            block?.OnBlockUpdate();
        }
    }
}

public abstract class BlockOpaque : Block
{
    public BlockOpaque(Vector3 pos) : base(pos)
    { }

    public override bool DrawFaceNextTo(Direction dir, Block neighbour)
    {
        return !(neighbour is BlockOpaque);
    }
}

public abstract class BlockTransparent : Block
{
    public BlockTransparent(Vector3 pos) : base(pos)
    { }

    public override bool DrawFaceNextTo(Direction dir, Block neighbour)
    {
        return !(neighbour is BlockTransparent);
    }
}

public abstract class Fluid : Block
{
    /// <summary>
    /// How many blocks already flown away from nearest source
    /// </summary>
    protected int currHorizontalFlow = 0;

    /// <summary>
    /// Surface height of source block
    /// </summary>
    protected const float defaultSurface = 0.8f;

    /// <summary>
    /// Surface height of block with max horizontal flow
    /// </summary>
    protected const float minSurface = 0.1f;

    #region Config
    public virtual float SinkSpeed => 3;
    public virtual float SpeedMultiplier => 0.8f;


    /// <summary>
    /// Max amount of blocks it can flow away from nearest source
    /// </summary>
    protected virtual int MaxHorizontalFlow => 2;


    /// <summary>
    /// Wether blocks next to 2 sources can become a source themselves
    /// </summary>
    protected virtual bool CanCombineToSource => false;

    public override bool CanPlaceInEntity => true;

    public virtual bool IsSource { get; protected set; }

    public virtual Color FogColor => new Color(0.1f, 0.25f, 0.6f);

    public virtual float FogDensity => 0.15f;
    #endregion

    protected float S => Mathf.Lerp(defaultSurface, minSurface, (float)currHorizontalFlow / MaxHorizontalFlow);

    public Fluid(Vector3 pos) : base(pos)
    {
        currHorizontalFlow = 0;
        IsSource = true;
    }

    public override bool DrawFaceNextTo(Direction dir, Block neighbour)
    {
        if (neighbour is BlockOpaque && dir != Direction.Up) return false;
        return neighbour?.Type != Type;
    }

    public override void OnBlockUpdate()
    {
        base.OnBlockUpdate();
        TryFlow();
        CheckForSource();
    }

    protected float GetMaxFlow(params Block[] blocks)
    {
        float max = S;
        foreach (var block in blocks)
            if (block is Fluid)
                max = Mathf.Max(max, (block as Fluid).S);

        return max;
    }

    #region Behaviour
    /// <summary>
    /// Gradually removes fluid without nearby source
    /// </summary>
    protected async void CheckForSource()
    {
        if (IsSource) return;
        if (!await Tick()) return;
        if (IsSource) return;

        var affectedChunks = new HashSet<Chunk>();

        var up = TerrainGenerator.Instance.GetBlock(Pos + Vector3.up);
        if (up?.Type == Type) return;
        var sideNeighbours = new List<Block>();
        sideNeighbours.Add(TerrainGenerator.Instance.GetBlock(Pos + Vector3.left));
        sideNeighbours.Add(TerrainGenerator.Instance.GetBlock(Pos + Vector3.right));
        sideNeighbours.Add(TerrainGenerator.Instance.GetBlock(Pos + Vector3.forward));
        sideNeighbours.Add(TerrainGenerator.Instance.GetBlock(Pos - Vector3.forward));
        foreach (var block in sideNeighbours)
        {
            if (block?.Type == Type && (block as Fluid).currHorizontalFlow < currHorizontalFlow) return;
        }
        if (currHorizontalFlow >= MaxHorizontalFlow)
        {
            TerrainGenerator.Instance.DestroyBlockSilent(Pos, affectedChunks);
        }
        else
        {
            currHorizontalFlow++;
            affectedChunks.Add(TerrainGenerator.Instance.GetChunk(Pos + Vector3.left));
            affectedChunks.Add(TerrainGenerator.Instance.GetChunk(Pos + Vector3.right));
            affectedChunks.Add(TerrainGenerator.Instance.GetChunk(Pos + Vector3.forward));
            affectedChunks.Add(TerrainGenerator.Instance.GetChunk(Pos - Vector3.forward));
            UpdateNeighbours();
            CheckForSource();
        }
        foreach (var chunk in affectedChunks) TerrainGenerator.Instance.MarkDirty(chunk);
    }

    /// <summary>
    /// If block below is air flow to it, if it is solid flow to unoccupied sides until maxHorizontalFlow is reached
    /// </summary>
    protected async void TryFlow()
    {
        if (!await Tick()) return;

        var affectedChunks = new HashSet<Chunk>();

        var blockBelow = TerrainGenerator.Instance.GetBlock(Pos + Vector3Int.down);
        if (blockBelow == null)
        {
            TryFlowToDir(Vector3Int.down, 0, affectedChunks);
            foreach (var chunk in affectedChunks) TerrainGenerator.Instance.MarkDirty(chunk);
            if (!IsSource) return;
        }
        if (blockBelow?.Type == Type)
        {
            (blockBelow as Fluid).currHorizontalFlow = 0;
            blockBelow.OnBlockUpdate();
            if (!IsSource) return;
        }
        if (currHorizontalFlow >= MaxHorizontalFlow) return;

        TryFlowToDir(Vector3Int.left, currHorizontalFlow + 1, affectedChunks);
        TryFlowToDir(Vector3Int.right, currHorizontalFlow + 1, affectedChunks);
        TryFlowToDir(Vec3Int.forward, currHorizontalFlow + 1, affectedChunks);
        TryFlowToDir(-Vec3Int.forward, currHorizontalFlow + 1, affectedChunks);
        foreach (var chunk in affectedChunks) TerrainGenerator.Instance.MarkDirty(chunk);
    }

    /// <summary>
    /// If pos + dir is free, places fluid there and sets its currHorizontalFlow to smaller value of either its own or newFlowAmount.
    /// If it is fluid, update its currHorizontalFlow and try to make it a source
    /// </summary>
    /// <param name="dir">direction to flow in</param>
    /// <param name="newFlowAmount"></param>
    protected void TryFlowToDir(Vector3Int dir, int newFlowAmount, HashSet<Chunk> affectedChunks)
    {
        var newPos = new Vector3(Pos.x + dir.x, Pos.y + dir.y, Pos.z + dir.z);
        var block = TerrainGenerator.Instance.GetBlock(newPos);
        Fluid f;
        if (block != null)
        {
            if (block.Type == Type)
            {
                f = block as Fluid;
                if (TryMakeSource(f, dir)) return;
                if (f.currHorizontalFlow > newFlowAmount)
                {
                    f.currHorizontalFlow = newFlowAmount;
                    affectedChunks.Add(TerrainGenerator.Instance.GetChunk(f.Pos));
                    f.TryFlow();
                }
            }
            return;
        }
        var newFluid = TerrainGenerator.Instance.PlaceBlockSilent(Type, newPos, affectedChunks);
        if (newFluid == null) return;
        f = newFluid as Fluid;
        if (TryMakeSource(f, dir)) return;
        f.currHorizontalFlow = newFlowAmount;
        f.IsSource = false;
    }

    /// <summary>
    /// Tries to make f to a source if enabled and 2+ source neighbours
    /// </summary>
    /// <param name="f">Fluid to try to make a source</param>
    /// <param name="dir">Direction of f in relation to neighbouring source</param>
    /// <returns></returns>
    protected bool TryMakeSource(Fluid f, Vector3Int dir)
    {
        if (!CanCombineToSource || !IsSource || f.IsSource) return false;
        var dir2 = TerrainGenerator.Instance.GetBlock(f.Pos + dir);
        if (dir2?.Type == Type && (dir2 as Fluid).IsSource)
        {
            f.MakeSource();
            return true;
        }
        var cross = dir.x != 0 ? Vector3.forward : Vector3.right;
        var cross1 = TerrainGenerator.Instance.GetBlock(f.Pos + cross);
        if (cross1?.Type == Type && (cross1 as Fluid).IsSource)
        {
            f.MakeSource();
            return true;
        }
        var cross2 = TerrainGenerator.Instance.GetBlock(f.Pos - cross);
        if (cross2?.Type == Type && (cross2 as Fluid).IsSource)
        {
            f.MakeSource();
            return true;
        }
        return false;
    }

    protected void MakeSource()
    {
        IsSource = true;
        currHorizontalFlow = 0;
        OnBlockUpdate();
        TerrainGenerator.Instance.MarkDirty(TerrainGenerator.Instance.GetChunk(Pos));
    }
    #endregion

    public override Vector3[] GetVertices(Direction dir, Block neighbour)
    {
        if (TerrainGenerator.Instance.GetBlock(Pos + Vector3.up) is Fluid)
            return base.GetVertices(dir, neighbour);

        Block f, b, l, r, fl, fr, bl, br, fu, bu, lu, ru, flu, fru, blu, bru;

        //TODO test performance of this, maybe keep neighbours saved and recalculate on change
        switch (dir)
        {
            case Direction.South:
                fl = TerrainGenerator.Instance.GetBlock(Pos + Vector3.left - Vector3.forward);
                fr = TerrainGenerator.Instance.GetBlock(Pos + Vector3.right - Vector3.forward);
                lu = TerrainGenerator.Instance.GetBlock(Pos + Vector3.up + Vector3.left);
                ru = TerrainGenerator.Instance.GetBlock(Pos + Vector3.up + Vector3.right);
                l = TerrainGenerator.Instance.GetBlock(Pos + Vector3.left);
                r = TerrainGenerator.Instance.GetBlock(Pos + Vector3.right);
                flu = TerrainGenerator.Instance.GetBlock(Pos - Vector3.forward + Vector3.up + Vector3.left);
                fru = TerrainGenerator.Instance.GetBlock(Pos - Vector3.forward + Vector3.up + Vector3.right);


                return new Vector3[]
                {
                    new Vector3(0, 0, 0),
                    new Vector3(0, (lu is Fluid || flu is Fluid) ? 1 : GetMaxFlow(l, fl), 0),
                    new Vector3(1, (ru is Fluid || fru is Fluid) ? 1 : GetMaxFlow(r, fr), 0),
                    new Vector3(1, 0, 0)
                };
            case Direction.North:
                bl = TerrainGenerator.Instance.GetBlock(Pos + Vector3.left + Vector3.forward);
                br = TerrainGenerator.Instance.GetBlock(Pos + Vector3.right + Vector3.forward);
                lu = TerrainGenerator.Instance.GetBlock(Pos + Vector3.up + Vector3.left);
                ru = TerrainGenerator.Instance.GetBlock(Pos + Vector3.up + Vector3.right);
                l = TerrainGenerator.Instance.GetBlock(Pos + Vector3.left);
                r = TerrainGenerator.Instance.GetBlock(Pos + Vector3.right);
                blu = TerrainGenerator.Instance.GetBlock(Pos + Vector3.forward + Vector3.up + Vector3.left);
                bru = TerrainGenerator.Instance.GetBlock(Pos + Vector3.forward + Vector3.up + Vector3.right);

                return new Vector3[]
                {
                    new Vector3(1, 0, 1),
                    new Vector3(1, (ru is Fluid || bru is Fluid) ? 1 : GetMaxFlow(r, br), 1),
                    new Vector3(0, (lu is Fluid || blu is Fluid) ? 1 : GetMaxFlow(l, bl), 1),
                    new Vector3(0, 0, 1)
                };
            case Direction.West:
                fl = TerrainGenerator.Instance.GetBlock(Pos + Vector3.left - Vector3.forward);
                bl = TerrainGenerator.Instance.GetBlock(Pos + Vector3.left + Vector3.forward);
                fu = TerrainGenerator.Instance.GetBlock(Pos - Vector3.forward + Vector3.up);
                bu = TerrainGenerator.Instance.GetBlock(Pos + Vector3.forward + Vector3.up);
                f = TerrainGenerator.Instance.GetBlock(Pos - Vector3.forward);
                b = TerrainGenerator.Instance.GetBlock(Pos + Vector3.forward);
                flu = TerrainGenerator.Instance.GetBlock(Pos - Vector3.forward + Vector3.up + Vector3.left);
                blu = TerrainGenerator.Instance.GetBlock(Pos + Vector3.forward + Vector3.up + Vector3.left);

                return new Vector3[]
                {
                    new Vector3(0, 0, 1),
                    new Vector3(0, (bu is Fluid || blu is Fluid) ? 1 : GetMaxFlow(b, bl), 1),
                    new Vector3(0, (fu is Fluid || flu is Fluid) ? 1 : GetMaxFlow(f, fl), 0),
                    new Vector3(0, 0, 0)
                };
            case Direction.East:
                fr = TerrainGenerator.Instance.GetBlock(Pos + Vector3.right - Vector3.forward);
                br = TerrainGenerator.Instance.GetBlock(Pos + Vector3.right + Vector3.forward);
                fu = TerrainGenerator.Instance.GetBlock(Pos - Vector3.forward + Vector3.up);
                bu = TerrainGenerator.Instance.GetBlock(Pos + Vector3.forward + Vector3.up);
                f = TerrainGenerator.Instance.GetBlock(Pos - Vector3.forward);
                b = TerrainGenerator.Instance.GetBlock(Pos + Vector3.forward);
                fru = TerrainGenerator.Instance.GetBlock(Pos - Vector3.forward + Vector3.up + Vector3.right);
                bru = TerrainGenerator.Instance.GetBlock(Pos + Vector3.forward + Vector3.up + Vector3.right);

                return new Vector3[]
                {
                    new Vector3(1, 0, 0),
                    new Vector3(1, (fu is Fluid || fru is Fluid) ? 1 : GetMaxFlow(f, fr), 0),
                    new Vector3(1, (bu is Fluid || bru is Fluid) ? 1 : GetMaxFlow(b, br), 1),
                    new Vector3(1, 0, 1)
                };
            case Direction.Up:
                l = TerrainGenerator.Instance.GetBlock(Pos + Vector3.left);
                r = TerrainGenerator.Instance.GetBlock(Pos + Vector3.right);
                f = TerrainGenerator.Instance.GetBlock(Pos - Vector3.forward);
                b = TerrainGenerator.Instance.GetBlock(Pos + Vector3.forward);

                fl = TerrainGenerator.Instance.GetBlock(Pos + Vector3.left - Vector3.forward);
                fr = TerrainGenerator.Instance.GetBlock(Pos + Vector3.right - Vector3.forward);
                bl = TerrainGenerator.Instance.GetBlock(Pos + Vector3.left + Vector3.forward);
                br = TerrainGenerator.Instance.GetBlock(Pos + Vector3.right + Vector3.forward);

                fu = TerrainGenerator.Instance.GetBlock(Pos - Vector3.forward + Vector3.up);
                bu = TerrainGenerator.Instance.GetBlock(Pos + Vector3.forward + Vector3.up);
                lu = TerrainGenerator.Instance.GetBlock(Pos + Vector3.left + Vector3.up);
                ru = TerrainGenerator.Instance.GetBlock(Pos + Vector3.right + Vector3.up);

                flu = TerrainGenerator.Instance.GetBlock(Pos - Vector3.forward + Vector3.up + Vector3.left);
                fru = TerrainGenerator.Instance.GetBlock(Pos - Vector3.forward + Vector3.up + Vector3.right);
                blu = TerrainGenerator.Instance.GetBlock(Pos + Vector3.forward + Vector3.up + Vector3.left);
                bru = TerrainGenerator.Instance.GetBlock(Pos + Vector3.forward + Vector3.up + Vector3.right);


                return new Vector3[]
                {
                    new Vector3(0, (fu is Fluid || lu is Fluid || flu is Fluid) ? 1 : GetMaxFlow(f, fl, l), 0),
                    new Vector3(0, (bu is Fluid || lu is Fluid || blu is Fluid) ? 1 : GetMaxFlow(b, bl, l), 1),
                    new Vector3(1, (bu is Fluid || ru is Fluid || bru is Fluid) ? 1 : GetMaxFlow(b, br, r), 1),
                    new Vector3(1, (fu is Fluid || ru is Fluid || fru is Fluid) ? 1 : GetMaxFlow(f, fr, r), 0)
                };
        }
        return base.GetVertices(dir, neighbour);
    }
}

