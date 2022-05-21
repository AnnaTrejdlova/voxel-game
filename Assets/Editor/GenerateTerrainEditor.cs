using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GenerateTerrain))]
public class GenerateTerrainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GenerateTerrain terrainGen = (GenerateTerrain)target;
        DrawDefaultInspector();

        if (GUILayout.Button("Generate"))
        {
            if (EditorApplication.isPlaying)
            {
                GameObject GenerateTerrainObject = GameObject.Find("GenerateTerrain");
                GenerateTerrain GenerateTerrain = GenerateTerrainObject.GetComponent<GenerateTerrain>();
                GenerateTerrain.DestroyObjects();
                //GenerateTerrain.GenerateMapData();
                //GenerateTerrain.GenerateMeshFromData();
                GenerateTerrain.GenerateTerrainParallel();
            }
        }

        if (GUILayout.Button("Destroy"))
        {
            if (EditorApplication.isPlaying)
            {
                GameObject GenerateTerrainObject = GameObject.Find("GenerateTerrain");
                GenerateTerrain GenerateTerrain = GenerateTerrainObject.GetComponent<GenerateTerrain>();
                GenerateTerrain.DestroyObjects();
            }
        }
    }
}
