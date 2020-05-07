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

    [Range(0, 100)]
    public int amplitude;

    [Range(0, 100)]
    public int minSurfaceLevel = 50;

    [Range(0, 70)]
    public int waterLevel = 60;

    [Range(0, 10)]
    public int renderDistance = 5;

    public bool useRandomSeed;

    public float seed;

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
        RebuildDirtyChunks();
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
                chunk.gameObject.SetActive(true);
                continue;
            }
            chunk.gameObject.SetActive(false);
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

    private void RebuildDirtyChunks()
    {
        if (dirtyChunks.Count == 0) return;
        foreach (var chunk in dirtyChunks)
        {
            chunk.MakeMesh();
        }
        dirtyChunks.Clear();
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

    private void MarkDirty(Chunk c)
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
            seed = Random.Range(0f, 10000f);
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
    public int PerlinNoise(float x, float z)
    {
        return (int)(Mathf.PerlinNoise(x * frequency + seed, z * frequency + seed) * amplitude) + minSurfaceLevel;
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
        var chunk = GetChunk(pos.x, pos.z);
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
        var chunk = GetChunk(pos.x, pos.z);
        if (!chunk) return false;
        return chunk.DestroyBlock(pos);
    }

    /// <summary>
    /// Places default block at pos
    /// </summary>
    /// <param name="pos">where to place the block</param>
    /// <returns></returns>
    public bool PlaceBlock(Vector3 pos)
    {
        if (pos.y <= 0) return false;
        if (pos.y >= Chunk.HEIGHT) return false;
        var chunk = GetChunk(pos.x, pos.z);
        if (!chunk) return false;
        return chunk.PlaceBlock(pos);
    }
}
