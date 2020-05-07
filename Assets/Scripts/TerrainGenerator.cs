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

    public Dictionary<Vector2Int, Chunk> chunks;

    [Range(0, 0.15f)]
    public float frequency;

    [Range(0, 50)]
    public int amplitude;

    public int minSurfaceLevel = 50;

    public int waterLevel = 60;

    public bool useRandomSeed;

    public float seed;

    private void Start()
    {
        Generate();
    }

    public void Generate()
    {
        Clear();

        if (useRandomSeed)
        {
            seed = Random.Range(0f, 10000f);
        }

        for (int x = -2; x < 2; x++)
        {
            for (int z = -2; z < 2; z++)
            {
                var chunkPos = new Vector2Int(x, z);
                var chunk = Instantiate(chunkPrefab.gameObject, transform).GetComponent<Chunk>();
                chunk.Initialize(chunkPos);
                chunks[chunkPos] = chunk;
            }
        }

        MakeMesh();
    }

    public void MakeMesh()
    {
        foreach (var chunk in chunks.Values)
        {
            chunk.MakeMesh();
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
        return (int)(Mathf.PerlinNoise(x * frequency + seed, z * frequency + seed) * amplitude) + minSurfaceLevel;
    }

    public Chunk GetChunk(float x, float z)
    {
        var chunkPos = new Vector2Int(Mathf.FloorToInt(x / 16), Mathf.FloorToInt(z / 16));
        if (!chunks.ContainsKey(chunkPos)) return null;
        return chunks[chunkPos];
    }

    public Block GetBlock(Vector3 pos)
    {
        var chunk = GetChunk(pos.x, pos.z);
        if (!chunk) return null;
        return chunk.GetBlock(pos);
    }

    public bool DestroyBlock(Vector3 pos)
    {
        var chunk = GetChunk(pos.x, pos.z);
        if (!chunk) return false;
        return chunk.DestroyBlock(pos);
    }

    public bool PlaceBlock(Vector3 pos)
    {
        if (pos.y <= 0) return false;
        if (pos.y >= Chunk.HEIGHT) return false;
        var chunk = GetChunk(pos.x, pos.z);
        if (!chunk) return false;
        return chunk.PlaceBlock(pos);
    }
}
