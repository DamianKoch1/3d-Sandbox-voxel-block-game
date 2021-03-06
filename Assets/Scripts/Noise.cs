﻿using UnityEngine;

[CreateAssetMenu]
public class Noise : ScriptableObject
{
#if UNITY_EDITOR
    private static string copied;

    [ContextMenu("Copy")]
    private void Copy()
    {
        copied = JsonUtility.ToJson(this);
    }

    [ContextMenu("Paste")]
    private void Paste()
    {
        if (string.IsNullOrEmpty(copied)) return;
        JsonUtility.FromJsonOverwrite(copied, this);
    }

#endif

    public int seed;

    [Range(0, 0.15f)]
    public float frequency = 0.01f;

    [Range(1, 50)]
    public float amplitude = 1;

    [SerializeField] private NoiseLayer[] layers = new NoiseLayer[0];

    private SimplexNoise sNoise;

    private SimplexNoise SNoise
    {
        get
        {
            if (sNoise?.Seed != seed) sNoise = new SimplexNoise(seed);
            return sNoise;
        }
    }

    public void RandomizeSeed()
    {
        seed = Random.Range(0, 1000000);
    }

    public float GetValue(float x, float z)
    {
        var retVal = SNoise.Evaluate(x * frequency, z * frequency) * amplitude;
        foreach (NoiseLayer layer in layers) retVal += layer.GetValue(SNoise, x, z);
        return retVal;
    }

    public float GetValue(float x, float y, float z)
    {
        var retVal = SNoise.Evaluate(x * frequency, y * frequency, z * frequency) * amplitude;
        foreach (NoiseLayer layer in layers) retVal += layer.GetValue(SNoise, x, y, z);
        return retVal;
    }

    [System.Serializable]
    private class NoiseLayer
    {
        [SerializeField, Range(0, 1)]
        private float weight = 1;

        [SerializeField, Range(0, 0.3f)]
        private float frequency = 0.01f;

        [SerializeField, Range(0, 10)]
        private float amplitude = 1;

        public float GetValue(SimplexNoise n, float x, float z) => (n.Evaluate(x * frequency, z * frequency) * amplitude * 2 - amplitude) * weight;

        public float GetValue(SimplexNoise n, float x, float y, float z) => (n.Evaluate(x * frequency, y * frequency, z * frequency) * amplitude * 2 - amplitude) * weight;
    }
}
