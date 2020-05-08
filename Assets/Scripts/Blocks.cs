using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Grass : BlockOpaque
{
    public Grass(Vector3Int _pos) : base(_pos)
    {
        type = BlockType.grass;
    }

    protected override Vector2Int GetTilesetPos()
    {
        return new Vector2Int(0, 0);
    }

    protected override Vector2Int GetSideTilesetPos()
    {
        return new Vector2Int(1, 0);
    }

    protected override Vector2Int GetBottomTilesetPos()
    {
        return new Vector2Int(2, 0);
    }
}

public class Dirt : BlockOpaque
{
    public Dirt(Vector3Int _pos) : base(_pos)
    {
        type = BlockType.dirt;
    }

    protected override Vector2Int GetTilesetPos()
    {
        return new Vector2Int(2, 0);
    }
}

public class Stone : BlockOpaque
{
    public Stone(Vector3Int _pos) : base(_pos)
    {
        type = BlockType.stone;
    }

    protected override Vector2Int GetTilesetPos()
    {
        return new Vector2Int(3, 0);
    }
}

public class BottomStone : BlockOpaque
{
    public BottomStone(Vector3Int _pos) : base(_pos)
    {
        type = BlockType.bottomStone;
    }

    protected override Vector2Int GetTilesetPos()
    {
        return new Vector2Int(4, 0);
    }
}

public class Water : Fluid
{
    public Water(Vector3Int _pos) : base(_pos)
    {
        type = BlockType.water;
    }

    protected override Vector2Int GetTilesetPos()
    {
        return new Vector2Int(5, 0);
    }
}

public class Glass : BlockTransparent
{
    public Glass(Vector3Int _pos) : base(_pos)
    {
        type = BlockType.glass;
    }

    protected override Vector2Int GetTilesetPos()
    {
        return new Vector2Int(6, 0);
    }
}