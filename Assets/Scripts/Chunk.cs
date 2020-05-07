using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public const int SIZE = 16;

    public const int HEIGHT = 128;



    public Vector2Int pos;

    public Block[,,] blocks;

    Mesh mesh;

    MeshRenderer mr;

    MeshFilter mf;

    MeshCollider mc;

    public GameObject waterPrefab;

    private GameObject water;


    public void Initialize(Vector2Int _pos)
    {
        pos = _pos;
        mr = GetComponent<MeshRenderer>();
        mf = GetComponent<MeshFilter>();
        mc = GetComponent<MeshCollider>();
        transform.position = new Vector3(pos.x * SIZE, 0, pos.y * SIZE);
        gameObject.name = "Chunk " + pos;
        Generate();
        water = Instantiate(waterPrefab, transform);
    }

    public virtual void Generate()
    {
        blocks = new Block[SIZE, HEIGHT, SIZE];
        for (int x = 0; x < SIZE; x++)
        {
            for (int z = 0; z < SIZE; z++)
            {
                var localSurfaceLevel = TerrainGenerator.Instance.PerlinNoise(x + pos.x * 16, z + pos.y * 16);
                for (int y = 0; y < Mathf.Max(localSurfaceLevel, TerrainGenerator.Instance.waterLevel); y++)
                {
                    if (y <= localSurfaceLevel)
                    {
                        var block = new BlockOpaque(new Vector3Int(x + pos.x * SIZE, y, z + pos.y * SIZE));
                        blocks[x, y, z] = block;
                    }
                    else
                    {
                        var water = new Fluid(new Vector3Int(x + pos.x * SIZE, y, z + pos.y * SIZE));
                        blocks[x, y, z] = water;
                    }
                }
            }
        }
    }

    public void MakeMesh()
    {
        mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> UVs = new List<Vector2>();

        for (int x = 0; x < SIZE; x++)
        {
            for (int z = 0; z < SIZE; z++)
            {
                for (int y = 0; y < HEIGHT; y++)
                {
                    var block = blocks[x, y, z];
                    if (block == null) continue;
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
                        vertices.Add(blockPos + new Vector3(0, 0, 0));
                        vertices.Add(blockPos + new Vector3(0, 1, 0));
                        vertices.Add(blockPos + new Vector3(1, 1, 0));
                        vertices.Add(blockPos + new Vector3(1, 0, 0));
                        numFaces++;
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
                        vertices.Add(blockPos + new Vector3(1, 0, 1));
                        vertices.Add(blockPos + new Vector3(1, 1, 1));
                        vertices.Add(blockPos + new Vector3(0, 1, 1));
                        vertices.Add(blockPos + new Vector3(0, 0, 1));
                        numFaces++;
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
                        vertices.Add(blockPos + new Vector3(0, 0, 1));
                        vertices.Add(blockPos + new Vector3(0, 1, 1));
                        vertices.Add(blockPos + new Vector3(0, 1, 0));
                        vertices.Add(blockPos + new Vector3(0, 0, 0));
                        numFaces++;
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
                        vertices.Add(blockPos + new Vector3(1, 0, 0));
                        vertices.Add(blockPos + new Vector3(1, 1, 0));
                        vertices.Add(blockPos + new Vector3(1, 1, 1));
                        vertices.Add(blockPos + new Vector3(1, 0, 1));
                        numFaces++;
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
                        vertices.Add(blockPos + new Vector3(0, 0, 0));
                        vertices.Add(blockPos + new Vector3(1, 0, 0));
                        vertices.Add(blockPos + new Vector3(1, 0, 1));
                        vertices.Add(blockPos + new Vector3(0, 0, 1));
                        numFaces++;
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
                            vertices.Add(blockPos + new Vector3(0, 1, 0));
                            vertices.Add(blockPos + new Vector3(0, 1, 1));
                            vertices.Add(blockPos + new Vector3(1, 1, 1));
                            vertices.Add(blockPos + new Vector3(1, 1, 0));
                            numFaces++;
                        }
                    }




                    int triangleIdx = vertices.Count - numFaces * 4;
                    for (int i = 0; i < numFaces; i++)
                    {
                        int i4 = i * 4;
                        triangles.AddRange(new int[]
                        {
                            triangleIdx + i4, triangleIdx + i4 + 1, triangleIdx + i4 + 2,
                            triangleIdx + i4, triangleIdx + i4 + 2, triangleIdx + i4 + 3
                        });
                    }
                }
            }
        }


        mesh.vertices = vertices.ToArray();

        mesh.triangles = triangles.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.Optimize();

        mf.mesh = null;
        mf.mesh = mesh;

        mc.sharedMesh = null;
        mc.sharedMesh = mesh;
    }

    public Vector3Int GetBlockIdx(Vector3 v)
    {
        Vector3Int vi = Vector3Int.zero;
        vi.x = Mathf.FloorToInt(v.x) - pos.x * SIZE;
        vi.y = Mathf.FloorToInt(v.y);
        vi.z = Mathf.FloorToInt(v.z) - pos.y * SIZE;
        return vi;
    }

    public Block GetBlock(Vector3 _pos)
    {
        var idx = GetBlockIdx(_pos);
        return blocks[idx.x, idx.y, idx.z];
    }

    public bool DestroyBlock(Vector3 _pos)
    {
        var idx = GetBlockIdx(_pos);
        if (blocks[idx.x, idx.y, idx.z] == null) return false;
        blocks[idx.x, idx.y, idx.z] = null;
        MakeMesh();
        return true;
    }

    public bool PlaceBlock(Vector3 _pos)
    {
        var idx = GetBlockIdx(_pos);
        if (blocks[idx.x, idx.y, idx.z] != null) return false;
        if (Physics.CheckBox(new Vector3(idx.x, idx.y, idx.z) + Vector3.one * 0.5f, Vector3.one * 0.5f)) return false;
        blocks[idx.x, idx.y, idx.z] = new BlockOpaque(new Vector3Int(idx.x, idx.y, idx.z));
        MakeMesh();
        return true;
    }
}
