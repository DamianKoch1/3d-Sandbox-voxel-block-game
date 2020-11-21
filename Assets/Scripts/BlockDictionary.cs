using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BlockDictionary
{
    /// <summary>
    /// How many 16px block textures there are in a row / column
    /// </summary>
    public const int TILESET_DIMENSIONS = 10;

    public const float TILESET_DIMENSIONSf = TILESET_DIMENSIONS;

    private static Direction[] directions;

    static BlockDictionary()
    {
        directions = (Direction[])System.Enum.GetValues(typeof(Direction));

        UVs = new Dictionary<BlockType, Dictionary<Direction, Vector2[]>>();

        InitVertexDict();

        RegisterBlocks();
    }

    private static void RegisterBlocks()
    {
        RegisterBlock(BlockType.Grass, 0, 0
            , new TexOffset(1, 0
                , Direction.North
                , Direction.South
                , Direction.East
                , Direction.West
            )
            , new TexOffset(2, 0, Direction.Down)
        );

        RegisterBlock(BlockType.Dirt, 2, 0);
        RegisterBlock(BlockType.Stone, 3, 0);
        RegisterBlock(BlockType.BottomStone, 4, 0);
        RegisterBlock(BlockType.Water, 5, 0);
        RegisterBlock(BlockType.Glass, 6, 0);
        RegisterBlock(BlockType.Tnt, 7, 0);
    }


    #region UVs
    private static Dictionary<BlockType, Dictionary<Direction, Vector2[]>> UVs;

    public static Vector2[] GetBlockUVs(BlockType type, Direction dir) => UVs[type][dir];

    private static void RegisterBlock(BlockType type, int uvX, int uvY)
    {
        UVs.Add(type, new Dictionary<Direction, Vector2[]>());
        foreach (var dir in directions)
            UVs[type].Add(dir, GetUVs(uvX, uvY));
    }

    private static void RegisterBlock(BlockType type, int uvX, int uvY, params TexOffset[] customs)
    {
        UVs.Add(type, new Dictionary<Direction, Vector2[]>());
        foreach (var tex in customs)
            foreach (var dir in tex.dirs)
                UVs[type].Add(dir, GetUVs(tex.x, tex.y));

        foreach (var dir in directions)
        {
            if (UVs[type].ContainsKey(dir)) continue;
            UVs[type].Add(dir, GetUVs(uvX, uvY));
        }
    }

    private static Vector2[] GetUVs(int offsetX, int offsetY)
    {
        return new Vector2[]
        {
            new Vector2(offsetX / TILESET_DIMENSIONSf, offsetY),
            new Vector2(offsetX / TILESET_DIMENSIONSf, (offsetY + 1) / TILESET_DIMENSIONSf),
            new Vector2((offsetX + 1) / TILESET_DIMENSIONSf, (offsetY + 1) / TILESET_DIMENSIONSf),
            new Vector2((offsetX + 1) / TILESET_DIMENSIONSf, offsetY / TILESET_DIMENSIONSf)
        };
    }

    private class TexOffset
    {
        public TexOffset(int x, int y, params Direction[] dirs)
        {
            this.dirs = dirs;
            this.x = x;
            this.y = y;
        }

        public Direction[] dirs;
        public int x;
        public int y;
    }
    #endregion

    #region Vertices
    private static Dictionary<Direction, Vector3[]> defaultVertices;

    public static Vector3[] GetDefaultVertices(Direction dir) => defaultVertices[dir];

    private static void InitVertexDict()
    {
        defaultVertices = new Dictionary<Direction, Vector3[]>();
        defaultVertices.Add(Direction.North, new Vector3[]
        {
            new Vector3(1, 0, 1),
            new Vector3(1, 1, 1),
            new Vector3(0, 1, 1),
            new Vector3(0, 0, 1)
        });
        defaultVertices.Add(Direction.South, new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 1, 0),
            new Vector3(1, 0, 0)
        });
        defaultVertices.Add(Direction.East, new Vector3[]
        {
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(1, 1, 1),
            new Vector3(1, 0, 1)
        });
        defaultVertices.Add(Direction.West, new Vector3[]
        {
            new Vector3(0, 0, 1),
            new Vector3(0, 1, 1),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 0)
        });
        defaultVertices.Add(Direction.Up, new Vector3[]
        {
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 1),
            new Vector3(1, 1, 1),
            new Vector3(1, 1, 0)
        });
        defaultVertices.Add(Direction.Down, new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
            new Vector3(0, 0, 1)
        });
    }
    #endregion
}
