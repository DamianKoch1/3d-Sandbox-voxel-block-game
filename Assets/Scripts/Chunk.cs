using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public const int SIZE = 16;

    public const int HEIGHT = 128;

    public const int MIN_SURFACE_LEVEL = 50;

    public const int WATER_LEVEL = 60;

    public Vector2Int pos;

    public Block[,,] blocks;

    Mesh mesh;

    MeshRenderer mr;

    MeshFilter mf;

    MeshCollider mc;


    public void Initialize(Vector2Int _pos)
    {
        pos = _pos;
        mr = GetComponent<MeshRenderer>();
        mf = GetComponent<MeshFilter>();
        mc = GetComponent<MeshCollider>();
        transform.position = new Vector3(pos.x * SIZE, 0, pos.y * SIZE);
        gameObject.name = "Chunk " + pos;
        Generate();
    }

    public void Generate()
    {
        blocks = new Block[SIZE, HEIGHT, SIZE];
        for (int x = 0; x < SIZE; x++)
        {
            for (int z = 0; z < SIZE; z++)
            {
                Vector3Int blockPos = new Vector3Int(x, TerrainGenerator.Instance.PerlinNoise(x + pos.x * 16, z + pos.y * 16) + MIN_SURFACE_LEVEL, z);
                for (int y = 0; y <= blockPos.y; y++)
                {
                    var block = new BlockOpaque(blockPos);
                    blocks[blockPos.x, blockPos.y - y, blockPos.z] = block;
                }
            }
        }
        MakeMesh();
    }

    private void MakeMesh()
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


                    if (z == 0 || block.DrawFaceNextTo(blocks[x, y, z - 1]))
                    {
                        //front
                        vertices.Add(blockPos + new Vector3(0, 0, 0));
                        vertices.Add(blockPos + new Vector3(0, 1, 0));
                        vertices.Add(blockPos + new Vector3(1, 1, 0));
                        vertices.Add(blockPos + new Vector3(1, 0, 0));
                        numFaces++;
                    }

                    if (z == SIZE - 1 || block.DrawFaceNextTo(blocks[x, y, z + 1]))
                    {
                        //back
                        vertices.Add(blockPos + new Vector3(1, 0, 1));
                        vertices.Add(blockPos + new Vector3(1, 1, 1));
                        vertices.Add(blockPos + new Vector3(0, 1, 1));
                        vertices.Add(blockPos + new Vector3(0, 0, 1));
                        numFaces++;
                    }

                    if (x == SIZE - 1 || block.DrawFaceNextTo(blocks[x + 1, y, z]))
                    {
                        //right
                        vertices.Add(blockPos + new Vector3(1, 0, 0));
                        vertices.Add(blockPos + new Vector3(1, 1, 0));
                        vertices.Add(blockPos + new Vector3(1, 1, 1));
                        vertices.Add(blockPos + new Vector3(1, 0, 1));
                        numFaces++;
                    }

                    if (x == 0 || block.DrawFaceNextTo(blocks[x - 1, y, z]))
                    {
                        //left
                        vertices.Add(blockPos + new Vector3(0, 0, 1));
                        vertices.Add(blockPos + new Vector3(0, 1, 1));
                        vertices.Add(blockPos + new Vector3(0, 1, 0));
                        vertices.Add(blockPos + new Vector3(0, 0, 0));
                        numFaces++;
                    }

                    if (y == HEIGHT - 1 || block.DrawFaceNextTo(blocks[x, y + 1, z]))
                    {
                        //top
                        vertices.Add(blockPos + new Vector3(0, 1, 0));
                        vertices.Add(blockPos + new Vector3(0, 1, 1));
                        vertices.Add(blockPos + new Vector3(1, 1, 1));
                        vertices.Add(blockPos + new Vector3(1, 1, 0));
                        numFaces++;
                    }

                    if (y == 0 || block.DrawFaceNextTo(blocks[x, y - 1, z]))
                    {
                        //bottom
                        vertices.Add(blockPos + new Vector3(0, 0, 0));
                        vertices.Add(blockPos + new Vector3(1, 0, 0));
                        vertices.Add(blockPos + new Vector3(1, 0, 1));
                        vertices.Add(blockPos + new Vector3(0, 0, 1));
                        numFaces++;
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

        mf.mesh = mesh;

        mc.sharedMesh = null;
        mc.sharedMesh = mesh;
    }

    public Block GetBlock(float x, float y, float z)
    {
        return blocks[(int)x - pos.x * SIZE, (int)y, (int)z - pos.y * SIZE];
    }

    public bool DestroyBlock(Vector3 _pos)
    {
        int x = (int)_pos.x - pos.x * SIZE;
        int y = (int)_pos.y;
        int z = (int)_pos.z - pos.y * SIZE;
        if (blocks[x, y, z] == null) return false;
        blocks[x, y, z] = null;
        MakeMesh();
        return true;
    }

    public bool PlaceBlock(Vector3 _pos)
    {
        int x = (int)_pos.x - pos.x * SIZE;
        int y = (int)_pos.y;
        int z = (int)_pos.z - pos.y * SIZE;
        if (blocks[x, y, z] != null) return false;
        if (Physics.CheckBox(new Vector3(x, y, z) + Vector3.one * 0.5f, Vector3.one * 0.4f)) return false;
        blocks[x, y, z] = new BlockOpaque(new Vector3Int(x, y, z));
        MakeMesh();
        return true;
    }
}
