using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (PerlinNoise))]
public class CustomMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PerlinNoise noiseGen = (PerlinNoise)target;

        if (DrawDefaultInspector())
        {
            if (noiseGen.autoUpdate)
            {
                GenerateNoise(noiseGen);
            }
        }

        if (GUILayout.Button("Generate"))
        {
            GenerateNoise(noiseGen);

            if (EditorApplication.isPlaying)
            {
                GameObject GenerateTerrainObject = GameObject.Find("GenerateTerrain");
                GenerateTerrain GenerateTerrain = GenerateTerrainObject.GetComponent<GenerateTerrain>();
                GenerateTerrain.DestroyObjects();

                int[,] heightMap = GenerateTerrain.GenerateHeightMap();
                GenerateTerrain.SetHeightMap(heightMap);
                GenerateTerrain.GenerateTerrainParallel();
            }
        }

        if (GUILayout.Button("Save PNG"))
        {
            noiseGen.ExportToPNG();
        }
    }

    void GenerateNoise(PerlinNoise noiseGen)
    {
        noiseGen.PrepareRender();
        noiseGen.DrawNoise();

        GameObject canvasRawImage = GameObject.Find("RawImage");
        noiseGen.DrawOnCanvas(canvasRawImage);
        //GameObject canvasImage = GameObject.Find("Image");
        //noiseGen.DrawOnCanvas(canvasImage);
    }
}
