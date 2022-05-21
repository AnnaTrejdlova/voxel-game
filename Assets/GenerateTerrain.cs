using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class GenerateTerrain : MonoBehaviour
{
    public Vector2Int mapSize = new Vector2Int(100, 100);
    public bool lockAspectRatioToSquare = true;

    public int scale = 10;
    public int heightScale = 60;
    public Vector2Int chunkSize = new Vector2Int(10, 10);
    public byte maxHeight = byte.MaxValue;

    public bool parallel = true;

    static PerlinNoise noiseGenerator;

    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    public GameObject grassTopQuad;
    public GameObject grassBottomQuad;
    public GameObject grassSideQuad;

    public Material terrainMaterial;
    public float mipMapBias = 0f;

    static readonly Vector3[] neighbors = new Vector3[] { Vector3.right, Vector3.up, Vector3.forward, Vector3.left, Vector3.down, Vector3.back };

    Vector3[] quadPrefabVertices;
    int[] quadPrefabTriangles;
    Vector2[] quadPrefabUvs;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    float startTime;
    float endTime;

    public byte[,,] heightMapGlob;

    Dictionary<Vector3Int, byte> modifiedBlocks = new Dictionary<Vector3Int, byte>();

    // Start is called before the first frame update
    void Start()
    {
        System.GC.Collect();
        noiseGenerator = FindObjectOfType<PerlinNoise>();

        MakeMeshData();
        quadPrefabVertices = grassTopQuad.GetComponent<MeshFilter>().mesh.vertices;
        quadPrefabTriangles = grassTopQuad.GetComponent<MeshFilter>().mesh.triangles;
        quadPrefabUvs = (Vector2[])uvs.Clone();

        int [,] heightMap = GenerateHeightMap();
        SetHeightMap(heightMap);

        if (parallel)
        {
            GenerateTerrainParallel();
        }
        else
        {
            GenerateTerrainSeries();
        }
    }

    private void OnValidate()
    {
        if (lockAspectRatioToSquare)
        {
            mapSize.y = mapSize.x;
            chunkSize.y = chunkSize.x;
        }
    }

    void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
                if (i == meshDataThreadInfoQueue.Count-1)
                {
                    endTime = Time.realtimeSinceStartup;
                    print("Time to dequeue (parallel): " + (endTime - startTime) * 1000 + " ms");
                }
            }
        }
    }

    void GenerateChunk(Vector2 chunk)
    {
        MapData mapData = GenerateMapData(new Vector2(chunk.x, chunk.y));
        MakeMeshData();
        MeshData meshData = GenerateMeshFromData(mapData);

        //meshData.CreateAllMeshes();
        CreateObjectsFromMeshData(meshData);
    }

    void GenerateTerrainSeries()
    {
        terrainMaterial.mainTexture.mipMapBias = mipMapBias;
        float startTime = Time.realtimeSinceStartup;
        for (int chunkZ = 0; chunkZ < Mathf.Ceil(mapSize.y / chunkSize.y); chunkZ++)
        {
            for (int chunkX = 0; chunkX < Mathf.Ceil(mapSize.x / chunkSize.x); chunkX++)
            {
                GenerateChunk(new Vector2(chunkX, chunkZ));
            }
        }
        float endTime = Time.realtimeSinceStartup;
        print("Time to create data: " + (endTime - startTime) * 1000 + " ms");
    }

    public void GenerateTerrainParallel()
    {
        terrainMaterial.mainTexture.mipMapBias = mipMapBias;
        startTime = Time.realtimeSinceStartup;
        for (int chunkZ = 0; chunkZ < Mathf.Ceil(mapSize.y / chunkSize.y); chunkZ++)
        {
            for (int chunkX = 0; chunkX < Mathf.Ceil(mapSize.x / chunkSize.x); chunkX++)
            {
                RequestMapData(new Vector2(chunkX, chunkZ), OnMapDataReceived);
            }
        }
        endTime = Time.realtimeSinceStartup;
        print("Time to create data (parallel): " + (endTime - startTime) * 1000 + " ms");
    }

    public MapData GenerateMapData(Vector2 chunkCoords)
    {
        //int[,] heightMap = new int[(int)chunkSize.x, (int)chunkSize.y];
        //int[,] heightMap = (int[,])this.heightMapGlob.Clone();

        int chunkX = (int)chunkCoords.x;
        int chunkZ = (int)chunkCoords.y;
        int chunkId = chunkZ * (int)Mathf.Ceil(mapSize.x / chunkSize.x) + chunkX;

        //for (int z = 0; z < (int)chunkSize.y; z++)
        //{
        //    for (int x = 0; x < (int)chunkSize.x; x++)
        //    {
        //        heightMap[x, z] = heightMapGlob[chunkX * (int)chunkSize.x + x, chunkZ * (int)chunkSize.y + z];
        //    }
        //}

        List<BlockInfo> chunkTop = new List<BlockInfo>();
        List<BlockInfo> chunkBottom = new List<BlockInfo>();
        List<BlockInfo> chunkSide = new List<BlockInfo>();

        foreach (KeyValuePair<Vector3Int, byte> block in modifiedBlocks)
        {
            heightMapGlob[block.Key.x, block.Key.y, block.Key.z] = block.Value;
        }

        for (int z = chunkZ * (int)chunkSize.y; z < (chunkZ+1) * (int)chunkSize.y; z++)
        {
            for (int x = chunkX * (int)chunkSize.x; x < (chunkX+1) * (int)chunkSize.x; x++)
            {
                for (int y = 0; y <= maxHeight; y++)
                {
                    byte blockId = heightMapGlob[x, z, y];
                    if (blockId == 0)
                    {
                        continue;
                    }
                    //Vector3 chunkPos = new Vector3(x, y, z);
                    //Vector3 pos = new Vector3(chunkX * (int)chunkSize.x + x, y, chunkZ * (int)chunkSize.y + z);
                    Vector3 pos = new Vector3Int(x, y, z);
                    if (y == maxHeight || heightMapGlob[x, z, y + 1] == 0)
                    {
                        chunkTop.Add(new BlockInfo(blockId, pos, Quaternion.Euler(90, 0, 0)));
                    }
                    if (y == 0 || heightMapGlob[x, z, y - 1] == 0)
                    {
                        chunkBottom.Add(new BlockInfo(blockId, new Vector3(pos.x, y-1, pos.z), Quaternion.Euler(-90, 0, 0)));
                    }

                    foreach (Vector3 dir in neighbors)
                    {
                        Vector3 coordToCheck = pos + dir;
                        //int diff;
                        //bool isAir = false;

                        if (coordToCheck.x < 0 || coordToCheck.x >= mapSize.x//chunkSize
                            || coordToCheck.z < 0 || coordToCheck.z >= mapSize.y)
                        {
                            //diff = y;
                            //isAir = true;
                        }
                        else
                        {
                            if (heightMapGlob[(int)coordToCheck.x, (int)coordToCheck.z, y] != 0)
                            {
                                continue;
                            }
                            //diff = y - heightMapGlob[(int)coordToCheck.x, (int)coordToCheck.z, y];
                        }

                        //by this point the neighboring block is air

                        //if (diff < 0)
                        //{
                        //    continue;
                        //}
                        //for (int i = diff - 1; i >= 0; i--)
                        //{
                            if (y == maxHeight || heightMapGlob[x, z, y+1] == 0)//i == 0
                            {
                                chunkSide.Add(new BlockInfo(blockId, new Vector3(pos.x, (float)y - 0.5f, pos.z) + dir / 2, Quaternion.LookRotation(-dir)));
                            }
                            else
                            {
                                chunkBottom.Add(new BlockInfo(blockId, new Vector3(pos.x, (float)y - 0.5f, pos.z) + dir / 2, Quaternion.LookRotation(-dir)));
                            }
                        //}
                    }
                }
            }
        }

        return new MapData(chunkCoords, chunkTop, chunkBottom, chunkSide);
    }

    public int[,] GenerateHeightMap()
    {
        float[,] noiseMap = noiseGenerator.CalcNoise((int)mapSize.x, (int)mapSize.y);

        int[,] heightMap = new int[(int)mapSize.x, (int)mapSize.y];

        for (int z = 0; z < mapSize.y; z++)
        {
            for (int x = 0; x < mapSize.x; x++)
            {
                heightMap[x, z] = (int)Mathf.Floor(noiseMap[x, z] * heightScale);
            }
        }

        return heightMap;
    }

    public void SetHeightMap(int[,] heightMap)
    {
        heightMapGlob = new byte[mapSize.x, mapSize.y, maxHeight+1];
        for (int x = 0; x < heightMap.GetLength(0); x++)
        {
            for (int z = 0; z < heightMap.GetLength(1); z++)
            {
                int surface = -1;
                for (int y = maxHeight; y >= 0; y--)
                {
                    if (y <= heightMap[x, z])
                    {
                        if (surface == -1)
                        {
                            heightMapGlob[x, z, y] = 2;//grass
                            surface = y;
                        }
                        else if (surface - y < 3)
                        {
                            heightMapGlob[x, z, y] = 3;//dirt
                        }
                        else
                        {
                            heightMapGlob[x, z, y] = 1;//stone
                        }
                    }
                    else
                    {
                        heightMapGlob[x, z, y] = 0;//air
                    }
                }
            }
        }
    }

    void RequestMapData(Vector2 chunkCoords, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(chunkCoords, callback);
        };
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 chunkCoords, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(chunkCoords);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    void OnMapDataReceived(MapData mapData)
    {
        RequestMeshData(mapData, OnMeshDataReceived);
    }

    void RequestMeshData(MapData mapData, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, callback);
        };
        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, Action<MeshData> callback)
    {
        MeshData meshData = GenerateMeshFromData(mapData);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void OnMeshDataReceived(MeshData meshData)
    {
        //meshData.CreateAllMeshes();
        CreateObjectsFromMeshData(meshData);
    }

    public MeshData GenerateMeshFromData(MapData mapData)
    {
        List<BlockInfo>[] objectPool = new List<BlockInfo>[] { mapData.chunksTop, mapData.chunksBottom, mapData.chunksSide };
        Vector3[][] meshVertices = new Vector3[objectPool.Length][];
        int[][] meshTriangles = new int[objectPool.Length][];
        Vector2[][] meshUvs = new Vector2[objectPool.Length][];

        for (int j = 0; j < objectPool.Length; j++)
        {
            List<BlockInfo> chunk = objectPool[j];
            int numObjects = chunk.Count;
            Vector3[] vertices = new Vector3[numObjects * 4];
            int verticesIndex = 0;
            int[] triangles = new int[numObjects * 6];
            int trianglesIndex = 0;
            Vector2[] uvs = new Vector2[numObjects * 4];
            //int uvsIndex = 0;

            Dictionary<byte, Vector2Int> texturePositionMapping = new Dictionary<byte, Vector2Int>();
            texturePositionMapping.Add(1, new Vector2Int(1, 15));
            texturePositionMapping.Add(2, new Vector2Int(7, 4));
            texturePositionMapping.Add(3, new Vector2Int(2, 15));
            texturePositionMapping.Add(4, new Vector2Int(0, 14));


            Dictionary<byte, Vector2[]> uvBlockMapping = new Dictionary<byte, Vector2[]>();

            foreach (byte key in texturePositionMapping.Keys)
            {
                uvBlockMapping.Add(key, CreateUvData(texturePositionMapping[key].x, texturePositionMapping[key].y));
            }

            foreach (BlockInfo block in chunk)
            {
                Vector3 pos = block.transform;
                Quaternion rotation = block.rotation * Quaternion.Euler(0, 0, 90);

                Matrix4x4 rotMatrix = Matrix4x4.Rotate(rotation);
                for (int i = 0; i < quadPrefabVertices.Length; i++)
                {
                    Vector3 position = rotMatrix.MultiplyPoint3x4(quadPrefabVertices[i]) + pos;
                    vertices[verticesIndex + i] = position;
                    if (block.blockId == 2 && j == 0)
                    {
                        uvs[verticesIndex + i] = CreateUvData(8, 4)[i];
                    }
                    else
                    {
                        uvs[verticesIndex + i] = uvBlockMapping[block.blockId][(byte)i];//uvBlockMapping[id][(byte)i]
                            //quadPrefabUvs[i]
                    }
                }

                for (int i = 0; i < quadPrefabTriangles.Length; i++)
                {
                    triangles[trianglesIndex + i] = verticesIndex + quadPrefabTriangles[i];
                }

                verticesIndex += quadPrefabVertices.Length;
                trianglesIndex += quadPrefabTriangles.Length;
            }
            meshVertices[j] = vertices;
            meshTriangles[j] = triangles;
            meshUvs[j] = uvs;
        }
        return new MeshData(meshVertices, meshTriangles, meshUvs, mapData.chunk);
    }

    Vector2[] CreateUvData(int x, int y)
    {
        float offset = 0f;
        Vector2[] uvs = new Vector2[] { new Vector2((float)x / 16 + offset, (float)y / 16 + offset), new Vector2((float)x / 16 + offset, (float)(y+1) / 16 - offset), new Vector2((float)(x+1) / 16 - offset, (float)y / 16 + offset), new Vector2((float)(x+1) / 16 - offset, (float)(y+1) / 16 - offset) };

        return uvs;
    }

    void CreateObjectsFromMeshData(MeshData meshData)
    {
        for (int i = 0; i < meshData.vertices.Length; i++)
        {
            string name = i switch
            {
                0 => "chunkTop",
                1 => "chunkBottom",
                2 => "chunkSide",
                _ => "chunk",
            };
            name += "_" + (int)meshData.chunk.x + "," + (int)meshData.chunk.y;
            GameObject chunkObject = new GameObject(name)
            {
                tag = "WorldBlocks"
            };
            MeshFilter meshComponent = chunkObject.AddComponent<MeshFilter>();
            meshComponent.mesh.vertices = meshData.vertices[i];
            meshComponent.mesh.triangles = meshData.triangles[i];
            meshComponent.mesh.uv = meshData.uvs[i];
            meshComponent.mesh.RecalculateNormals();
            meshComponent.mesh.RecalculateBounds();

            MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();

            //meshRenderer.material = i switch
            //{
            //    0 => Resources.Load("Materials/grass_block_top") as Material,
            //    1 => Resources.Load("Materials/grass_block_bottom") as Material,
            //    2 => Resources.Load("Materials/grass_block_side") as Material,
            //    _ => new Material(Shader.Find("default")),
            //};
            //if (i == 0)
            //{
            //    meshRenderer.material = Resources.Load("Materials/grass_block_top") as Material;
            //    //meshRenderer.material.color = new Color((float)119 / 255, (float)171 / 255, (float)47 / 255);
            //} else if (i == 2)
            //{
            //    meshRenderer.material = Resources.Load("Materials/grass_block_side") as Material;
            //    //meshRenderer.material.color = new Color((float)119 / 255, (float)171 / 255, (float)47 / 255);
            //}
            //else
            {
                meshRenderer.material = terrainMaterial;
            }

            MeshCollider meshCollider = chunkObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshComponent.mesh;
        }
    }

    void MakeMeshData()
    {
        vertices = new Vector3[] { new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0), new Vector3(1, 0, 1) };
        triangles = new int[] { 0, 1, 2, 2, 1, 3 };
        uvs = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) };
    }

    void CreateMesh(Mesh mesh)
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
    }

    public void DestroyObjects()
    {
        GameObject[] toDestroy;
        toDestroy = GameObject.FindGameObjectsWithTag("WorldBlocks");
        print(toDestroy.Length);
        foreach (GameObject obj in toDestroy)
        {
            if (obj.name.Contains("(Clone)") || obj.name.Contains("chunk"))
            {
                Destroy(obj);
            }
        }
    }

    public void DestroyChunk(Vector2 chunk)
    {
        for (int i = 0; i < 3; i++)
        {
            string name = i switch
            {
                0 => "chunkTop",
                1 => "chunkBottom",
                2 => "chunkSide",
                _ => "chunk",
            };
            name += "_" + (int)chunk.x + "," + (int)chunk.y;

            Destroy(GameObject.Find(name));
        }
    }

    public void DestroyBlock(Vector3 pos)
    {
        modifiedBlocks[new Vector3Int((int)pos.x, (int)pos.z, Mathf.CeilToInt(pos.y))] = 0;

        ReloadChunksAtPos(pos);
    }

    public void AddBlock(Vector3 pos, Vector3 hitNormal, int blockId)
    {
        Vector3 newPos = pos + hitNormal;
        if (newPos.x < 0 || newPos.x >= mapSize.x || newPos.z < 0 || newPos.z >= mapSize.y || newPos.y < -0.5 || newPos.y > maxHeight)
        {
            return;
        }

        modifiedBlocks[new Vector3Int((int)newPos.x, (int)newPos.z, Mathf.CeilToInt(newPos.y))] = (byte)blockId;

        ReloadChunksAtPos(pos);
    }

    void ReloadChunksAtPos(Vector3 pos) //if position is at chunk border, reloads neighboring chunk
    {
        Vector2Int chunk = new Vector2Int(Mathf.FloorToInt(pos.x / chunkSize.x), Mathf.FloorToInt(pos.z / chunkSize.y));

        //RequestMapData(chunk, OnMapDataReceived);
        DestroyChunk(chunk);
        GenerateChunk(chunk);

        if ((int)(pos.x % chunkSize.x) == 0 && chunk.x > 0)
        {
            DestroyChunk(new Vector2(chunk.x - 1, chunk.y));
            GenerateChunk(new Vector2(chunk.x - 1, chunk.y));
        }
        else if ((int)(pos.x % chunkSize.x) == (int)((chunkSize.x - 1) % chunkSize.x) && (chunk.x + 1) * chunkSize.x < mapSize.x)
        {
            DestroyChunk(new Vector2(chunk.x + 1, chunk.y));
            GenerateChunk(new Vector2(chunk.x + 1, chunk.y));
        }
        if ((int)(pos.z % chunkSize.y) == 0 && chunk.y > 0)
        {
            DestroyChunk(new Vector2(chunk.x, chunk.y - 1));
            GenerateChunk(new Vector2(chunk.x, chunk.y - 1));
        }
        else if ((int)(pos.z % chunkSize.y) == (int)(chunkSize.y - 1) && (chunk.y + 1) * chunkSize.y < mapSize.y)
        {
            DestroyChunk(new Vector2(chunk.x, chunk.y + 1));
            GenerateChunk(new Vector2(chunk.x, chunk.y + 1));
        }
    }
}

