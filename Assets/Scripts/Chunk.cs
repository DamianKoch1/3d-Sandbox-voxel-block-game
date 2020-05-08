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
        for (int x = 0; x < SIZE; x++)
        {
            for (int z = 0; z < SIZE; z++)
            {
                var localSurfaceLevel = TerrainGenerator.Instance.PerlinNoise(x + pos.x * 16, z + pos.y * 16);
                for (int y = 0; y <= Mathf.Max(localSurfaceLevel, TerrainGenerator.Instance.waterLevel); y++)
                {
                    Block block = null;
                    var blockPos = new Vector3Int(x + pos.x * SIZE, y, z + pos.y * SIZE);
                    if (y == 0) block = BlockFactory.Create(BlockType.bottomStone, blockPos);
                    else if (y > localSurfaceLevel) block = BlockFactory.Create(BlockType.water, blockPos);
                    else if (y == localSurfaceLevel)
                    {
                        if (y >= TerrainGenerator.Instance.waterLevel) block = BlockFactory.Create(BlockType.grass, blockPos);
                        else block = BlockFactory.Create(BlockType.dirt, blockPos);
                    }
                    else if (y > localSurfaceLevel - TerrainGenerator.Instance.dirtLayerSize) block = BlockFactory.Create(BlockType.dirt, blockPos);
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
    /// Get the block in this chunk at _pos
    /// </summary>
    /// <param name="_pos">position of block (world pos)</param>
    /// <returns></returns>
    public Block GetBlockByPos(Vector3 _pos)
    {
        var idx = GetBlockIdx(_pos);
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
        var block = blocks[idx.x, idx.y, idx.z];
        if (block == null) return false;
        blocks[idx.x, idx.y, idx.z] = null;
        MakeMesh();
        if (idx.x == 0) TerrainGenerator.Instance.GetChunkByIdx(pos + Vector2Int.left)?.MakeMesh();
        else if (idx.x == SIZE - 1) TerrainGenerator.Instance.GetChunkByIdx(pos + Vector2Int.right)?.MakeMesh();
        if (idx.z == 0) TerrainGenerator.Instance.GetChunkByIdx(pos + Vector2Int.down)?.MakeMesh();
        else if (idx.z == SIZE - 1) TerrainGenerator.Instance.GetChunkByIdx(pos + Vector2Int.up)?.MakeMesh();
        return true;
    }

    /// <summary>
    /// Places default block at _pos, if successful rebuilds mesh, if at chunk border rebuilds neighbour mesh aswell
    /// </summary>
    /// <param name="_pos">where to place block (world pos)</param>
    /// <returns></returns>
    public bool PlaceBlock(BlockType type, Vector3 _pos)
    {
        var idx = GetBlockIdx(_pos);
        var block = blocks[idx.x, idx.y, idx.z];
        if (!(block is Fluid))
        {
            if (block != null) return false;
        }
        if (Physics.CheckBox(new Vector3(idx.x, idx.y, idx.z) + Vector3.one * 0.5f, Vector3.one * 0.45f)) return false;
        blocks[idx.x, idx.y, idx.z] = BlockFactory.Create(type, idx);
        MakeMesh();
        if (idx.x == 0) TerrainGenerator.Instance.chunks[pos + Vector2Int.left]?.MakeMesh();
        else if (idx.x == SIZE - 1) TerrainGenerator.Instance.chunks[pos + Vector2Int.right]?.MakeMesh();
        if (idx.z == 0) TerrainGenerator.Instance.chunks[pos + Vector2Int.down]?.MakeMesh();
        else if (idx.z == SIZE - 1) TerrainGenerator.Instance.chunks[pos + Vector2Int.up]?.MakeMesh();
        return true;
    }
}
