using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    private static TerrainGenerator instance;
    public static TerrainGenerator Instance
    {
        get
        {
            if (!instance)
            {
                instance = FindObjectOfType<TerrainGenerator>();
            }
            return instance;
        }
    }


    public Chunk chunkPrefab;

    public Vector2Int chunkCount;

    public Dictionary<Vector2Int, Chunk> chunks;

    [Range(0, 1)]
    public float perlinScale;

    public int amplitude;

    public bool useRandomSeed;
    
    public float seed;

    public void Generate()
    {
        Clear();
        chunks = new Dictionary<Vector2Int, Chunk>();

        if (useRandomSeed)
        {
            seed = Random.Range(0f, 10000f);
        }

        for (int x = 0; x < chunkCount.x; x++)
        {
            for (int z = 0; z < chunkCount.y; z++)
            {
                var chunk = Instantiate(chunkPrefab.gameObject, transform).GetComponent<Chunk>();
                var chunkPos = new Vector2Int(x, z);
                chunk.Initialize(chunkPos);
                chunks[chunkPos] = chunk;
            }
        }
    }

    public void Clear()
    {
        chunks = new Dictionary<Vector2Int, Chunk>();

        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    public int PerlinNoise(float x, float z)
    {
        return (int)(Mathf.PerlinNoise(x * perlinScale + seed, z * perlinScale + seed) * amplitude);
    }

    public Chunk GetChunk(float x, float z)
    {
        var chunkPos = new Vector2Int((int)(x / 16), (int)(z / 16));
        if (!chunks.ContainsKey(chunkPos)) return null;
        return chunks[chunkPos];
    }

    public Block GetBlock(float x, float y, float z)
    {
        var chunk = GetChunk(x, z);
        if (!chunk) return null;
        return chunk.GetBlock(x, y, z);
    }
}
