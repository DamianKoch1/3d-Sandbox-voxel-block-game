﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    private TerrainGenerator tg => (TerrainGenerator)target;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Generate"))
        {
            tg.Generate();
        }

        if (GUILayout.Button("Clear"))
        {
            tg.Clear();
        }

        GUILayout.BeginHorizontal();
        var style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.UpperCenter;
        style.fixedWidth = 100;
        style.fixedHeight = 100;
        if (tg.showSurfaceNoiseSample)  GUILayout.Label(tg.GetSurfaceNoiseSampleTex(), style);
        if (tg.showCaveNoiseSample)     GUILayout.Label(tg.GetCaveNoiseSampleTex(), style);
        GUILayout.EndHorizontal();


    }
}
