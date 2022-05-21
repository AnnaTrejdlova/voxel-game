using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class PerlinNoise : MonoBehaviour
{
    // Width and height of the texture in pixels.
    public Vector2 resolution;
    public bool lockAspectRatioToSquare;

    // The origin of the sampled area in the plane.
    public Vector2 offset;

    // The number of cycles of the basic noise pattern that are repeated
    // over the width and height of the texture.
    public float scale = 1.0F;

    public int octaves;

    [Range(0,1)]
    public float persistance;
    public float lacunarity;

    public bool autoUpdate;

    public bool useRegions;
    public TerrainType[] regions;

    private Texture2D noiseTex;
    private Color[] pix;
    private Renderer rend;

    // Start is called before the first frame update
    void Start()
    {
        PrepareRender();
        DrawNoise();
    }

    private void OnValidate()
    {
        if (lockAspectRatioToSquare)
        {
            resolution.y = resolution.x;
        }

        if (resolution.x < 1)
        {
            resolution.x = 1;
        }
        if (resolution.y < 1)
        {
            resolution.y = 1;
        }
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 0)
        {
            octaves = 1;
        }
    }

    public float[,] CalcNoise(int width, int height)
    {
        if (width <= 0)
        {
            width = noiseTex.width;
        }
        if (height <= 0)
        {
            height = noiseTex.height;
        }
        float[,] noiseMap = new float[width, height];

        if (scale <= 0)
        {
            scale = 0.01f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float xCoord = offset.x + (float) x / scale * frequency;
                    float yCoord = offset.y + (float) y / scale * frequency;
                    float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }
                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }

        return noiseMap;
    }

    public float[,] CalcNoiseMinecraft(int width, int height)
    {
        if (width <= 0)
        {
            width = noiseTex.width;
        }
        if (height <= 0)
        {
            height = noiseTex.height;
        }
        float[,] noiseMap = new float[width, height];

        if (scale <= 0)
        {
            scale = 0.01f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float[] octaveRng = new float[octaves];

        for (int i = 0; i < octaveRng.Length; i++)
        {
            octaveRng[i] = Random.value * 100000;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1;
                //float frequency = 1;
                float noiseHeight = 0;
                float scale = 1000;

                for (int i = 0; i < octaves; i++)
                {
                    float xCoord = offset.x + (float)x / scale;
                    float yCoord = offset.y + (float)y / scale;
                    float perlinValue = Mathf.PerlinNoise(octaveRng[i] + xCoord / amplitude, octaveRng[i] + yCoord / amplitude);
                    noiseHeight += perlinValue / amplitude;

                    amplitude /= 2;
                    //frequency /= 2;
                }
                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }

        return noiseMap;
    }

    public void DrawNoise()
    {
        float[,] noiseMap = CalcNoiseMinecraft(noiseTex.width, noiseTex.height);

        for (int y = 0; y < noiseTex.height; y++)
        {
            for (int x = 0; x < noiseTex.width; x++)
            {
                float currentHeight = noiseMap[x, y];
                if (useRegions)
                {
                    for (int i = 0; i < regions.Length; i++)
                    {
                        if (currentHeight <= regions[i].height)
                        {
                            pix[y * noiseTex.width + x] = regions[i].color;
                            break;
                        }
                    }
                }
                else
                {
                    pix[y * noiseTex.width + x] = new Color(currentHeight, currentHeight, currentHeight);
                }
            }
        }

        // Copy the pixel data to the texture and load it into the GPU.
        noiseTex.SetPixels(pix);
        noiseTex.Apply();
    }

    public void PrepareRender()
    {
        rend = GetComponent<Renderer>();

        // Set up the texture and a Color array to hold pixels during processing.
        noiseTex = new Texture2D((int)resolution.x, (int)resolution.y, TextureFormat.RGB24, false);
        if (useRegions)
        {
            noiseTex.filterMode = FilterMode.Point;
        }
        noiseTex.wrapMode = TextureWrapMode.Clamp;
        pix = new Color[noiseTex.width * noiseTex.height];
        rend.material.mainTexture = noiseTex;
    }

    public void DrawOnCanvas(GameObject drawTarget)
    {
        //drawTarget.GetComponent<CanvasRenderer>().SetTexture(noiseTex);
        RawImage raw = drawTarget.GetComponent<RawImage>();
        raw.texture = noiseTex;
        Material material = new Material(Shader.Find("Unlit/Texture"));
        material.mainTexture = noiseTex;
        raw.material = material;
    }

    public void ExportToPNG()
    {
        byte[] imageBytes = noiseTex.EncodeToPNG();
        int num = 0;
        while (File.Exists(Application.dataPath + "/../NoiseMap" + num + ".png"))
        {
            num++;
        }
        File.WriteAllBytes(Application.dataPath + "/../NoiseMap" + num + ".png", imageBytes);
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}