public struct BlockInfo
{
    public readonly byte blockId;
    public readonly Vector3 transform;
    public readonly Quaternion rotation;

    public BlockInfo(byte blockId, Vector3 transform, Quaternion rotation)
    {
        this.blockId = blockId;
        this.transform = transform;
        this.rotation = rotation;
    }
}

public class MapData
{
    public Vector2 chunk;
    public List<BlockInfo> chunksTop;
    public List<BlockInfo> chunksBottom;
    public List<BlockInfo> chunksSide;

    public MapData(Vector2 chunk, List<BlockInfo> chunksTop, List<BlockInfo> chunksBottom, List<BlockInfo> chunksSide)
    {
        this.chunk = chunk;
        this.chunksTop = chunksTop;
        this.chunksBottom = chunksBottom;
        this.chunksSide = chunksSide;
    }
}

public class MeshData
{
    //public Mesh[] meshes;
    public Vector3[][] vertices;
    public int[][] triangles;
    public Vector2[][] uvs;
    public Vector2 chunk;

    public MeshData(Vector3[][] vertices, int[][] triangles, Vector2[][] uvs, Vector2 chunk)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.uvs = uvs;
        this.chunk = chunk;
    }

    //public void CreateMesh(int index, Vector3[] vertices, int[] triangles, Vector2[] uvs)
    //{
    //    this.meshes[index] = new Mesh();
    //    this.meshes[index].vertices = vertices;
    //    this.meshes[index].triangles = triangles;
    //    this.meshes[index].uv = uvs;
    //    this.meshes[index].RecalculateNormals();
    //}

    //public void CreateAllMeshes()
    //{
    //    meshes = new Mesh[this.vertices.Length];
    //    for (int i = 0; i < this.meshes.Length; i++)
    //    {
    //        this.CreateMesh(i, this.vertices[i], this.triangles[i], this.uvs[i]);
    //    }
    //}
}

struct MapThreadInfo<T>
{
    public readonly Action<T> callback;
    public readonly T parameter;

    public MapThreadInfo(Action<T> callback, T parameter)
    {
        this.callback = callback;
        this.parameter = parameter;
    }
}

public class TerrainChunk
{
    GameObject meshObject;
    Vector2 position;
}