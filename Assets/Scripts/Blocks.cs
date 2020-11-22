using System.Collections.Generic;
using UnityEngine;


public class Grass : BlockOpaque
{
    public Grass(Vector3 pos) : base(pos)
    {
        Type = BlockType.Grass;
    }
}

public class Dirt : BlockOpaque
{
    public Dirt(Vector3 pos) : base(pos)
    {
        Type = BlockType.Dirt;
    }
}

public class Stone : BlockOpaque
{
    public Stone(Vector3 pos) : base(pos)
    {
        Type = BlockType.Stone;
    }
}

public class BottomStone : BlockOpaque
{
    public BottomStone(Vector3 pos) : base(pos)
    {
        Type = BlockType.BottomStone;
    }
}

public class Water : Fluid
{
    protected override bool CanCombineToSource => true;

    public override float SinkSpeed => 3;

    protected override int MaxHorizontalFlow => 5;

    public Water(Vector3 pos) : base(pos)
    {
        Type = BlockType.Water;
    }
}

public class Lava : Fluid
{
    public override float SinkSpeed => 1;

    protected override int MaxHorizontalFlow => 2;

    protected override float TickInterval => 1f;

    public override float SpeedMultiplier => 0.3f;

    public override Color FogColor => new Color(1f, 0.35f, 0f);

    public override float FogDensity => 0.45f;

    public Lava(Vector3 pos) : base(pos)
    {
        Type = BlockType.Lava;
    }
}

public class Glass : BlockTransparent
{
    public Glass(Vector3 pos) : base(pos)
    {
        Type = BlockType.Glass;
    }
}

public class TNT : BlockOpaque, IUseable
{
    private int range = 2;

    public TNT(Vector3 pos) : base(pos)
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