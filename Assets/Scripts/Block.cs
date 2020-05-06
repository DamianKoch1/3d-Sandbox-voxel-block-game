using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BlockType
{
    opaque,
    transparent,
    fluid,
    air
}

public class Block
{
    public Vector3Int pos;

    public BlockType type;

    public Block()
    { }

    public Block(Vector3Int _pos, BlockType _type = BlockType.opaque)
    {
        pos = _pos;
        type = _type;
    }
}

