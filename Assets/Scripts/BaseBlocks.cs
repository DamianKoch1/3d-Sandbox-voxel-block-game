using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class Block
{
    public Vector3Int Pos { get; private set; }

    public BlockType Type { get; protected set; }

    public Block(Vector3Int pos)
    {
        Pos = pos;
    }

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

    public Fluid(Vector3Int pos) : base(pos)
    {
        currHorizontalFlow = 0;
    }

    public override bool DrawFaceNextTo(Block neighbour)
    {
        return neighbour == null || neighbour is BlockTransparent;
    }

    public override void OnBlockUpdate()
    {
        base.OnBlockUpdate();
        TryFlow();
    }

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
        else if (blockBelow is Fluid)
        {
            ((Fluid)blockBelow).currHorizontalFlow = 0;
            return;
        }
        else if (currHorizontalFlow >= maxHorizontalFlow) return;

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
}

