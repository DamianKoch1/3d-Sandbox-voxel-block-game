using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public const int SIZE = 16;

    public const int HEIGHT = 32;

    public const int MIN_SURFACE_LEVEL = 5;

    public const int WATER_LEVEL = 7;

    public Vector2Int pos;

    public Block[,,] blocks;

    Mesh mesh;

    MeshRenderer mr;

    MeshFilter mf;

    MeshCollider mc;


    public void Initialize(Vector2Int _pos)
    {
        blocks = new Block[SIZE, HEIGHT, SIZE];
        pos = _pos;
        mr = GetComponent<MeshRenderer>();
        mf = GetComponent<MeshFilter>();
        mc = GetComponent<MeshCollider>();
        transform.position = new Vector3(pos.x * SIZE, 0, pos.y * SIZE);
        gameObject.name = "Chunk " + pos;
        Generate();
        MakeMesh();
    }

    private void Generate()
    {
        for (int x = 0; x < SIZE; x++)
        {
            for (int z = 0; z < SIZE; z++)
            {
                Vector3Int blockPos = new Vector3Int(x, TerrainGenerator.Instance.PerlinNoise(x + pos.x * 16, z + pos.y * 16) + MIN_SURFACE_LEVEL, z);
                for (int y = 0; y <= blockPos.y; y++)
                {
                    var block = new Block(blockPos);
                    blocks[blockPos.x, blockPos.y - y, blockPos.z] = block;
                }
            }
        }
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
                    var blockPos = new Vector3(x, y, z);
                    if (block == null) continue;
                    if (block.type == BlockType.air) continue;
                    if (block.type == BlockType.fluid)
                    {

                    }
                    else
                    {
                        //front
                        vertices.Add(blockPos + new Vector3(0, 0, 0));
                        vertices.Add(blockPos + new Vector3(0, 1, 0));
                        vertices.Add(blockPos + new Vector3(1, 1, 0));
                        vertices.Add(blockPos + new Vector3(1, 0, 0));

                        //back
                        vertices.Add(blockPos + new Vector3(1, 0, 1));
                        vertices.Add(blockPos + new Vector3(1, 1, 1));
                        vertices.Add(blockPos + new Vector3(0, 1, 1));
                        vertices.Add(blockPos + new Vector3(0, 0, 1));

                        //right
                        vertices.Add(blockPos + new Vector3(1, 0, 0));
                        vertices.Add(blockPos + new Vector3(1, 1, 0));
                        vertices.Add(blockPos + new Vector3(1, 1, 1));
                        vertices.Add(blockPos + new Vector3(1, 0, 1));

                        //left
                        vertices.Add(blockPos + new Vector3(0, 0, 1));
                        vertices.Add(blockPos + new Vector3(0, 1, 1));
                        vertices.Add(blockPos + new Vector3(0, 1, 0));
                        vertices.Add(blockPos + new Vector3(0, 0, 0));

                        //top
                        vertices.Add(blockPos + new Vector3(0, 1, 0));
                        vertices.Add(blockPos + new Vector3(0, 1, 1));
                        vertices.Add(blockPos + new Vector3(1, 1, 1));
                        vertices.Add(blockPos + new Vector3(1, 1, 0));

                        //bottom
                        vertices.Add(blockPos + new Vector3(0, 0, 0));
                        vertices.Add(blockPos + new Vector3(1, 0, 0));
                        vertices.Add(blockPos + new Vector3(1, 0, 1));
                        vertices.Add(blockPos + new Vector3(0, 0, 1));


                        int triangleIdx = vertices.Count - 24;
                        for (int i = 0; i < 6; i++)
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
        }


        mesh.vertices = vertices.ToArray();

        mesh.triangles = triangles.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.Optimize();

        mf.mesh = mesh;

        mc.sharedMesh = mesh;
    }

    public Block GetBlock(float x, float y, float z)
    {
        return blocks[(int)x - pos.x * SIZE, (int)y, (int)z - pos.y * SIZE];
    }
}
