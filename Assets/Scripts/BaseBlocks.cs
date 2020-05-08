using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Block
{
    public const int TILESET_DIMENSIONS = 10;

    public Vector3Int pos;

    public Block(Vector3Int _pos)
    {
        pos = _pos;
    }

    protected abstract Vector2Int GetTilesetPos();

    protected virtual Vector2Int GetSideTilesetPos()
    {
        return GetTilesetPos();
    }

    protected virtual Vector2Int GetBottomTilesetPos()
    {
        return GetTilesetPos();
    }

    private Vector2[] GetUVs(Vector2Int offset)
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
    { }

    public virtual void OnDestroyed()
    { }

    public virtual void OnUsed()
    { }


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

}

