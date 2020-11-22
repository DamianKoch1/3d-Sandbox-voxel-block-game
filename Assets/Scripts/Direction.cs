using UnityEngine;

public enum Direction
{
    South = 0,  // Front
    North = 1,  // Back
    West = 2,   // Left
    East = 3,   // Right
    Down = 4,   // Bottom
    Up = 5,     // Top
}

public static class Vec3Int
{
    public static readonly Vector3Int forward = new Vector3Int(0, 0, 1);
}
