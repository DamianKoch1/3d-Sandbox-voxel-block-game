using System.Collections.Generic;
using System.Threading.Tasks;
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

    [SerializeField]
    private Biome biome;

    public Biome CurrentBiome => biome;


    [Header("Preview settings")]
    public bool updateSurfaceSample;
    public bool updateCaveSample;

    [SerializeField, Range(100, 256)]
    private int noiseSampleSize = 128;

    [SerializeField, Range(1, 10)]
    private int noiseUnitPerPix = 4;


    [SerializeField]
    private int caveNoiseSampleZ = 0;

    private Player player;

    [SerializeField]
    private Vector2Int playerChunkPos;

    private HashSet<Chunk> dirtyChunks;

    private async void Start()
    {
        if (!Instance) instance = this;
        await Generate();
        player = FindObjectOfType<Player>();
        player.Initialize();
        playerChunkPos = GetChunk(player.transform.position).Pos;
        dirtyChunks = new HashSet<Chunk>();
        InvokeRepeating(nameof(UpdatePlayerPos), 1, 1);
        RebuildDirtyChunks();
    }

    private async void RebuildDirtyChunks()
    {
        while (!ThreadingUtils.QuitToken.IsCancellationRequested)
        {
            await Task.Delay((int)(meshBuildingInterval * 1000));
            await RebuildDirtyChunk();
        }
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
            if (Mathf.Abs(chunk.Pos.x - playerChunkPos.x) <= renderDistance && Mathf.Abs(chunk.Pos.y - playerChunkPos.y) <= renderDistance)
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
    private async Task RebuildDirtyChunk()
    {
        if (dirtyChunks.Count == 0) return;
        Chunk closest = null;
        float dist = 10000000f;
        foreach (var chunk in dirtyChunks)
        {
            var newDist = (chunk.Pos - playerChunkPos).sqrMagnitude;
            if (newDist >= dist) continue;
            dist = newDist;
            closest = chunk;
        }
        dirtyChunks.Remove(closest);
        await closest.BuildMesh();
    }

    /// <summary>
    /// Adds a chunk, marks itself and its neighbours dirty, use this when adding chunks at runtime
    /// </summary>
    /// <param name="_idx"></param>
    private void AddChunk(Vector2Int _idx)
    {
        MarkDirty(GenerateChunk(_idx));
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
    public async Task Generate()
    {
        Clear();

        if (useRandomSeeds)
        {
            biome.surfaceNoise.RandomizeSeed();
            biome.caveNoise.RandomizeSeed();
        }

        for (int x = -renderDistance; x < renderDistance; x++)
        {
            for (int z = -renderDistance; z < renderDistance; z++)
            {
                GenerateChunk(x, z);
            }
        }

        await BuildMesh();
    }

    /// <summary>
    /// Generates a chunk without building its mesh, use this for preloading / initial generation
    /// </summary>
    /// <param name="_idx"></param>
    private Chunk GenerateChunk(Vector2Int _idx)
    {
        var chunk = Instantiate(chunkPrefab, transform);
        chunk.Initialize(_idx);
        chunk.Generate();
        chunks[_idx] = chunk;
        return chunk;
    }

    /// <summary>
    /// Generates a chunk without building its mesh
    /// </summary>
    /// <param name="_idx"></param>
    private Chunk GenerateChunk(int x, int z)
    {
        return GenerateChunk(new Vector2Int(x, z));
    }

    /// <summary>
    /// Builds mesh of all chunks
    /// </summary>
    public async Task BuildMesh()
    {
        foreach (var chunk in chunks.Values)
        {
            await chunk.BuildMesh();
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
        return (int)Mathf.Min(biome.surfaceNoise.GetValue(x, z) + biome.minSurfaceLevel, Chunk.HEIGHT - 1);
    }

    /// <summary>
    /// Returns a noise for (x, y, z)
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public float CaveNoise(float x, float y, float z)
    {
        return biome.caveNoise.GetValue(x, y, z);
    }

    #region Utils

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
        var chunkIdx = GetChunkIdx(x, z);
        if (!chunks.ContainsKey(chunkIdx)) return null;
        return chunks[chunkIdx];
    }

    private Vector2Int GetChunkIdx(float x, float z) => new Vector2Int(Mathf.FloorToInt(x / Chunk.SIZE), Mathf.FloorToInt(z / Chunk.SIZE));

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
    #endregion

    #region Place
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
        if (!chunk) chunk = GenerateChunk(GetChunkIdx(pos.x, pos.z));
        return chunk.PlaceBlock(type, pos, ignoreEntities);
    }

    public Block PlaceBlockSilent(BlockType type, Vector3 pos, HashSet<Chunk> affectedChunks)
    {
        var chunk = GetChunk(pos);
        if (!chunk) chunk = GenerateChunk(GetChunkIdx(pos.x, pos.z));
        return chunk.PlaceBlockSilent(type, pos, affectedChunks);
    }
    #endregion

    #region Destroy
    /// <summary>
    /// Remove the block at pos
    /// </summary>
    /// <param name="pos">position of block to destroy (world pos)</param>
    /// <returns></returns>
    public bool DestroyBlock(Vector3 pos)
    {
        var chunk = GetChunk(pos);
        if (!chunk) GenerateChunk(GetChunkIdx(pos.x, pos.z));
        return chunk.DestroyBlock(pos);
    }

    public bool DestroyBlockSilent(Vector3 pos, HashSet<Chunk> affectedChunks)
    {
        var chunk = GetChunk(pos);
        if (!chunk) GenerateChunk(GetChunkIdx(pos.x, pos.z));
        return GetChunk(pos).DestroyBlockSilent(pos, affectedChunks, true) == true;
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
            if (!chunk) GenerateChunk(GetChunkIdx(pos.x, pos.z));
            chunk.DestroyBlockSilent(pos, affectedChunks, includeFluids);
        }

        foreach (var chunk in affectedChunks)
        {
            chunk.BuildMesh();
        }
    }
    #endregion

    #region Preview
    private Texture2D surfaceSample, treeSample, caveSample;

    public Texture2D GetSurfaceNoiseSampleTex()
    {
        if (surfaceSample != null && !updateSurfaceSample) return surfaceSample;

        var size = noiseSampleSize - noiseSampleSize % noiseUnitPerPix;
        int offset = size / 2;
        surfaceSample = new Texture2D(size, size, TextureFormat.RGB24, false);
        for (int x = 0; x < size; x += noiseUnitPerPix)
        {
            for (int y = 0; y < size; y += noiseUnitPerPix)
            {
                var surface = SurfaceNoise(x - offset, y - offset);
                float sample = (float)(surface - biome.minSurfaceLevel) / (Chunk.HEIGHT - 1 - biome.minSurfaceLevel);
                var c = new Color(sample, surface >= biome.surfaceFluidLevel ? sample + 0.3f : sample, surface >= biome.surfaceFluidLevel ? sample : sample + 0.3f);
                for (int x1 = 0; x1 < noiseUnitPerPix; x1++)
                    for (int y1 = 0; y1 < noiseUnitPerPix; y1++)
                        surfaceSample.SetPixel(x + x1, y + y1, c);
            }
        }
        surfaceSample.Apply();
        return surfaceSample;
    }

    public Texture2D GetCaveNoiseSampleTex()
    {
        if (caveSample != null && !updateCaveSample) return caveSample;
        var size = noiseSampleSize - noiseSampleSize % noiseUnitPerPix;
        int offset = size / 2;
        caveSample = new Texture2D(size, size, TextureFormat.RGB24, false);
        for (int x = 0; x < size; x += noiseUnitPerPix)
        {
            for (int y = 0; y < size; y += noiseUnitPerPix)
            {
                float sample = CaveNoise(x - offset, y - offset, caveNoiseSampleZ);
                var c = Color.black;
                if (sample > biome.caveNoiseThreshold)
                {
                    c = Color.white;
                }
                for (int x1 = 0; x1 < noiseUnitPerPix; x1++)
                    for (int y1 = 0; y1 < noiseUnitPerPix; y1++)
                        caveSample.SetPixel(x + x1, y + y1, c);
            }
        }
        caveSample.Apply();
        return caveSample;
    }
    #endregion
}
