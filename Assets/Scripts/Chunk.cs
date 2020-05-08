using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : ChunkMesh
{
    public const int SIZE = 16;

    public const int HEIGHT = 128;


    public Vector2Int pos;

    public Block[,,] blocks;

    [SerializeField]
    private ChunkMesh fluidMesh;

    [SerializeField]
    private ChunkMesh transparentMesh;


    public void Initialize(Vector2Int _pos)
    {
        Initialize();
        pos = _pos;
        fluidMesh.Initialize();
        transparentMesh.Initialize();
        transform.position = new Vector3(pos.x * SIZE, 0, pos.y * SIZE);
        gameObject.name = "Chunk " + pos;
        Generate();
    }

    /// <summary>
    /// Generates blocks / water using TerrainGenerator settings / noise
    /// </summary>
    public virtual void Generate()
    {
        blocks = new Block[SIZE, HEIGHT, SIZE];
        var tg = TerrainGenerator.Instance;
        for (int x = 0; x < SIZE; x++)
        {
            for (int z = 0; z < SIZE; z++)
            {
                var localSurfaceLevel = tg.SurfaceNoise(x + pos.x * SIZE, z + pos.y * SIZE);
                for (int y = 0; y <= Mathf.Max(localSurfaceLevel, tg.waterLevel); y++)
                {
                    Block block = null;
                    var blockPos = new Vector3Int(x + pos.x * SIZE, y, z + pos.y * SIZE);
                    if (y == 0) block = BlockFactory.Create(BlockType.bottomStone, blockPos);
                    else if (y > localSurfaceLevel) block = BlockFactory.Create(BlockType.water, blockPos);
                    else if (tg.CaveNoise(x + pos.x * SIZE, y, z + pos.y * SIZE) * (1 - (y / (localSurfaceLevel - tg.minCaveSurfaceDistance))) > tg.caveNoiseThreshold) continue;
                    else if (y == localSurfaceLevel)
                    {
                        if (y >= tg.waterLevel) block = BlockFactory.Create(BlockType.grass, blockPos);
                        else block = BlockFactory.Create(BlockType.dirt, blockPos);
                    }
                    else if (y > localSurfaceLevel - tg.dirtLayerSize) block = BlockFactory.Create(BlockType.dirt, blockPos);
                    else if (y > 0) block = BlockFactory.Create(BlockType.stone, blockPos);
                    blocks[x, y, z] = block;
                }
            }
        }
    }

    /// <summary>
    /// Builds visible faces, fluid faces are added to child instead
    /// </summary>
    public virtual void MakeMesh()
    {
        mesh.Clear();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> UVs = new List<Vector2>();

        List<Vector3> fluidVertices = new List<Vector3>();
        List<int> fluidTriangles = new List<int>();
        List<Vector2> fluidUVs = new List<Vector2>();

        List<Vector3> transparentVertices = new List<Vector3>();
        List<int> transparentTriangles = new List<int>();
        List<Vector2> transparentUVs = new List<Vector2>();

        List<Vector3> vertexList = vertices;
        List<int> triList = triangles;
        List<Vector2> uvList = UVs;

        for (int x = 0; x < SIZE; x++)
        {
            for (int z = 0; z < SIZE; z++)
            {
                for (int y = 0; y < HEIGHT; y++)
                {
                    var block = blocks[x, y, z];
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

                    var blockPos = new Vector3(x, y, z);
                    int numFaces = 0;
                    Block neighbour;

                    if (z == 0)
                    {
                        neighbour = TerrainGenerator.Instance.GetBlock(block.pos - Vector3.forward);
                    }
                    else
                    {
                        neighbour = blocks[x, y, z - 1];
                    }

                    if (block.DrawFaceNextTo(neighbour))
                    {
                        //front
                        vertexList.Add(blockPos + new Vector3(0, 0, 0));
                        vertexList.Add(blockPos + new Vector3(0, 1, 0));
                        vertexList.Add(blockPos + new Vector3(1, 1, 0));
                        vertexList.Add(blockPos + new Vector3(1, 0, 0));
                        numFaces++;

                        uvList.AddRange(block.GetSideUVs());
                    }


                    if (z == SIZE - 1)
                    {
                        neighbour = TerrainGenerator.Instance.GetBlock(block.pos + Vector3.forward);
                    }
                    else
                    {
                        neighbour = blocks[x, y, z + 1];
                    }

                    if (block.DrawFaceNextTo(neighbour))
                    {
                        //back
                        vertexList.Add(blockPos + new Vector3(1, 0, 1));
                        vertexList.Add(blockPos + new Vector3(1, 1, 1));
                        vertexList.Add(blockPos + new Vector3(0, 1, 1));
                        vertexList.Add(blockPos + new Vector3(0, 0, 1));
                        numFaces++;

                        uvList.AddRange(block.GetSideUVs());
                    }


                    if (x == 0)
                    {
                        neighbour = TerrainGenerator.Instance.GetBlock(block.pos - Vector3.right);
                    }
                    else
                    {
                        neighbour = blocks[x - 1, y, z];
                    }

                    if (block.DrawFaceNextTo(neighbour))
                    {
                        //left
                        vertexList.Add(blockPos + new Vector3(0, 0, 1));
                        vertexList.Add(blockPos + new Vector3(0, 1, 1));
                        vertexList.Add(blockPos + new Vector3(0, 1, 0));
                        vertexList.Add(blockPos + new Vector3(0, 0, 0));
                        numFaces++;

                        uvList.AddRange(block.GetSideUVs());
                    }


                    if (x == SIZE - 1)
                    {
                        neighbour = TerrainGenerator.Instance.GetBlock(block.pos + Vector3.right);
                    }
                    else
                    {
                        neighbour = blocks[x + 1, y, z];
                    }

                    if (block.DrawFaceNextTo(neighbour))
                    {
                        //right
                        vertexList.Add(blockPos + new Vector3(1, 0, 0));
                        vertexList.Add(blockPos + new Vector3(1, 1, 0));
                        vertexList.Add(blockPos + new Vector3(1, 1, 1));
                        vertexList.Add(blockPos + new Vector3(1, 0, 1));
                        numFaces++;

                        uvList.AddRange(block.GetSideUVs());
                    }


                    if (y == 0)
                    {
                        neighbour = null;
                    }
                    else
                    {
                        neighbour = blocks[x, y - 1, z];
                    }

                    if (block.DrawFaceNextTo(neighbour))
                    {
                        //bottom
                        vertexList.Add(blockPos + new Vector3(0, 0, 0));
                        vertexList.Add(blockPos + new Vector3(1, 0, 0));
                        vertexList.Add(blockPos + new Vector3(1, 0, 1));
                        vertexList.Add(blockPos + new Vector3(0, 0, 1));
                        numFaces++;

                        uvList.AddRange(block.GetBottomUVs());
                    }

                    if (y == HEIGHT - 1)
                    {
                        neighbour = null;
                    }
                    else
                    {
                        neighbour = blocks[x, y + 1, z];
                    }

                    if (block.DrawFaceNextTo(neighbour))
                    {
                        //top
                        vertexList.Add(blockPos + new Vector3(0, 1, 0));
                        vertexList.Add(blockPos + new Vector3(0, 1, 1));
                        vertexList.Add(blockPos + new Vector3(1, 1, 1));
                        vertexList.Add(blockPos + new Vector3(1, 1, 0));
                        numFaces++;

                        uvList.AddRange(block.GetTopUVs());
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
        fluidMesh.ApplyMesh(fluidVertices, fluidTriangles, fluidUVs);

        transparentMesh.ApplyMesh(transparentVertices, transparentTriangles, transparentUVs);

        ApplyMesh(vertices, triangles, UVs);
    }

    /// <summary>
    /// Get the block index (local, 0 - (SIZE-1)) of this chunk at v
    /// </summary>
    /// <param name="v">position of block to get local index for (world pos)</param>
    /// <returns></returns>
    public Vector3Int GetBlockIdx(Vector3 v)
    {
        Vector3Int vi = Vector3Int.zero;
        vi.x = Mathf.FloorToInt(v.x) - pos.x * SIZE;
        vi.y = Mathf.FloorToInt(v.y);
        vi.z = Mathf.FloorToInt(v.z) - pos.y * SIZE;
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
        return blocks[idx.x, idx.y, idx.z];
    }

    /// <summary>
    /// Destroys block at _pos, if successful rebuilds mesh, if at chunk border rebuilds neighbour mesh aswell
    /// </summary>
    /// <param name="_pos">position of block to destroy (world pos)</param>
    /// <returns></returns>
    public bool DestroyBlock(Vector3 _pos)
    {
        var idx = GetBlockIdx(_pos);
        if (!IsValidBlockIdx(idx)) return false;
        blocks[idx.x, idx.y, idx.z].OnDestroyed();
        blocks[idx.x, idx.y, idx.z] = null;
        MakeMesh();
        UpdateAdjacentChunks(idx);
        return true;
    }

    /// <summary>
    /// Does this chunk have a block at given index?
    /// </summary>
    /// <param name="idx">local index of block</param>
    /// <returns></returns>
    private bool IsValidBlockIdx(Vector3Int idx)
    {
        return blocks[idx.x, idx.y, idx.z] != null;
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
            if (blocks[idx.x, idx.y, idx.z] is Fluid) return false;
        }
        blocks[idx.x, idx.y, idx.z].OnDestroyed();
        blocks[idx.x, idx.y, idx.z] = null;
        affectedChunks.Add(this);
        var tg = TerrainGenerator.Instance;
        if (idx.x == 0)             affectedChunks.Add(tg.GetChunkByIdx(pos + Vector2Int.left));
        else if (idx.x == SIZE - 1) affectedChunks.Add(tg.GetChunkByIdx(pos + Vector2Int.right));
        if (idx.z == 0)             affectedChunks.Add(tg.GetChunkByIdx(pos + Vector2Int.down));
        else if (idx.z == SIZE - 1) affectedChunks.Add(tg.GetChunkByIdx(pos + Vector2Int.up));
        return true;
    }

    /// <summary>
    /// Places block of given type at _pos if possible, does not rebuild meshes but stores affected chunks, useful for placing multiple blocks at once
    /// </summary>
    /// <param name="type">what block to place</param>
    /// <param name="_pos">where to place block (world pos)</param>
    /// <param name="affectedChunks">hashset to store affected chunks in</param>
    /// <returns>returns false if spot is blocked by player / occupied by solid block</returns>
    public bool PlaceBlockSilent(BlockType type, Vector3 _pos, HashSet<Chunk> affectedChunks)
    {
        var idx = GetBlockIdx(_pos);
        var blockPos = Vector3Int.FloorToInt(_pos);
        var block = blocks[idx.x, idx.y, idx.z];
        if (!(block is Fluid))
        {
            if (block != null) return false;
        }
        blocks[idx.x, idx.y, idx.z] = BlockFactory.Create(type, blockPos);
        blocks[idx.x, idx.y, idx.z].OnPlaced();
        MakeMesh();
        UpdateAdjacentChunks(idx);
        return true;
    }

    /// <summary>
    /// If blockIdx is at borders of this chunk, rebuild the mesh of adjacent ones to it
    /// </summary>
    /// <param name="blockIdx">index of block that was updated</param>
    private void UpdateAdjacentChunks(Vector3Int blockIdx)
    {
        var tg = TerrainGenerator.Instance;
        if (blockIdx.x == 0)                tg.GetChunkByIdx(pos + Vector2Int.left)?.MakeMesh();
        else if (blockIdx.x == SIZE - 1)    tg.GetChunkByIdx(pos + Vector2Int.right)?.MakeMesh();
        if (blockIdx.z == 0)                tg.GetChunkByIdx(pos + Vector2Int.down)?.MakeMesh();
        else if (blockIdx.z == SIZE - 1)    tg.GetChunkByIdx(pos + Vector2Int.up)?.MakeMesh();
    }

    /// <summary>
    /// Places block of given type at _pos if possible, if successful rebuilds mesh, if at chunk border rebuilds neighbour mesh aswell
    /// </summary>
    /// <param name="type">what block to place</param>
    /// <param name="_pos">where to place block (world pos)</param>
    /// <returns>returns false if spot is blocked by player / occupied by solid block</returns>
    public bool PlaceBlock(BlockType type, Vector3 _pos)
    {
        var idx = GetBlockIdx(_pos);
        var blockPos = Vector3Int.FloorToInt(_pos);
        var block = blocks[idx.x, idx.y, idx.z];
        if (!(block is Fluid))
        {
            if (block != null) return false;
        }
        if (Physics.CheckBox(blockPos + Vector3.one * 0.5f, Vector3.one * 0.45f)) return false;
        blocks[idx.x, idx.y, idx.z] = BlockFactory.Create(type, blockPos);
        blocks[idx.x, idx.y, idx.z].OnPlaced();
        MakeMesh();
        UpdateAdjacentChunks(idx);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(new Vector3(pos.x, 0, pos.y) * SIZE + new Vector3(SIZE, HEIGHT, SIZE) * 0.5f, new Vector3(SIZE, HEIGHT, SIZE));
    }
}
