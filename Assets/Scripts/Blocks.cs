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
    public override float BlastResistance => 0.7f;

    public Stone(Vector3Int pos) : base(pos)
    {
        Type = BlockType.Stone;
    }
}

public class BottomStone : BlockOpaque
{
    public override float BlastResistance => 10000f;

    public BottomStone(Vector3Int pos) : base(pos)
    {
        Type = BlockType.BottomStone;
    }
}

public class Water : Fluid
{
    protected override bool CanCombineToSource => true;

    public override float SinkSpeed => 3;

    protected override int MaxHorizontalFlow => 5;

    public Water(Vector3Int pos) : base(pos)
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

    public Lava(Vector3Int pos) : base(pos)
    {
        Type = BlockType.Lava;
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
    protected virtual int Range => 10;

    protected virtual float Scattering => 2;

    public TNT(Vector3Int pos) : base(pos)
    {
        Type = BlockType.Tnt;
    }

    public void OnUsed()
    {
        List<Vector3> blocksToDestroy = new List<Vector3>();
        for (int y = -Range; y <= Range; y++)
        {
            if (Pos.y + y < 0) continue;
            if (Pos.y + y >= Chunk.HEIGHT) continue;
            for (int x = -Range; x <= Range; x++)
            {
                for (int z = -Range; z <= Range; z++)
                {
                    var pos = new Vector3(x, y, z);
                    var sqrMg = pos.sqrMagnitude + Random.Range(-Range * Scattering, Range * Scattering);
                    if (sqrMg > Range * Range) continue;
                    var block = TerrainGenerator.Instance.GetBlock(Pos + pos);
                    if (block == null || block is Fluid || 1 - (sqrMg / Range) / Range <= block.BlastResistance * block.BlastResistance) continue;
                    blocksToDestroy.Add(Pos + pos);
                }
            }
        }
        TerrainGenerator.Instance.DestroyBlocks(blocksToDestroy);
    }
}

public class Log : BlockOpaque
{
    public Log(Vector3Int pos) : base(pos)
    {
        Type = BlockType.Log;
    }
}

public class Leaves : BlockTransparent
{
    public Leaves(Vector3Int pos) : base(pos)
    {
        Type = BlockType.Leaves;
    }
}

public class Sand : BlockOpaque
{
    public Sand(Vector3Int pos) : base(pos)
    {
        Type = BlockType.Sand;
    }
}