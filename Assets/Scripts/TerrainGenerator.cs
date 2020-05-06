using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public GameObject cube;

    public Vector2Int dimensions;

    public Dictionary<Vector2Int, Chunk> chunks;

    [Range(0, 1)]
    public float perlinScale;

    public float amplitude;

    public bool useRandomSeed;
    
    public float seed;

    void Start()
    {

    }

    public void Generate()
    {
        Clear();
        chunks = new Dictionary<Vector2Int, Chunk>();

        if (useRandomSeed)
        {
            seed = Random.Range(0f, 10000f);
        }

        for (int i = 0; i < dimensions.x; i++)
        {
            for (int j = 0; j < dimensions.y; j++)
            {
                Instantiate(cube, transform).transform.position = new Vector3(i, PerlinNoise(i, j) * amplitude, j);
            }
        }
    }

    public void Clear()
    {
        chunks = new Dictionary<Vector2Int, Chunk>();

        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    void Update()
    {

    }

    public float PerlinNoise(float x, float y)
    {
        return Mathf.PerlinNoise(x * perlinScale + seed, y * perlinScale + seed) * amplitude;
    }
}
