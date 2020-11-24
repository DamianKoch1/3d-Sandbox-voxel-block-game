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
        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fixedWidth = 200,
            fixedHeight = 200
        };
        if (tg.CurrentBiome)  GUILayout.Label(tg.GetSurfaceNoiseSampleTex(), style);
        if (tg.CurrentBiome)  GUILayout.Label(tg.GetCaveNoiseSampleTex(), style);
        GUILayout.EndHorizontal();


    }
}
