﻿using System.Collections;
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

    [SerializeField]
    private Chunk chunkPrefab;

    [SerializeField, Range(0.01f, 0.5f)]
    private float meshBuildingInterval = 0.1f;

    [SerializeField, Tooltip("Should far chunks get unloaded?")]
    private bool unloadChunks;

    public Dictionary<Vector2Int, Chunk> chunks;

    [SerializeField]
    private Noise shapeNoise;

    [SerializeField]
    private Noise detailNoise;

    [SerializeField]
    private Noise caveShapeNoise;

    [SerializeField]
    private Noise caveDetailNoise;

    [Range(0, 1), Tooltip("Carves a cave if CaveNoise at position is above this")]
    public float caveNoiseThreshold = 0.8f;

    [Tooltip("How far below the surface caves start to generate")]
    public int minCaveSurfaceDistance = 10;

    [Range(0, 100)]
    public int minSurfaceLevel = 50;

    [Range(0, 70)]
    public int waterLevel = 60;

    [Range(2, 10)]
    public int dirtLayerSize = 5;

    [SerializeField, Range(0, 10)]
    private int renderDistance = 5;

    [SerializeField]
    private bool useRandomSeed;

    private Player player;

    public Vector2Int playerChunkPos;

    private List<Chunk> dirtyChunks;

    private void Start()
    {
        Generate();
        player = FindObjectOfType<Player>();
        playerChunkPos = GetChunk(player.transform.position).pos;
        dirtyChunks = new List<Chunk>();
        InvokeRepeating(nameof(UpdatePlayerPos), 1, 1);
        InvokeRepeating(nameof(RebuildDirtyChunk), 1, meshBuildingInterval);
    }

    /// <summary>
    /// Checks in which chunk player is, calls UpdateChunkVisibility if it changed
    /// </summary>
    private void UpdatePlayerPos()
    {
        var newChunkPos = new Vector2Int(Mathf.FloorToInt(player.transform.position.x / 16), Mathf.FloorToInt(player.transform.position.z / 16));
        if (newChunkPos == playerChunkPos) return;
        playerChunkPos = newChunkPos;
        if (!chunks.ContainsKey(playerChunkPos))
        {
            AddChunk(playerChunkPos);
        }
        UpdateChunkVisibility();
    }

    /// <summary>
    /// Toggles chunks within renderDistance from player on, others off
    /// </summary>
    private void UpdateChunkVisibility()
    {
        foreach (var chunk in chunks.Values)
        {
            if (Mathf.Abs(chunk.pos.x - playerChunkPos.x) <= renderDistance && Mathf.Abs(chunk.pos.y - playerChunkPos.y) <= renderDistance)
            {
                if (!chunk.gameObject.activeSelf)
                {
                    chunk.gameObject.SetActive(true);
                }
            }
            else if (!unloadChunks)
            {
                if (!chunk.gameObject.activeSelf)
                {
                    chunk.gameObject.SetActive(true);
                }
            }
            else
            {
                if (chunk.gameObject.activeSelf)
                {
                    chunk.gameObject.SetActive(false);
                }
            }
        }

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                var newChunkPos = new Vector2Int(x, z) + playerChunkPos;
                if (!chunks.ContainsKey(newChunkPos))
                {
                    AddChunk(newChunkPos);
                }
            }
        }
    }

    /// <summary>
    /// Builds the mesh for the oldest dirty chunk and removes it from the dirty list
    /// </summary>
    private void RebuildDirtyChunk()
    {
        if (dirtyChunks.Count == 0) return;
        dirtyChunks[0].MakeMesh();
        dirtyChunks.RemoveAt(0);
    }

    /// <summary>
    /// Adds a chunk, marks itself and its neighbours dirty, use this when adding chunks at runtime
    /// </summary>
    /// <param name="_idx"></param>
    private void AddChunk(Vector2Int _idx)
    {
        GenerateChunk(_idx);
        MarkDirty(chunks[_idx]);
        MarkDirty(GetChunkByIdx(_idx + Vector2Int.left));
        MarkDirty(GetChunkByIdx(_idx + Vector2Int.right));
        MarkDirty(GetChunkByIdx(_idx + Vector2Int.up));
        MarkDirty(GetChunkByIdx(_idx + Vector2Int.down));
    }

    /// <summary>
    /// Queues a chunk for a mesh rebuild
    /// </summary>
    /// <param name="c"></param>
    public void MarkDirty(Chunk c)
    {
        if (c == null) return;
        if (dirtyChunks.Contains(c)) return;
        dirtyChunks.Add(c);
    }

    /// <summary>
    /// Generates starting chunks
    /// </summary>
    public void Generate()
    {
        Clear();

        if (useRandomSeed)
        {
            shapeNoise.RandomizeSeed();
            detailNoise.RandomizeSeed();
            caveShapeNoise.RandomizeSeed();
            caveDetailNoise.RandomizeSeed();
        }

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                GenerateChunk(x, z);
            }
        }

        MakeMesh();
    }

    /// <summary>
    /// Generates a chunk without building its mesh, use this for preloading / initial generation
    /// </summary>
    /// <param name="_idx"></param>
    private void GenerateChunk(Vector2Int _idx)
    {
        var chunk = Instantiate(chunkPrefab.gameObject, transform).GetComponent<Chunk>();
        chunk.Initialize(_idx);
        chunks[_idx] = chunk;
    }

    /// <summary>
    /// Generates a chunk without building its mesh
    /// </summary>
    /// <param name="_idx"></param>
    private void GenerateChunk(int x, int z)
    {
        GenerateChunk(new Vector2Int(x, z));
    }

    /// <summary>
    /// Builds mesh of all chunks
    /// </summary>
    public void MakeMesh()
    {
        foreach (var chunk in chunks.Values)
        {
            chunk.MakeMesh();
        }
    }

    /// <summary>
    /// Destroys all chunks
    /// </summary>
    public void Clear()
    {
        chunks = new Dictionary<Vector2Int, Chunk>();

        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    /// <summary>
    /// Get minSurfaceLevel + perlin noise int for (x, z) * frequency offset by seed
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public int SurfaceNoise(float x, float z)
    {
        return (int)Mathf.Min(detailNoise.GetValue(x, z) * (shapeNoise.GetValue(x, z)) + minSurfaceLevel, Chunk.HEIGHT - 1);
    }

    /// <summary>
    /// Returns a "noise" for (x, y, z), needs a better noise library since this is mirrored around x = y = z and heavily weighted around 0.5f
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public float CaveNoise(float x, float y, float z)
    {
        float retVal = caveShapeNoise.GetValue(x, y) * caveDetailNoise.GetValue(x, y);
        retVal += caveShapeNoise.GetValue(y, z) * caveDetailNoise.GetValue(y, z);
        retVal += caveShapeNoise.GetValue(x, z) * caveDetailNoise.GetValue(x, z);
        retVal += caveShapeNoise.GetValue(y, x) * caveDetailNoise.GetValue(y, x);
        retVal += caveShapeNoise.GetValue(z, y) * caveDetailNoise.GetValue(z, y);
        retVal += caveShapeNoise.GetValue(z, x) * caveDetailNoise.GetValue(z, x);
        retVal /= 6f;
        return retVal / caveShapeNoise.amplitude / caveDetailNoise.amplitude;
    }

    /// <summary>
    /// Get the chunk v is in
    /// </summary>
    /// <param name="v">position in chunk (world pos)</param>
    /// <returns></returns>
    public Chunk GetChunk(Vector3 v)
    {
        return GetChunk(v.x, v.z);
    }

    /// <summary>
    /// Get the chunk at given xz coordinates
    /// </summary>
    /// <param name="x">position in chunk (world pos)</param>
    /// <param name="z">position in chunk (world pos)</param>
    /// <returns></returns>
    public Chunk GetChunk(float x, float z)
    {
        var chunkPos = new Vector2Int(Mathf.FloorToInt(x / 16), Mathf.FloorToInt(z / 16));
        if (!chunks.ContainsKey(chunkPos)) return null;
        return chunks[chunkPos];
    }

    /// <summary>
    /// Get the chunk with given index, returns null if not generated yet
    /// </summary>
    /// <param name="_idx">index of chunk (NOT world pos)</param>
    /// <returns></returns>
    public Chunk GetChunkByIdx(Vector2Int _idx)
    {
        if (!chunks.ContainsKey(_idx)) return null;
        return chunks[_idx];
    }

    /// <summary>
    /// Get the block at pos
    /// </summary>
    /// <param name="pos">position of block (world pos)</param>
    /// <returns></returns>
    public Block GetBlock(Vector3 pos)
    {
        var chunk = GetChunk(pos);
        if (!chunk) return null;
        return chunk.GetBlockByPos(pos);
    }

    /// <summary>
    /// Remove the block at pos
    /// </summary>
    /// <param name="pos">position of block to destroy (world pos)</param>
    /// <returns></returns>
    public bool DestroyBlock(Vector3 pos)
    {
        var chunk = GetChunk(pos);
        if (!chunk) return false;
        return chunk.DestroyBlock(pos);
    }

    /// <summary>
    /// Remove blocks at each position, rebuilds mesh of affected chunks only when finished
    /// </summary>
    /// <param name="positions">positions of block to destroy (world pos)</param>
    /// <returns></returns>
    public void DestroyBlocks(List<Vector3> positions, bool includeFluids = false)
    {
        HashSet<Chunk> affectedChunks = new HashSet<Chunk>();
        foreach (var pos in positions)
        {
            var chunk = GetChunk(pos);
            if (!chunk) continue;
            chunk.DestroyBlockSilent(pos, affectedChunks, includeFluids);
        }

        foreach (var chunk in affectedChunks)
        {
            chunk.MakeMesh();
        }
    }

    /// <summary>
    /// Gets chunk pos is in and lets it place block of given type at pos
    /// </summary>
    /// <param name="type">what block to place</param>
    /// <param name="pos">where to place the block</param>
    /// <returns></returns>
    public bool PlaceBlock(BlockType type, Vector3 pos)
    {
        if (pos.y <= 0) return false;
        if (pos.y >= Chunk.HEIGHT) return false;
        var chunk = GetChunk(pos);
        if (!chunk) return false;
        return chunk.PlaceBlock(type, pos);
    }
}

[System.Serializable]
public class Noise
{
    public float seed;

    public void RandomizeSeed()
    {
        seed = Random.Range(0f, 100000f);
    }

    [Range(0, 0.15f)]
    public float frequency;

    [Range(1, 50)]
    public float amplitude;

    public float GetValue(float x, float z)
    {
        return Mathf.PerlinNoise(x * frequency + seed, z * frequency + seed) * amplitude;
    }

}
