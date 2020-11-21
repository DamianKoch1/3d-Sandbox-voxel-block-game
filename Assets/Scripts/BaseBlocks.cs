using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class Block
{
    public Block(Vector3Int pos)
    {
        Pos = pos;
    }

    public Vector3Int Pos { get; private set; }

    public BlockType Type { get; protected set; }

    public bool CanPlaceInEntity { get; protected set; } = false;


    /// <summary>
    /// Should a face be drawn when given block is next to it?
    /// </summary>
    /// <param name="neighbour">neighbour block</param>
    /// <returns></returns>
    public abstract bool DrawFaceNextTo(Block neighbour);

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
    }

    /// <summary>
    /// Usually called when placed / adjacent block is placed / destroyed
    /// </summary>
    public virtual void OnBlockUpdate()
    { }

    public List<Block> GetNeighbours()
    {
        List<Block> neighbours = new List<Block>();
        neighbours.Add(TerrainGenerator.Instance.GetBlock(Pos + Vector3Int.left));
        neighbours.Add(TerrainGenerator.Instance.GetBlock(Pos + Vector3Int.right));
        neighbours.Add(TerrainGenerator.Instance.GetBlock(Pos + Vector3Int.up));
        neighbours.Add(TerrainGenerator.Instance.GetBlock(Pos + Vector3Int.down));
        neighbours.Add(TerrainGenerator.Instance.GetBlock(Pos + new Vector3Int(0, 0, 1)));
        neighbours.Add(TerrainGenerator.Instance.GetBlock(Pos + new Vector3Int(0, 0, -1)));
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
    public BlockOpaque(Vector3Int pos) : base(pos)
    { }

    public override bool DrawFaceNextTo(Block neighbour)
    {
        return !(neighbour is BlockOpaque);
    }
}

public abstract class BlockTransparent : Block
{
    public BlockTransparent(Vector3Int pos) : base(pos)
    { }

    public override bool DrawFaceNextTo(Block neighbour)
    {
        return !(neighbour is BlockTransparent);
    }
}

public abstract class Fluid : Block
{
    public float FallSpeed { get; protected set; } = 3;

    protected float tickInterval = 0.5f;

    protected int currHorizontalFlow = 0;
    protected int maxHorizontalFlow = 2;

    protected readonly float defaultSurface = 0.8f;
    protected readonly float minSurface = 0.1f;

    protected float S => Mathf.Lerp(defaultSurface, minSurface, (float)currHorizontalFlow / maxHorizontalFlow);

    public Fluid(Vector3Int pos) : base(pos)
    {
        currHorizontalFlow = 0;
        defaultSurface = 0.8f;
        minSurface = 0.1f;
        CanPlaceInEntity = true;
    }

    public override bool DrawFaceNextTo(Block neighbour)
    {
        return neighbour == null || neighbour is BlockTransparent;
    }

    //TODO if no neighbours with lower flow level increase flow level, if at max remove
    public override void OnBlockUpdate()
    {
        base.OnBlockUpdate();
        TryFlow();
    }

    #region Flowing
    /// <summary>
    /// Waits for tickInterval seconds, if block below is air flow to it, if it is solid flow to unoccupied sides until maxHorizontalFlow is reached
    /// </summary>
    protected async void TryFlow()
    {
        await Task.Delay((int)(tickInterval * 1000));

        var affectedChunks = new HashSet<Chunk>();

        var blockBelow = TerrainGenerator.Instance.GetBlock(Pos + Vector3Int.down);
        if (blockBelow == null)
        {
            TryFlowToDir(Vector3Int.down, 0, affectedChunks);
            foreach (var chunk in affectedChunks) TerrainGenerator.Instance.MarkDirty(chunk);
            return;
        }
        if (blockBelow is Fluid)
        {
            ((Fluid)blockBelow).currHorizontalFlow = 0;
            return;
        }
        if (currHorizontalFlow >= maxHorizontalFlow) return;

        TryFlowToDir(Vector3Int.left, currHorizontalFlow + 1, affectedChunks);
        TryFlowToDir(Vector3Int.right, currHorizontalFlow + 1, affectedChunks);
        TryFlowToDir(new Vector3Int(0, 0, 1), currHorizontalFlow + 1, affectedChunks);
        TryFlowToDir(new Vector3Int(0, 0, -1), currHorizontalFlow + 1, affectedChunks);
        foreach (var chunk in affectedChunks) TerrainGenerator.Instance.MarkDirty(chunk);
    }

    /// <summary>
    /// If pos + dir is free, places fluid there and sets its currHorizontalFlow to smaller value of either its own or newFlowAmount
    /// </summary>
    /// <param name="dir">direction to flow in</param>
    /// <param name="newFlowAmount"></param>
    protected void TryFlowToDir(Vector3Int dir, int newFlowAmount, HashSet<Chunk> affectedChunks)
    {
        var newPos = Pos + dir;
        var block = TerrainGenerator.Instance.GetBlock(newPos);
        if (block != null)
        {
            if (block is Fluid)
            {
                var fluid = (Fluid)block;
                fluid.currHorizontalFlow = Mathf.Min(fluid.currHorizontalFlow, newFlowAmount);
            }
            return;
        }
        var newFluid = TerrainGenerator.Instance.PlaceBlockSilent(Type, newPos, affectedChunks);
        if (!(newFluid is Fluid)) return;
        ((Fluid)newFluid).currHorizontalFlow = newFlowAmount;
    }
    #endregion

    private float GetMaxFlow(params Block[] blocks)
    {
        float max = S;
        foreach (var block in blocks)
            if (block is Fluid)
                max = Mathf.Max(max, (block as Fluid).S);

        return max;
    }

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

