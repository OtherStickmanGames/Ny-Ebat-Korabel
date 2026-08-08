using UnityEngine;
using UnityEditor;

public class WaterSceneSetup : EditorWindow
{
    static Mesh GenerateDensePlane(float size, int segments)
    {
        Mesh m = new Mesh();
        m.name = "DenseWaterPlane";
        
        int numVertices = (segments + 1) * (segments + 1);
        Vector3[] vertices = new Vector3[numVertices];
        Vector2[] uvs = new Vector2[numVertices];
        Vector3[] normals = new Vector3[numVertices];
        
        for (int z = 0, i = 0; z <= segments; z++)
        {
            for (int x = 0; x <= segments; x++, i++)
            {
                vertices[i] = new Vector3((x / (float)segments - 0.5f) * size, 0, (z / (float)segments - 0.5f) * size);
                uvs[i] = new Vector2(x / (float)segments, z / (float)segments);
                normals[i] = Vector3.up;
            }
        }
        
        int[] triangles = new int[segments * segments * 6];
        for (int ti = 0, vi = 0, y = 0; y < segments; y++, vi++)
        {
            for (int x = 0; x < segments; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 1] = vi + segments + 1;
                triangles[ti + 2] = vi + 1;
                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + segments + 1;
                triangles[ti + 5] = vi + segments + 2;
            }
        }
        
        m.vertices = vertices;
        m.uv = uvs;
        m.normals = normals;
        m.triangles = triangles;
        m.RecalculateBounds();
        return m;
    }

    [MenuItem("Tools/Setup Water Test Scene")]
    public static void SetupScene()
    {
        // 1. Create High-Density Water Plane
        GameObject waterObj = new GameObject("WaterSurface");
        waterObj.transform.position = Vector3.zero;
        
        MeshFilter mf = waterObj.AddComponent<MeshFilter>();
        // Генерируем меш 100х100 метров, разбитый на 100 сегментов (10 000 вершин)
        mf.sharedMesh = GenerateDensePlane(100f, 100); 
        
        MeshRenderer mr = waterObj.AddComponent<MeshRenderer>();

        // 2. Create Material
        Shader waterShader = Shader.Find("Custom/Quest3D_Style_Water");
        if (waterShader == null)
        {
            Debug.LogError("Could not find shader 'Custom/Quest3D_Style_Water'. Please ensure it's compiled without errors.");
            return;
        }

        Material waterMat = new Material(waterShader);
        
        // Ensure Materials folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        
        // Save material as an asset
        AssetDatabase.CreateAsset(waterMat, "Assets/Materials/WaterTestMaterial.mat");
        
        // Setup default normal maps if possible, but unity defaults to grey if none assigned.
        // The user will need to assign water normal maps for the ripples to look good.
        
        // Assign material
        waterObj.GetComponent<MeshRenderer>().material = waterMat;

        // 3. Create environment to demonstrate depth and foam
        // A large pool bottom
        GameObject bottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bottom.name = "PoolBottom";
        bottom.transform.position = new Vector3(0, -3, 0);
        bottom.transform.localScale = new Vector3(100, 1, 100);
        
        // An island sphere intersecting the water
        GameObject island = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        island.name = "Island_Sphere";
        island.transform.position = new Vector3(0, -1, 0);
        island.transform.localScale = new Vector3(10, 10, 10);
        
        // A slope intersecting the water to show depth gradient
        GameObject slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slope.name = "Shore_Slope";
        slope.transform.position = new Vector3(15, -2, 0);
        slope.transform.rotation = Quaternion.Euler(0, 0, 15);
        slope.transform.localScale = new Vector3(20, 1, 10);

        // 4. Setup Camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            // Crucial for depth shader to work in Built-in!
            cam.depthTextureMode = DepthTextureMode.Depth;
            
            cam.transform.position = new Vector3(0, 5, -15);
            cam.transform.LookAt(Vector3.zero);
        }
        else
        {
            Debug.LogWarning("Main Camera not found. Please ensure your camera has DepthTextureMode.Depth enabled.");
        }

        // Add a helper component to camera to force depth mode on Play
        if (cam != null && cam.gameObject.GetComponent<ForceCameraDepth>() == null)
        {
            cam.gameObject.AddComponent<ForceCameraDepth>();
        }

        Debug.Log("Water Test Scene generated successfully! Check your Scene view.");
    }
}

// Helper script to ensure camera renders depth texture when running the game
[ExecuteInEditMode]
public class ForceCameraDepth : MonoBehaviour
{
    void OnEnable()
    {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
    }
}
