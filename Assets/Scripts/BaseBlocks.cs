using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class Block
{
    /// <summary>
    /// How many 16px block textures there are in a row / column
    /// </summary>
    public const int TILESET_DIMENSIONS = 10;

    public Vector3Int pos;

    public BlockType type;

    public Block(Vector3Int _pos)
    {
        pos = _pos;
    }

    /// <summary>
    /// Where on the tileset this blocks default (top) texture starts (bottom left to top right, 1 unit = 16px)
    /// </summary>
    /// <returns></returns>
    protected abstract Vector2Int GetTilesetPos();

    /// <summary>
    /// Where on the tileset this blocks side texture starts (bottom left to top right, 1 unit = 16px)
    /// </summary>
    /// <returns></returns>
    protected virtual Vector2Int GetSideTilesetPos()
    {
        return GetTilesetPos();
    }

    /// <summary>
    /// Where on the tileset this blocks bottom texture starts (bottom left to top right, 1 unit = 16px)
    /// </summary>
    /// <returns></returns>
    protected virtual Vector2Int GetBottomTilesetPos()
    {
        return GetTilesetPos();
    }

    private static Vector2[] GetUVs(Vector2Int offset)
    {
        return new Vector2[]
        {
            new Vector2(offset.x, offset.y) / TILESET_DIMENSIONS,
            new Vector2(offset.x, offset.y + 1) / TILESET_DIMENSIONS,
            new Vector2(offset.x + 1, offset.y + 1) / TILESET_DIMENSIONS,
            new Vector2(offset.x + 1, offset.y) / TILESET_DIMENSIONS
        };
    }

    public Vector2[] GetTopUVs()
    {
        return GetUVs(GetTilesetPos());
    }

    public Vector2[] GetSideUVs()
    {
        return GetUVs(GetSideTilesetPos());
    }

    public Vector2[] GetBottomUVs()
    {
        return GetUVs(GetBottomTilesetPos());
    }


    /// <summary>
    /// Should a face be drawn when given block is next to it?
    /// </summary>
    /// <param name="neighbour">neighbour block</param>
    /// <returns></returns>
    public abstract bool DrawFaceNextTo(Block neighbour);

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
        neighbours.Add(TerrainGenerator.Instance.GetBlock(pos + Vector3Int.left));
        neighbours.Add(TerrainGenerator.Instance.GetBlock(pos + Vector3Int.right));
        neighbours.Add(TerrainGenerator.Instance.GetBlock(pos + Vector3Int.up));
        neighbours.Add(TerrainGenerator.Instance.GetBlock(pos + Vector3Int.down));
        neighbours.Add(TerrainGenerator.Instance.GetBlock(pos + new Vector3Int(0, 0, 1)));
        neighbours.Add(TerrainGenerator.Instance.GetBlock(pos + new Vector3Int(0, 0, -1)));
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
    public BlockOpaque(Vector3Int _pos) : base(_pos)
    { }

    public override bool DrawFaceNextTo(Block neighbour)
    {
        return !(neighbour is BlockOpaque);
    }
}

public abstract class BlockTransparent : Block
{
    public BlockTransparent(Vector3Int _pos) : base(_pos)
    { }

    public override bool DrawFaceNextTo(Block neighbour)
    {
        return !(neighbour is BlockTransparent);
    }

    protected override Vector2Int GetTilesetPos()
    {
        return new Vector2Int(0, 0);
    }
}

public abstract class Fluid : Block
{
    public float fallSpeed = 3;

    protected float tickInterval = 0.5f;

    protected int currHorizontalFlow = 0;
    protected int maxHorizontalFlow = 2;

    public Fluid(Vector3Int _pos) : base(_pos)
    {
        currHorizontalFlow = 0;
    }

    public override bool DrawFaceNextTo(Block neighbour)
    {
        return neighbour == null || neighbour is BlockTransparent;
    }

    protected override Vector2Int GetTilesetPos()
    {
        return new Vector2Int(5, 0);
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

        var blockBelow = TerrainGenerator.Instance.GetBlock(pos + Vector3Int.down);
        if (blockBelow == null)
        {
            TryFlowToDir(Vector3Int.down, 0);
            return;
        }
        else if (blockBelow is Fluid)
        {
            ((Fluid)blockBelow).currHorizontalFlow = 0;
            return;
        }
        else if (currHorizontalFlow >= maxHorizontalFlow) return;

        TryFlowToDir(Vector3Int.left, currHorizontalFlow + 1);
        TryFlowToDir(Vector3Int.right, currHorizontalFlow + 1);
        TryFlowToDir(new Vector3Int(0, 0, 1), currHorizontalFlow + 1);
        TryFlowToDir(new Vector3Int(0, 0, -1), currHorizontalFlow + 1);
    }

    /// <summary>
    /// If pos + dir is free, places fluid there and sets its currHorizontalFlow to smaller value of either its own or newFlowAmount
    /// </summary>
    /// <param name="dir">direction to flow in</param>
    /// <param name="newFlowAmount"></param>
    protected void TryFlowToDir(Vector3Int dir, int newFlowAmount)
    {
        var newPos = pos + dir;
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
        var newFluid = TerrainGenerator.Instance.PlaceBlock(type, newPos);
        if (!(newFluid is Fluid)) return;
        ((Fluid)newFluid).currHorizontalFlow = newFlowAmount;
    }
}

