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

    public virtual void OnPlaced()
    {
        OnBlockUpdate();
        UpdateNeighbours();
    }

    public virtual void OnDestroyed()
    {
        UpdateNeighbours();
    }

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

    public Fluid(Vector3Int _pos) : base(_pos)
    { }

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

    protected async void TryFlow()
    {
        await Task.Delay((int)(tickInterval * 1000));

        var flowPos = pos + Vector3Int.down;
        var blockBelow = TerrainGenerator.Instance.GetBlock(flowPos);
        if (blockBelow == null)
        {
            TerrainGenerator.Instance.PlaceBlock(type, flowPos);
            return;
        }
        else if (blockBelow is Fluid) return;

        flowPos = pos + Vector3Int.left;
        if (TerrainGenerator.Instance.GetBlock(flowPos) == null)
        {
            TerrainGenerator.Instance.PlaceBlock(type, flowPos);
        }

        flowPos = pos + Vector3Int.right;
        if (TerrainGenerator.Instance.GetBlock(flowPos) == null)
        {
            TerrainGenerator.Instance.PlaceBlock(type, flowPos);
        }


        flowPos = pos + new Vector3Int(0, 0, 1);
        if (TerrainGenerator.Instance.GetBlock(flowPos) == null)
        {
            TerrainGenerator.Instance.PlaceBlock(type, flowPos);
        }

        flowPos = pos + new Vector3Int(0, 0, -1);
        if (TerrainGenerator.Instance.GetBlock(flowPos) == null)
        {
            TerrainGenerator.Instance.PlaceBlock(type, flowPos);
        }
    }
}

