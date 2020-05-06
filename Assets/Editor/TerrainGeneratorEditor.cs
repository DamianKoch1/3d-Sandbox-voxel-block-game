using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Generate"))
        {
            ((TerrainGenerator)target).Generate();
        }

        if (GUILayout.Button("Clear"))
        {
            ((TerrainGenerator)target).Clear();
        }
    }
}
