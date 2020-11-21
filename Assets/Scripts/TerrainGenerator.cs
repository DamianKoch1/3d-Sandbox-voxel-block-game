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

    [SerializeField]
    private Chunk chunkPrefab;

    [SerializeField, Range(0.01f, 0.5f)]
    private float meshBuildingInterval = 0.1f;

    [SerializeField, Tooltip("Should far chunks get unloaded?")]
    private bool unloadChunks;

    public Dictionary<Vector2Int, Chunk> chunks;

    [SerializeField, Range(0, 10)]
    private int renderDistance = 5;

    public bool drawGizmos;

    [SerializeField, Space]
    private bool useRandomSeeds;

    public TerrainGeneratorConfig config;


    [Header("Preview settings")]
    public bool showSurfaceNoiseSample;

    [SerializeField, Range(100, 256)]
    private int noiseSampleSize = 128;

    [SerializeField, Range(1, 10)]
    private int noisePixPerUnit = 4;

    public bool showCaveNoiseSample;

    [SerializeField]
    private int caveNoiseSampleZ = 0;

    private Player player;

    [SerializeField]
    private Vector2Int playerChunkPos;

    private List<Chunk> dirtyChunks;

    private void Start()
    {
        Generate();
        player = FindObjectOfType<Player>();
        player.Initialize();
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
        var newChunkPos = new Vector2Int(Mathf.FloorToInt(player.transform.position.x / Chunk.SIZE), Mathf.FloorToInt(player.transform.position.z / Chunk.SIZE));
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
        dirtyChunks[0].BuildMesh();
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

        if (useRandomSeeds)
        {
            config.surfaceNoise.RandomizeSeed();
            config.caveNoise.RandomizeSeed();
        }

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                GenerateChunk(x, z);
            }
        }

        BuildMesh();
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
    public void BuildMesh()
    {
        foreach (var chunk in chunks.Values)
        {
            chunk.BuildMesh();
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
        return (int)Mathf.Min(config.surfaceNoise.GetValue(x, z) + config.minSurfaceLevel, Chunk.HEIGHT - 1);
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
        return config.caveNoise.GetValue(x, y, z);
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
        var chunkPos = new Vector2Int(Mathf.FloorToInt(x / Chunk.SIZE), Mathf.FloorToInt(z / Chunk.SIZE));
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
            chunk.BuildMesh();
        }
    }

    public Block PlaceBlockSilent(BlockType type, Vector3 _pos, HashSet<Chunk> affectedChunks)
    {
        return GetChunk(_pos).PlaceBlockSilent(type, _pos, affectedChunks);
    }

    /// <summary>
    /// Gets chunk pos is in and lets it place block of given type at pos
    /// </summary>
    /// <param name="type">what block to place</param>
    /// <param name="pos">where to place the block</param>
    /// <returns></returns>
    public Block PlaceBlock(BlockType type, Vector3 pos, bool ignoreEntities = false)
    {
        if (pos.y <= 0) return null;
        if (pos.y >= Chunk.HEIGHT) return null;
        var chunk = GetChunk(pos);
        if (!chunk) return null;
        return chunk.PlaceBlock(type, pos, ignoreEntities);
    }

    public Texture2D GetSurfaceNoiseSampleTex()
    {
        var size = noiseSampleSize - noiseSampleSize % noisePixPerUnit;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);
        for (int x = 0; x < size; x += noisePixPerUnit)
        {
            for (int y = 0; y < size; y += noisePixPerUnit)
            {
                var surface = SurfaceNoise(x, y);
                float sample = (float)(surface - config.minSurfaceLevel) / (Chunk.HEIGHT - 1 - config.minSurfaceLevel);
                var c = new Color(sample, surface >= config.waterLevel ? sample + 0.3f : sample, surface >= config.waterLevel ? sample : sample + 0.3f);
                for (int x1 = 0; x1 < noisePixPerUnit; x1++)
                    for (int y1 = 0; y1 < noisePixPerUnit; y1++)
                        tex.SetPixel(x + x1, y + y1, c);
            }
        }
        tex.Apply();
        return tex;
    }

    public Texture2D GetCaveNoiseSampleTex()
    {
        var size = noiseSampleSize - noiseSampleSize % noisePixPerUnit;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);
        for (int x = 0; x < size; x += noisePixPerUnit)
        {
            for (int y = 0; y < size; y += noisePixPerUnit)
            {
                float sample = CaveNoise(x, y, caveNoiseSampleZ);
                var c = Color.black;
                if (sample > config.caveNoiseThreshold)
                {
                    c = Color.white;
                }
                for (int x1 = 0; x1 < noisePixPerUnit; x1++)
                    for (int y1 = 0; y1 < noisePixPerUnit; y1++)
                        tex.SetPixel(x+x1, y+y1, c);
            }
        }
        tex.Apply();
        return tex;
    }
}

[System.Serializable]
public class Noise
{
    public long seed;

    [Range(0, 0.15f)]
    public float frequency = 0.01f;

    [Range(1, 50)]
    public float amplitude = 1;

    [SerializeField] private NoiseLayer[] layers;

    private SimplexNoise sNoise;

    private SimplexNoise SNoise
    {
        get
        {
            if (sNoise?.Seed != seed) sNoise = new SimplexNoise(seed);
            return sNoise;
        }
    }

    public void RandomizeSeed()
    {
        seed = (long)Random.Range(0f, 100000f);
    }

    public float GetValue(float x, float z)
    {
        var retVal = SNoise.Evaluate(x * frequency, z * frequency) * amplitude;
        foreach (NoiseLayer layer in layers) retVal += layer.GetValue(SNoise, x, z);
        return retVal;
    }

    public float GetValue(float x, float y, float z)
    {
        var retVal = SNoise.Evaluate(x * frequency, y * frequency, z * frequency) * amplitude;
        foreach (NoiseLayer layer in layers) retVal += layer.GetValue(SNoise, x, y, z);
        return retVal;
    }

    [System.Serializable]
    private class NoiseLayer
    {
        [SerializeField, Range(0, 1)]
        private float weight = 1;

        [SerializeField, Range(0, 0.3f)]
        private float frequency = 0.01f;

        [SerializeField, Range(0, 10)]
        private float amplitude = 1;

        public float GetValue(SimplexNoise n, float x, float z) => (n.Evaluate(x * frequency, z * frequency) * amplitude * 2 - amplitude) * weight;

        public float GetValue(SimplexNoise n, float x, float y, float z) => (n.Evaluate(x * frequency, y * frequency, z * frequency) * amplitude * 2 - amplitude) * weight;
    }

}
