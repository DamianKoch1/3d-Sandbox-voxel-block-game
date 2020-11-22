using UnityEngine;

[CreateAssetMenu]
public class TerrainGeneratorConfig : ScriptableObject
{
    [Header("Surface settings"), Range(0, 100)]
    public int minSurfaceLevel = 50;

    [Range(0, 100)]
    public int waterLevel = 60; 
    
    [Range(0, 100)]
    public int lavaLevel = 8;

    [Range(2, 10)]
    public int dirtLayerSize = 5;

    public Noise surfaceNoise;

    [Header("Cave settings")]
    public Noise caveNoise;

    [Range(0, 1), Tooltip("Carves a cave if CaveNoise at position is above this")]
    public float caveNoiseThreshold = 0.8f;

    [SerializeField, Range(0, 10), Tooltip("How far below the surface / water level caves start to generate")]
    public int minCaveSurfaceDistance = 10;
}
