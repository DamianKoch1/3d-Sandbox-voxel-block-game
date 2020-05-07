using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Block
{
    public Vector3Int pos;

    public Block(Vector3Int _pos)
    {
        pos = _pos;
    }

    /// <summary>
    /// Should a face be drawn when given block is next to it?
    /// </summary>
    /// <param name="neighbour">neighbour block</param>
    /// <returns></returns>
    public abstract bool DrawFaceNextTo(Block neighbour);
}

public class BlockOpaque : Block
{
    public BlockOpaque(Vector3Int _pos) : base(_pos)
    { }

    public override bool DrawFaceNextTo(Block neighbour)
    {
        return !(neighbour is BlockOpaque);
    }
}

public class BlockTransparent : Block
{
    public BlockTransparent(Vector3Int _pos) : base(_pos)
    { }

    public override bool DrawFaceNextTo(Block neighbour)
    {
        return !(neighbour is BlockTransparent);
    }
}

public class Fluid : Block
{
    public Fluid(Vector3Int _pos) : base(_pos)
    { }

    public override bool DrawFaceNextTo(Block neighbour)
    {
        return neighbour == null;
    }

    public float fallSpeed = 3;
}

