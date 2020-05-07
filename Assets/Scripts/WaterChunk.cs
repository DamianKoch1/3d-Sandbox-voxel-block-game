using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterChunk : Chunk
{
    public override void Generate()
    {
        blocks = new Block[SIZE, HEIGHT, SIZE];
        for (int x = 0; x < SIZE; x++)
        {
            for (int z = 0; z < SIZE; z++)
            {
                Vector3Int blockPos = new Vector3Int(x, TerrainGenerator.Instance.PerlinNoise(x + pos.x * 16, z + pos.y * 16) + TerrainGenerator.Instance.minSurfaceLevel, z);
                for (int y = 0; y <= blockPos.y; y++)
                {
                    var block = new Fluid(blockPos + new Vector3Int(pos.x, 0, pos.y) * SIZE);
                    blocks[blockPos.x, blockPos.y - y, blockPos.z] = block;
                }
            }
        }
        MakeMesh();
    }
}
