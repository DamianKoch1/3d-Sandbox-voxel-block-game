using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Chunk : ChunkMesh
{
    public const int SIZE = 16;

    public const int HEIGHT = 128;


    public Vector2Int Pos { get; private set; }

    public Block[,,] Blocks { get; private set; }

    [SerializeField]
    private ChunkMesh fluidMesh;

    [SerializeField]
    private ChunkMesh transparentMesh;


    public void Initialize(Vector2Int _pos)
    {
        Initialize();
        Pos = _pos;
        fluidMesh.Initialize();
        transparentMesh.Initialize();
        transform.position = new Vector3(Pos.x * SIZE, 0, Pos.y * SIZE);
        gameObject.name = "Chunk " + Pos;
    }

    /// <summary>
    /// Generates blocks / water using TerrainGenerator settings / noise
    /// </summary>
    public virtual Task Generate()
    {
        return Task.Run(() =>
        {
            Blocks = new Block[SIZE, HEIGHT, SIZE];
            var tg = TerrainGenerator.Instance;
            for (int x = 0; x < SIZE; x++)
            {
                for (int z = 0; z < SIZE; z++)
                {
                    var localSurfaceLevel = tg.SurfaceNoise(x + Pos.x * SIZE, z + Pos.y * SIZE);
                    for (int y = Mathf.Max(localSurfaceLevel, tg.config.waterLevel); y >= 0; y--)
                    {
                        Block block = null;
                        var blockPos = new Vector3Int(x + Pos.x * SIZE, y, z + Pos.y * SIZE);
                        if (y == 0) block = BlockFactory.Create(BlockType.BottomStone, blockPos);
                        else if (y > localSurfaceLevel) block = BlockFactory.Create(BlockType.Water, blockPos);
                        else if (y <= Mathf.Max(tg.config.waterLevel, localSurfaceLevel) - tg.config.minCaveSurfaceDistance
                            && tg.CaveNoise(x + Pos.x * SIZE, y, z + Pos.y * SIZE) > tg.config.caveNoiseThreshold)
                        {
                            if (y < tg.config.lavaLevel) block = BlockFactory.Create(BlockType.Lava, blockPos);
                            else
                            {
                                var above = Blocks[x, y + 1, z];
                                if (above is Fluid) block = BlockFactory.Create(above.Type, blockPos);
                                else continue;
                            }
                        }
                        else if (y == localSurfaceLevel)
                        {
                            if (y >= tg.config.waterLevel) block = BlockFactory.Create(BlockType.Grass, blockPos);
                            else block = BlockFactory.Create(BlockType.Dirt, blockPos);
                        }
                        else if (y > localSurfaceLevel - tg.config.dirtLayerSize) block = BlockFactory.Create(BlockType.Dirt, blockPos);
                        else if (y > 0) block = BlockFactory.Create(BlockType.Stone, blockPos);
                        Blocks[x, y, z] = block;
                    }
                }
            }
        });
    }

    /// <summary>
    /// Builds visible faces, fluid faces are added to child instead
    /// </summary>
    public virtual async Task BuildMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> UVs = new List<Vector2>();

        List<Vector3> fluidVertices = new List<Vector3>();
        List<int> fluidTriangles = new List<int>();
        List<Vector2> fluidUVs = new List<Vector2>();

        List<Vector3> transparentVertices = new List<Vector3>();
        List<int> transparentTriangles = new List<int>();
        List<Vector2> transparentUVs = new List<Vector2>();

        //reference to list to add to (solid / fluid / transparent)
        List<Vector3> vertexList;
        List<int> triList;
        List<Vector2> uvList;

        while (Blocks == null) await Generate();

        await Task.Run(() =>
        {
            for (int x = 0; x < SIZE; x++)
            {
                for (int z = 0; z < SIZE; z++)
                {
                    for (int y = 0; y < HEIGHT; y++)
                    {
                        var block = Blocks[x, y, z];
                        if (block == null) continue;
                        if (block is Fluid)
                        {
                            vertexList = fluidVertices;
                            triList = fluidTriangles;
                            uvList = fluidUVs;
                        }
                        else if (block is BlockTransparent)
                        {
                            vertexList = transparentVertices;
                            triList = transparentTriangles;
                            uvList = transparentUVs;
                        }
                        else
                        {
                            vertexList = vertices;
                            triList = triangles;
                            uvList = UVs;
                        }

                        var blockIdx = new Vector3(x, y, z);
                        int numFaces = 0;
                        Block neighbour;

                        if (z == 0)
                        {
                            neighbour = TerrainGenerator.Instance.GetBlock(block.Pos - Vector3.forward);
                        }
                        else
                        {
                            neighbour = Blocks[x, y, z - 1];
                        }

                        if (block.DrawFaceNextTo(Direction.South, neighbour))
                        {
                            foreach (var vertex in block.GetVertices(Direction.South, neighbour))
                                vertexList.Add(blockIdx + vertex);
                            uvList.AddRange(block.GetUVs(Direction.South));
                            numFaces++;
                        }


                        if (z == SIZE - 1)
                        {
                            neighbour = TerrainGenerator.Instance.GetBlock(block.Pos + Vector3.forward);
                        }
                        else
                        {
                            neighbour = Blocks[x, y, z + 1];
                        }

                        if (block.DrawFaceNextTo(Direction.North, neighbour))
                        {
                            foreach (var vertex in block.GetVertices(Direction.North, neighbour))
                                vertexList.Add(blockIdx + vertex);
                            uvList.AddRange(block.GetUVs(Direction.North));
                            numFaces++;
                        }


                        if (x == 0)
                        {
                            neighbour = TerrainGenerator.Instance.GetBlock(block.Pos - Vector3.right);
                        }
                        else
                        {
                            neighbour = Blocks[x - 1, y, z];
                        }

                        if (block.DrawFaceNextTo(Direction.West, neighbour))
                        {
                            foreach (var vertex in block.GetVertices(Direction.West, neighbour))
                                vertexList.Add(blockIdx + vertex);
                            uvList.AddRange(block.GetUVs(Direction.West));
                            numFaces++;
                        }


                        if (x == SIZE - 1)
                        {
                            neighbour = TerrainGenerator.Instance.GetBlock(block.Pos + Vector3.right);
                        }
                        else
                        {
                            neighbour = Blocks[x + 1, y, z];
                        }

                        if (block.DrawFaceNextTo(Direction.East, neighbour))
                        {
                            foreach (var vertex in block.GetVertices(Direction.East, neighbour))
                                vertexList.Add(blockIdx + vertex);
                            uvList.AddRange(block.GetUVs(Direction.East));
                            numFaces++;
                        }


                        if (y == 0)
                        {
                            neighbour = null;
                        }
                        else
                        {
                            neighbour = Blocks[x, y - 1, z];
                        }

                        if (block.DrawFaceNextTo(Direction.Down, neighbour))
                        {
                            foreach (var vertex in block.GetVertices(Direction.Down, neighbour))
                                vertexList.Add(blockIdx + vertex);
                            uvList.AddRange(block.GetUVs(Direction.Down));
                            numFaces++;
                        }

                        if (y == HEIGHT - 1)
                        {
                            neighbour = null;
                        }
                        else
                        {
                            neighbour = Blocks[x, y + 1, z];
                        }

                        if (block.DrawFaceNextTo(Direction.Up, neighbour))
                        {
                            foreach (var vertex in block.GetVertices(Direction.Up, neighbour))
                                vertexList.Add(blockIdx + vertex);
                            uvList.AddRange(block.GetUVs(Direction.Up));
                            numFaces++;
                        }

                        int triangleIdx = vertexList.Count - numFaces * 4;
                        for (int i = 0; i < numFaces; i++)
                        {
                            int i4 = i * 4;
                            triList.AddRange(new int[]
                            {
                            triangleIdx + i4, triangleIdx + i4 + 1, triangleIdx + i4 + 2,
                            triangleIdx + i4, triangleIdx + i4 + 2, triangleIdx + i4 + 3
                            });
                        }
                    }
                }
            }
        }
        );
        fluidMesh.ApplyMesh(fluidVertices, fluidTriangles, fluidUVs);

        transparentMesh.ApplyMesh(transparentVertices, transparentTriangles, transparentUVs);

        ApplyMesh(vertices, triangles, UVs);
    }

    #region Utils
    /// <summary>
    /// Get the block index (local, 0 - (SIZE-1)) of this chunk at v
    /// </summary>
    /// <param name="v">position of block to get local index for (world pos)</param>
    /// <returns></returns>
    public Vector3Int GetBlockIdx(Vector3 v)
    {
        Vector3Int vi = Vector3Int.zero;
        vi.x = Mathf.FloorToInt(v.x) - Pos.x * SIZE;
        vi.y = Mathf.FloorToInt(v.y);
        vi.z = Mathf.FloorToInt(v.z) - Pos.y * SIZE;
        return vi;
    }

    /// <summary>
    /// Get the block in this chunk at _pos, calculate the chunk of _pos before calling this to make sure _pos is in this chunk
    /// </summary>
    /// <param name="_pos">position of block within this chunk (world pos)</param>
    /// <returns></returns>
    public Block GetBlockByPos(Vector3 _pos)
    {
        var idx = GetBlockIdx(_pos);
        if (idx.y < 0) return null;
        if (idx.y >= HEIGHT) return null;
        return Blocks[idx.x, idx.y, idx.z];
    }

    /// <summary>
    /// Does this chunk have a block at given index?
    /// </summary>
    /// <param name="idx">local index of block</param>
    /// <returns></returns>
    private bool IsValidBlockIdx(Vector3Int idx)
    {
        return Blocks[idx.x, idx.y, idx.z] != null;
    }
    #endregion

    #region Place
    /// <summary>
    /// Places block of given type at _pos if possible, if successful rebuilds mesh, if at chunk border rebuilds neighbour mesh aswell
    /// </summary>
    /// <param name="type">what block to place</param>
    /// <param name="_pos">where to place block (world pos)</param>
    /// <returns>returns placed block, returns null if spot is blocked by player / occupied by solid block</returns>
    public Block PlaceBlock(BlockType type, Vector3 _pos, bool ignoreEntities = false)
    {
        var idx = GetBlockIdx(_pos);
        var blockPos = Vector3Int.FloorToInt(_pos);
        var block = Blocks[idx.x, idx.y, idx.z];
        if (block != null)
        {
            if (block is Fluid) block.OnDestroyed();
            else return null;
        }
        var newBlock = BlockFactory.Create(type, blockPos);
        if (!ignoreEntities && !newBlock.CanPlaceInEntity && Physics.CheckBox(blockPos + Vector3.one * 0.5f, Vector3.one * 0.45f)) return null;
        Blocks[idx.x, idx.y, idx.z] = newBlock;
        newBlock.OnPlaced();
        BuildMesh();
        UpdateAdjacentChunks(idx);
        return newBlock;
    }

    /// <summary>
    /// Places block of given type at _pos if possible, does not rebuild meshes but stores affected chunks, useful for placing multiple blocks at once
    /// </summary>
    /// <param name="type">what block to place</param>
    /// <param name="_pos">where to place block (world pos)</param>
    /// <param name="affectedChunks">hashset to store affected chunks in</param>
    /// <returns>returns false if spot is blocked by player / occupied by solid block</returns>
    public Block PlaceBlockSilent(BlockType type, Vector3 _pos, HashSet<Chunk> affectedChunks)
    {
        var idx = GetBlockIdx(_pos);
        var blockPos = Vector3Int.FloorToInt(_pos);
        var block = Blocks[idx.x, idx.y, idx.z];
        if (!(block is Fluid))
        {
            if (block != null) return null;
        }
        Blocks[idx.x, idx.y, idx.z] = BlockFactory.Create(type, blockPos);
        Blocks[idx.x, idx.y, idx.z].OnPlaced();
        affectedChunks.Add(this);
        var tg = TerrainGenerator.Instance;
        if (idx.x == 0) affectedChunks.Add(tg.GetChunkByIdx(Pos + Vector2Int.left));
        else if (idx.x == SIZE - 1) affectedChunks.Add(tg.GetChunkByIdx(Pos + Vector2Int.right));
        if (idx.z == 0) affectedChunks.Add(tg.GetChunkByIdx(Pos + Vector2Int.down));
        else if (idx.z == SIZE - 1) affectedChunks.Add(tg.GetChunkByIdx(Pos + Vector2Int.up));
        return Blocks[idx.x, idx.y, idx.z];
    }
    #endregion

    #region Destroy
    /// <summary>
    /// Destroys block at _pos, if successful rebuilds mesh, if at chunk border rebuilds neighbour mesh aswell
    /// </summary>
    /// <param name="_pos">position of block to destroy (world pos)</param>
    /// <returns></returns>
    public bool DestroyBlock(Vector3 _pos)
    {
        var idx = GetBlockIdx(_pos);
        if (!IsValidBlockIdx(idx)) return false;
        Blocks[idx.x, idx.y, idx.z].OnDestroyed();
        Blocks[idx.x, idx.y, idx.z] = null;
        BuildMesh();
        UpdateAdjacentChunks(idx);
        return true;
    }

    /// <summary>
    /// Destroys block at _pos, does not rebuild meshes but stores affected chunks, useful for destroying multiple blocks at once
    /// </summary>
    /// <param name="_pos">position of block to destroy (world pos)</param>
    /// <param name="affectedChunks">hashset to store affected chunks in</param>
    /// <param name="includeFluids">determines whether fluids should get destroyed aswell as solid blocks</param>
    /// <returns></returns>
    public bool DestroyBlockSilent(Vector3 _pos, HashSet<Chunk> affectedChunks, bool includeFluids)
    {
        var idx = GetBlockIdx(_pos);
        if (!IsValidBlockIdx(idx)) return false;
        if (!includeFluids)
        {
            if (Blocks[idx.x, idx.y, idx.z] is Fluid) return false;
        }
        Blocks[idx.x, idx.y, idx.z].OnDestroyed();
        Blocks[idx.x, idx.y, idx.z] = null;
        affectedChunks.Add(this);
        var tg = TerrainGenerator.Instance;
        if (idx.x == 0) affectedChunks.Add(tg.GetChunkByIdx(Pos + Vector2Int.left));
        else if (idx.x == SIZE - 1) affectedChunks.Add(tg.GetChunkByIdx(Pos + Vector2Int.right));
        if (idx.z == 0) affectedChunks.Add(tg.GetChunkByIdx(Pos + Vector2Int.down));
        else if (idx.z == SIZE - 1) affectedChunks.Add(tg.GetChunkByIdx(Pos + Vector2Int.up));
        return true;
    }
    #endregion

    /// <summary>
    /// If blockIdx is at borders of this chunk, rebuild the mesh of adjacent ones to it
    /// </summary>
    /// <param name="blockIdx">index of block that was updated</param>
    private void UpdateAdjacentChunks(Vector3Int blockIdx)
    {
        var tg = TerrainGenerator.Instance;
        if (blockIdx.x == 0) tg.GetChunkByIdx(Pos + Vector2Int.left)?.BuildMesh();
        else if (blockIdx.x == SIZE - 1) tg.GetChunkByIdx(Pos + Vector2Int.right)?.BuildMesh();
        if (blockIdx.z == 0) tg.GetChunkByIdx(Pos + Vector2Int.down)?.BuildMesh();
        else if (blockIdx.z == SIZE - 1) tg.GetChunkByIdx(Pos + Vector2Int.up)?.BuildMesh();
    }

    public override int GetHashCode()
    {
        return Pos.GetHashCode();
    }

    private void OnDrawGizmosSelected()
    {
        if (!TerrainGenerator.Instance.drawGizmos) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(new Vector3(Pos.x, 0, Pos.y) * SIZE + new Vector3(SIZE, HEIGHT, SIZE) * 0.5f, new Vector3(SIZE, HEIGHT, SIZE));
    }
}
