using UnityEngine;

[CreateAssetMenu]
public class Biome : ScriptableObject
{
    [Header("Surface settings"), Range(0, 100)]
    public int minSurfaceLevel = 50;

    [Range(0, 100)]
    public int surfaceFluidLevel = 60; 
    
    [Range(0, 100)]
    public int caveFluidLevel = 8;

    [Range(2, 10)]
    public int surfaceLayerSize = 5;


    public BlockType surface = BlockType.Grass;
    public BlockType floodedSurface = BlockType.Dirt;
    public BlockType surfaceLayer = BlockType.Dirt;
    public BlockType surfaceFluid = BlockType.Water;
    public BlockType log = BlockType.Log;
    public BlockType leaves = BlockType.Leaves;
    public BlockType caveFluid = BlockType.Lava;
    public BlockType defaultBlock = BlockType.Stone;
    public BiomeLayer[] customLayers;

    public Noise surfaceNoise;

    [Range(0, 0.01f)]
    public float treeDensity = 0.01f;

    [Header("Cave settings")]
    public Noise caveNoise;

    [Range(0, 1), Tooltip("Carves a cave if CaveNoise at position is above this")]
    public float caveNoiseThreshold = 0.8f;

    [SerializeField, Range(0, 10), Tooltip("How far below the surface / water level caves start to generate")]
    public int minCaveSurfaceDistance = 10;
}

[System.Serializable]
public class BiomeLayer
{
    public BiomeLayer(int minHeight, int size, BlockType block)
    {
        this.minHeight = minHeight;
        this.size = size;
        this.block = block;
    }
    public int minHeight;
    public int size;
    public BlockType block;
}