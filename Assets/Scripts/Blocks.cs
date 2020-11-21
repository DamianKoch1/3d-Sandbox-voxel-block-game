using System.Collections.Generic;
using UnityEngine;


public class Grass : BlockOpaque
{
    public Grass(Vector3Int pos) : base(pos)
    {
        Type = BlockType.Grass;
    }
}

public class Dirt : BlockOpaque
{
    public Dirt(Vector3Int pos) : base(pos)
    {
        Type = BlockType.Dirt;
    }
}

public class Stone : BlockOpaque
{
    public Stone(Vector3Int pos) : base(pos)
    {
        Type = BlockType.Stone;
    }
}

public class BottomStone : BlockOpaque
{
    public BottomStone(Vector3Int pos) : base(pos)
    {
        Type = BlockType.BottomStone;
    }
}

public class Water : Fluid
{
    public Water(Vector3Int pos) : base(pos)
    {
        Type = BlockType.Water;
        FallSpeed = 3;
        maxHorizontalFlow = 5;
    }
}

public class Glass : BlockTransparent
{
    public Glass(Vector3Int pos) : base(pos)
    {
        Type = BlockType.Glass;
    }
}

public class TNT : BlockOpaque, IUseable
{
    private int range = 2;

    public TNT(Vector3Int pos) : base(pos)
    {
        Type = BlockType.Tnt;
    }

    public void OnUsed()
    {
        List<Vector3> blocksToDestroy = new List<Vector3>();
        for (int y = -range; y <= range; y++)
        {
            if (Pos.y + y < 0) continue;
            if (Pos.y + y >= Chunk.HEIGHT) continue;
            for (int x = -range; x <= range; x++)
            {
                for (int z = -range; z <= range; z++)
                {
                    blocksToDestroy.Add(Pos + new Vector3Int(x, y, z));
                }
            }
        }
        TerrainGenerator.Instance.DestroyBlocks(blocksToDestroy);
    }
}