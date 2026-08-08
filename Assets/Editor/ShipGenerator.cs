using UnityEngine;
using UnityEditor;

public class ShipGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Brig Ship")]
    public static void GenerateShip()
    {
        // 0. Очистка старых версий
        GameObject existing = GameObject.Find("Brig_Ship");
        if (existing != null) DestroyImmediate(existing);

        // 1. Создаем корень
        GameObject shipRoot = new GameObject("Brig_Ship");
        shipRoot.transform.position = new Vector3(0, 1.5f, 0);
        
        Rigidbody rb = shipRoot.AddComponent<Rigidbody>();
        rb.mass = 3000f; 
        rb.drag = 0.5f;
        rb.angularDrag = 1.0f;
        
        // 2. Материалы
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        Material woodMat = CreateOrLoadMaterial("Assets/Materials/Wood.mat", new Color(0.35f, 0.20f, 0.10f));
        Material sailMat = CreateOrLoadMaterial("Assets/Materials/Sail.mat", new Color(0.9f, 0.9f, 0.85f));
        Material darkWoodMat = CreateOrLoadMaterial("Assets/Materials/DarkWood.mat", new Color(0.2f, 0.1f, 0.05f));

        // 3. ПРОЦЕДУРНЫЙ КОРПУС (Математическая генерация меша)
        GameObject hull = new GameObject("ProceduralHull");
        hull.transform.parent = shipRoot.transform;
        hull.transform.localPosition = Vector3.zero;
        
        MeshFilter hullMf = hull.AddComponent<MeshFilter>();
        hullMf.sharedMesh = GenerateProceduralHull(12f, 4f, 2.5f, 20, 10); // Длина 12, Ширина 4, Глубина 2.5
        
        MeshRenderer hullMr = hull.AddComponent<MeshRenderer>();
        hullMr.sharedMaterial = woodMat;
        
        MeshCollider hullCol = hull.AddComponent<MeshCollider>();
        hullCol.sharedMesh = hullMf.sharedMesh;
        hullCol.convex = true; // Для Rigidbody нужен выпуклый коллайдер

        // 4. Мачты (Оставим цилиндры, так как мачта математически и есть цилиндр)
        GameObject mastsGroup = new GameObject("Masts");
        mastsGroup.transform.parent = shipRoot.transform;
        mastsGroup.transform.localPosition = Vector3.zero;

        CreatePart("MainMast", PrimitiveType.Cylinder, mastsGroup.transform, new Vector3(0, 4f, -1.5f), new Vector3(0.3f, 4f, 0.3f), darkWoodMat);
        CreatePart("MainYard", PrimitiveType.Cylinder, mastsGroup.transform, new Vector3(0, 5.5f, -1.3f), new Vector3(0.1f, 2.5f, 0.1f), darkWoodMat).transform.localRotation = Quaternion.Euler(0, 0, 90);
        
        CreatePart("ForeMast", PrimitiveType.Cylinder, mastsGroup.transform, new Vector3(0, 3.5f, 2.5f), new Vector3(0.25f, 3.5f, 0.25f), darkWoodMat);
        CreatePart("ForeYard", PrimitiveType.Cylinder, mastsGroup.transform, new Vector3(0, 4.5f, 2.7f), new Vector3(0.1f, 2f, 0.1f), darkWoodMat).transform.localRotation = Quaternion.Euler(0, 0, 90);

        // 5. ПРОЦЕДУРНЫЕ ПАРУСА (Математическая генерация ткани, надутой ветром)
        GameObject sailsGroup = new GameObject("Sails");
        sailsGroup.transform.parent = shipRoot.transform;
        sailsGroup.transform.localPosition = Vector3.zero;

        // Грот-парус
        GameObject mainSail = new GameObject("MainSail");
        mainSail.transform.parent = sailsGroup.transform;
        mainSail.transform.localPosition = new Vector3(0, 4f, -1.0f);
        mainSail.AddComponent<MeshFilter>().sharedMesh = GenerateProceduralSail(5f, 4f, 1.5f, 10);
        mainSail.AddComponent<MeshRenderer>().sharedMaterial = sailMat;

        // Фок-парус
        GameObject foreSail = new GameObject("ForeSail");
        foreSail.transform.parent = sailsGroup.transform;
        foreSail.transform.localPosition = new Vector3(0, 3f, 3.0f);
        foreSail.AddComponent<MeshFilter>().sharedMesh = GenerateProceduralSail(4f, 3f, 1.2f, 10);
        foreSail.AddComponent<MeshRenderer>().sharedMaterial = sailMat;

        // Очищаем лишние коллайдеры с примитивов (мачт), чтобы не ломать центр масс
        foreach (Collider col in mastsGroup.GetComponentsInChildren<Collider>()) DestroyImmediate(col);

        Selection.activeGameObject = shipRoot;
        Debug.Log("Procedural Brig Ship generated! Look at that beautiful math!");
    }

    // === ПРОЦЕДУРНАЯ ГЕНЕРАЦИЯ КОРПУСА ===
    static Mesh GenerateProceduralHull(float length, float width, float depth, int segZ, int segU)
    {
        Mesh m = new Mesh();
        m.name = "HullMesh";
        
        int numVerts = (segZ + 1) * (segU + 1);
        Vector3[] vertices = new Vector3[numVerts];
        Vector2[] uvs = new Vector2[numVerts];
        
        for (int z = 0, i = 0; z <= segZ; z++)
        {
            float tz = z / (float)segZ; // 0 to 1
            float nz = (tz - 0.5f) * 2f; // -1 (Stern) to 1 (Bow)
            
            // Форма лодки (вид сверху): сзади шире, спереди острый нос (парабола)
            float profileWidth = (nz < 0) ? (1.0f + nz*0.2f) : (1.0f - nz * nz);
            
            // Форма киля (вид сбоку): приподнимается к носу и корме
            float profileDepth = depth * (1.0f - Mathf.Pow(nz, 4f));
            
            for (int u = 0; u <= segU; u++, i++)
            {
                float tu = u / (float)segU; // 0 to 1 (from left rail down to keel up to right rail)
                float nu = (tu - 0.5f) * 2f; // -1 to 1
                
                // U-образный профиль днища
                float curve = 1.0f - Mathf.Pow(1.0f - Mathf.Abs(nu), 1.5f);
                
                float vx = nu * (width / 2f) * profileWidth;
                float vy = -profileDepth * (1.0f - curve);
                float vz = nz * (length / 2f);
                
                // Делаем плоскую корму, "срезая" заднюю часть
                if (nz == -1f) vz += 0.5f; 

                vertices[i] = new Vector3(vx, vy, vz);
                uvs[i] = new Vector2(tu, tz);
            }
        }
        
        m.vertices = vertices;
        m.uv = uvs;
        m.triangles = GenerateGridTriangles(segU, segZ);
        m.RecalculateNormals();
        return m;
    }

    // === ПРОЦЕДУРНАЯ ГЕНЕРАЦИЯ ПАРУСА (С ВЕТРОМ) ===
    static Mesh GenerateProceduralSail(float width, float height, float windDepth, int segments)
    {
        Mesh m = new Mesh();
        m.name = "SailMesh";
        
        int numVerts = (segments + 1) * (segments + 1);
        Vector3[] vertices = new Vector3[numVerts];
        Vector2[] uvs = new Vector2[numVerts];
        
        for (int y = 0, i = 0; y <= segments; y++)
        {
            float ty = y / (float)segments;
            for (int x = 0; x <= segments; x++, i++)
            {
                float tx = x / (float)segments;
                
                float vx = (tx - 0.5f) * width;
                float vy = (ty - 0.5f) * height;
                
                // Математическая кривая надутого паруса (синусоида по обеим осям)
                float bulge = Mathf.Sin(tx * Mathf.PI) * Mathf.Sin(ty * Mathf.PI);
                float vz = bulge * windDepth; // Парус выгибается вперед
                
                // Нижние углы паруса стягиваются к центру (как настоящие снасти)
                if (ty < 0.2f) vx *= Mathf.Lerp(0.6f, 1.0f, ty / 0.2f);
                
                vertices[i] = new Vector3(vx, vy, vz);
                uvs[i] = new Vector2(tx, ty);
            }
        }
        
        m.vertices = vertices;
        m.uv = uvs;
        m.triangles = GenerateGridTriangles(segments, segments, true); // true для двустороннего рендера
        m.RecalculateNormals();
        return m;
    }

    static int[] GenerateGridTriangles(int segX, int segY, bool doubleSided = false)
    {
        int multiplier = doubleSided ? 12 : 6;
        int[] tris = new int[segX * segY * multiplier];
        for (int y = 0, ti = 0, vi = 0; y < segY; y++, vi++)
        {
            for (int x = 0; x < segX; x++, ti += 6, vi++)
            {
                tris[ti] = vi;
                tris[ti+1] = vi + segX + 1;
                tris[ti+2] = vi + 1;
                tris[ti+3] = vi + 1;
                tris[ti+4] = vi + segX + 1;
                tris[ti+5] = vi + segX + 2;
                
                if (doubleSided)
                {
                    // Обратная сторона паруса
                    tris[ti+6] = vi;
                    tris[ti+7] = vi + 1;
                    tris[ti+8] = vi + segX + 1;
                    tris[ti+9] = vi + 1;
                    tris[ti+10] = vi + segX + 2;
                    tris[ti+11] = vi + segX + 1;
                }
            }
        }
        return tris;
    }

    static GameObject CreatePart(string name, PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name; go.transform.parent = parent; go.transform.localPosition = localPos;
        go.transform.localScale = localScale; go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static Material CreateOrLoadMaterial(string path, Color color)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Standard")); mat.color = color; mat.SetFloat("_Glossiness", 0.1f); AssetDatabase.CreateAsset(mat, path); }
        else { mat.color = color; }
        return mat;
    }
}
