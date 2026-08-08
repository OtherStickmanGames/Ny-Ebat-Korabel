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
        shipRoot.transform.position = new Vector3(0, 2f, 0);
        
        Rigidbody rb = shipRoot.AddComponent<Rigidbody>();
        rb.mass = 5000f; // Реалистичный вес
        rb.drag = 0.5f;
        rb.angularDrag = 1.0f;
        
        // 2. Материалы
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        Material woodMat = CreateOrLoadMaterial("Assets/Materials/Wood.mat", new Color(0.30f, 0.18f, 0.10f), 0.05f);
        Material darkWoodMat = CreateOrLoadMaterial("Assets/Materials/DarkWood.mat", new Color(0.15f, 0.08f, 0.04f), 0.05f);
        Material sailMat = CreateOrLoadMaterial("Assets/Materials/Sail.mat", new Color(0.92f, 0.90f, 0.85f), 0.0f);
        Material ropeMat = CreateOrLoadMaterial("Assets/Materials/Rope.mat", new Color(0.05f, 0.05f, 0.05f), 0.0f);
        Material ironMat = CreateOrLoadMaterial("Assets/Materials/Iron.mat", new Color(0.15f, 0.15f, 0.15f), 0.3f);
        ironMat.SetFloat("_Metallic", 0.9f);
        
        Material windowMat = CreateOrLoadMaterial("Assets/Materials/Window.mat", new Color(0.8f, 0.7f, 0.2f), 0.8f);
        windowMat.EnableKeyword("_EMISSION");
        windowMat.SetColor("_EmissionColor", new Color(0.8f, 0.7f, 0.2f) * 0.8f);

        // 3. СВЕРХ-РЕАЛИСТИЧНЫЙ КОРПУС (Naval Architecture Math)
        GameObject hull = new GameObject("RealisticHull");
        hull.transform.parent = shipRoot.transform;
        hull.transform.localPosition = Vector3.zero;
        
        MeshFilter hullMf = hull.AddComponent<MeshFilter>();
        hullMf.sharedMesh = GenerateRealisticHull(18f, 5.5f, 3f, 50, 30); // Еще больше полигонов!
        hull.AddComponent<MeshRenderer>().sharedMaterial = woodMat;
        
        MeshCollider hullCol = hull.AddComponent<MeshCollider>();
        hullCol.sharedMesh = hullMf.sharedMesh;
        hullCol.convex = true;

        // 4. МАЧТЫ И ПАРУСА (Составные, как у реальных кораблей)
        GameObject riggingGroup = new GameObject("Rigging");
        riggingGroup.transform.parent = shipRoot.transform;
        riggingGroup.transform.localPosition = Vector3.zero;

        // Бушприт (Bowsprit - передняя наклонная мачта)
        CreatePart("Bowsprit", PrimitiveType.Cylinder, riggingGroup.transform, new Vector3(0, 2.5f, 9.5f), new Vector3(0.2f, 3.5f, 0.2f), darkWoodMat).transform.localRotation = Quaternion.Euler(65, 0, 0);
        
        // Фок-мачта (Foremast)
        BuildMast(riggingGroup.transform, "Foremast", new Vector3(0, 1.5f, 4.5f), 10f, 0.35f, darkWoodMat, sailMat);
        
        // Грот-мачта (Mainmast) - самая высокая
        BuildMast(riggingGroup.transform, "Mainmast", new Vector3(0, 1.0f, -2.0f), 12f, 0.4f, darkWoodMat, sailMat);

        // Бизань или Спанкер (задний косой парус)
        GameObject spanker = new GameObject("Spanker_Sail");
        spanker.transform.parent = riggingGroup.transform;
        spanker.transform.localPosition = new Vector3(0, 4.5f, -4.5f);
        spanker.transform.localRotation = Quaternion.Euler(0, 90, 0);
        spanker.AddComponent<MeshFilter>().sharedMesh = GenerateProceduralSail(4f, 5f, 0.5f, 15);
        spanker.AddComponent<MeshRenderer>().sharedMaterial = sailMat;

        // 5. ПУШКИ (Cannon Deck)
        GameObject cannonsGroup = new GameObject("Cannons");
        cannonsGroup.transform.parent = shipRoot.transform;
        cannonsGroup.transform.localPosition = Vector3.zero;
        
        for (float z = -4f; z <= 6f; z += 2.5f)
        {
            // Учитываем сужение корпуса на краях для правильного расставления пушек
            float widthTaper = (z > 0) ? (1.0f - Mathf.Pow(z/9f, 2f)) : (1.0f - Mathf.Pow(z/9f, 4f)*0.3f);
            float xPos = (5.5f / 2f) * widthTaper * 0.95f;
            
            CreateCannon(cannonsGroup.transform, new Vector3(-xPos, 1.2f, z), true, ironMat, darkWoodMat);
            CreateCannon(cannonsGroup.transform, new Vector3(xPos, 1.2f, z), false, ironMat, darkWoodMat);
        }

        // 6. КАНАТЫ / ТАКЕЛАЖ (Rigging Lines)
        GameObject ropesGroup = new GameObject("Ropes");
        ropesGroup.transform.parent = shipRoot.transform;
        ropesGroup.transform.localPosition = Vector3.zero;

        // Штаги (Продольные канаты)
        CreateRope(ropesGroup.transform, new Vector3(0, 3f, 12f), new Vector3(0, 7f, 4.5f), 0.03f, ropeMat);
        CreateRope(ropesGroup.transform, new Vector3(0, 5f, 13f), new Vector3(0, 9f, 4.5f), 0.02f, ropeMat);
        CreateRope(ropesGroup.transform, new Vector3(0, 7f, 4.5f), new Vector3(0, 10f, -2.0f), 0.04f, ropeMat);
        CreateRope(ropesGroup.transform, new Vector3(0, 9f, 4.5f), new Vector3(0, 12f, -2.0f), 0.03f, ropeMat);

        // Ванты (Боковые поддерживающие канаты)
        for (float z = 3.5f; z <= 5.5f; z += 0.5f)
        {
            CreateRope(ropesGroup.transform, new Vector3(-2.6f, 1.8f, z), new Vector3(0, 6.5f, 4.5f), 0.015f, ropeMat);
            CreateRope(ropesGroup.transform, new Vector3(2.6f, 1.8f, z), new Vector3(0, 6.5f, 4.5f), 0.015f, ropeMat);
        }
        for (float z = -4.0f; z <= -1.0f; z += 0.6f)
        {
            CreateRope(ropesGroup.transform, new Vector3(-2.6f, 1.8f, z), new Vector3(0, 7.5f, -2.0f), 0.015f, ropeMat);
            CreateRope(ropesGroup.transform, new Vector3(2.6f, 1.8f, z), new Vector3(0, 7.5f, -2.0f), 0.015f, ropeMat);
        }

        // 7. МЕЛКИЕ ДЕТАЛИ (Окна, Штурвал)
        GameObject detailsGroup = new GameObject("Details");
        detailsGroup.transform.parent = shipRoot.transform;
        detailsGroup.transform.localPosition = Vector3.zero;

        // Капитанские окна на корме
        for (float x = -1.2f; x <= 1.2f; x += 0.8f)
        {
            GameObject win = CreatePart("Window", PrimitiveType.Cube, detailsGroup.transform, new Vector3(x, 3.2f, -8.7f), new Vector3(0.5f, 0.8f, 0.1f), windowMat);
            win.transform.localRotation = Quaternion.Euler(-10, 0, 0); // Наклон по транцу
        }
        
        // Штурвал (Helm)
        CreatePart("HelmBase", PrimitiveType.Cube, detailsGroup.transform, new Vector3(0, 2.2f, -6f), new Vector3(0.3f, 0.8f, 0.3f), darkWoodMat);
        CreatePart("HelmWheel", PrimitiveType.Cylinder, detailsGroup.transform, new Vector3(0, 2.6f, -5.8f), new Vector3(0.8f, 0.05f, 0.8f), darkWoodMat).transform.localRotation = Quaternion.Euler(90, 0, 0);


        // 8. ОЧИСТКА КОЛЛАЙДЕРОВ (Ради идеальной физики)
        foreach (Collider col in shipRoot.GetComponentsInChildren<Collider>())
        {
            if (col.gameObject.name != "RealisticHull") DestroyImmediate(col);
        }

        Selection.activeGameObject = shipRoot;
        Debug.Log("Ultra-Realistic Procedural Brig Ship generated! Look at all those details!");
    }

    // === СОЗДАНИЕ ПУШКИ ===
    static void CreateCannon(Transform parent, Vector3 pos, bool isLeft, Material ironMat, Material woodMat)
    {
        // Крышка порта
        GameObject hatch = CreatePart("PortHatch", PrimitiveType.Cube, parent, pos + new Vector3(isLeft ? 0.05f : -0.05f, 0.3f, 0), new Vector3(0.1f, 0.5f, 0.6f), woodMat);
        hatch.transform.localRotation = Quaternion.Euler(0, 0, isLeft ? -70 : 70); // Открыта вверх
        
        // Ствол пушки
        GameObject barrel = CreatePart("Barrel", PrimitiveType.Cylinder, parent, pos, new Vector3(0.12f, 0.5f, 0.12f), ironMat);
        barrel.transform.localRotation = Quaternion.Euler(0, 0, 90);
    }

    // === СОЗДАНИЕ КАНАТОВ ===
    static void CreateRope(Transform parent, Vector3 start, Vector3 end, float thickness, Material mat)
    {
        GameObject rope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rope.name = "Rope";
        rope.transform.parent = parent;
        rope.transform.localPosition = (start + end) / 2f;
        float length = Vector3.Distance(start, end);
        rope.transform.localScale = new Vector3(thickness, length / 2f, thickness);
        rope.transform.up = (end - start).normalized;
        rope.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // === ПРОВИНУТАЯ ГЕНЕРАЦИЯ КОРПУСА ===
    static Mesh GenerateRealisticHull(float length, float width, float depth, int segZ, int segU)
    {
        Mesh m = new Mesh();
        m.name = "RealisticHullMesh";
        
        int numVerts = (segZ + 1) * (segU + 1);
        Vector3[] vertices = new Vector3[numVerts];
        Vector2[] uvs = new Vector2[numVerts];
        
        for (int z = 0, i = 0; z <= segZ; z++)
        {
            float tz = z / (float)segZ; 
            float nz = (tz - 0.5f) * 2f; // -1 (Корма) до 1 (Нос)
            
            // 1. Sheer (Изгиб палубы) - палуба задирается на носу и корме
            float sheer = (nz > 0) ? Mathf.Pow(nz, 2.5f) * 1.8f : Mathf.Pow(-nz, 2.0f) * 1.5f;
            
            // 2. Форма ватерлинии (Ширина корпуса)
            float profileWidth;
            if (nz > 0)
                profileWidth = 1.0f - Mathf.Pow(nz, 1.8f); // Острый, слегка вогнутый нос
            else
                profileWidth = 1.0f - Mathf.Pow(-nz, 4.0f) * 0.4f; // Тупая, широкая корма
                
            // 3. Профиль киля
            float profileDepth = depth * (1.0f - Mathf.Pow(Mathf.Abs(nz), 3.0f));
            
            for (int u = 0; u <= segU; u++, i++)
            {
                float tu = u / (float)segU; 
                float nu = (tu - 0.5f) * 2f; // -1 (Левый борт) до 1 (Правый борт)
                
                float absNu = Mathf.Abs(nu);
                
                // 4. Tumblehome (Завал бортов внутрь)
                float tumblehome = (absNu > 0.8f) ? (1.0f - (absNu - 0.8f)*0.5f) : 1.0f;
                
                // Форма сечения корпуса (бочкообразная)
                float hFactor = 1.0f - Mathf.Pow(1.0f - absNu, 1.8f);
                
                float vx = Mathf.Sign(nu) * hFactor * (width / 2f) * profileWidth * tumblehome;
                
                float vy;
                if (absNu > 0.95f) // Бортики палубы (Bulwarks)
                {
                    vy = sheer + 1.2f; 
                }
                else if (absNu > 0.9f) // Сама палуба
                {
                    vy = sheer;
                }
                else // Днище
                {
                    float v_interp = absNu / 0.9f; 
                    float curve = Mathf.Pow(v_interp, 0.6f); 
                    vy = Mathf.Lerp(-profileDepth, sheer, curve);
                }
                
                float vz = nz * (length / 2f);
                
                // Скошенный транец (корма)
                if (nz < -0.9f && vy > -1f) vz += (vy + 1f) * 0.3f;
                
                // Бушпритная площадка (выступ на носу)
                if (nz > 0.95f && vy > sheer) vz += 0.5f;

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

    // === СОСТАВНАЯ МАЧТА ===
    static void BuildMast(Transform parent, string name, Vector3 pos, float height, float baseRad, Material woodMat, Material sailMat)
    {
        GameObject mastGroup = new GameObject(name);
        mastGroup.transform.parent = parent;
        mastGroup.transform.localPosition = pos;

        // Реальные корабли строятся из трех секций мачт
        // 1. Нижняя мачта (Lower Mast)
        CreatePart("LowerMast", PrimitiveType.Cylinder, mastGroup.transform, new Vector3(0, height*0.25f, 0), new Vector3(baseRad, height*0.25f, baseRad), woodMat);
        
        // Марсовая площадка (Crows Nest / Top)
        CreatePart("CrowsNest", PrimitiveType.Cylinder, mastGroup.transform, new Vector3(0, height*0.5f, 0), new Vector3(baseRad*5f, 0.05f, baseRad*5f), woodMat);
        
        // 2. Стеньга (Topmast)
        CreatePart("TopMast", PrimitiveType.Cylinder, mastGroup.transform, new Vector3(0, height*0.75f, 0.1f), new Vector3(baseRad*0.7f, height*0.2f, baseRad*0.7f), woodMat);
        
        // 3. Брам-стеньга (Topgallant Mast)
        CreatePart("TopgallantMast", PrimitiveType.Cylinder, mastGroup.transform, new Vector3(0, height*1.05f, 0.15f), new Vector3(baseRad*0.4f, height*0.1f, baseRad*0.4f), woodMat);
        
        // Многоуровневые паруса
        CreateYardAndSail(mastGroup.transform, "Course", height*0.42f, baseRad*20f, height*0.35f, sailMat, woodMat);
        CreateYardAndSail(mastGroup.transform, "Topsail", height*0.85f, baseRad*15f, height*0.30f, sailMat, woodMat);
        CreateYardAndSail(mastGroup.transform, "Topgallant", height*1.12f, baseRad*10f, height*0.20f, sailMat, woodMat);
    }
    
    static void CreateYardAndSail(Transform parent, string name, float yPos, float width, float height, Material sailMat, Material woodMat)
    {
        // Рея (Yard - перекладина)
        CreatePart(name + "_Yard", PrimitiveType.Cylinder, parent, new Vector3(0, yPos, -0.2f), new Vector3(0.08f, width*0.5f, 0.08f), woodMat).transform.localRotation = Quaternion.Euler(0, 0, 90);
        
        // Процедурный надутый парус
        GameObject sail = new GameObject(name + "_Sail");
        sail.transform.parent = parent;
        sail.transform.localPosition = new Vector3(0, yPos - height*0.5f, -0.3f);
        sail.AddComponent<MeshFilter>().sharedMesh = GenerateProceduralSail(width, height, height*0.4f, 15);
        sail.AddComponent<MeshRenderer>().sharedMaterial = sailMat;
    }

    // === ПРОЦЕДУРНАЯ ГЕНЕРАЦИЯ ПАРУСА ===
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
                
                // Кривая надутого паруса
                float bulge = Mathf.Sin(tx * Mathf.PI) * Mathf.Sin(ty * Mathf.PI);
                float vz = bulge * windDepth; 
                
                // Стягивание нижних углов (шкаторин)
                if (ty < 0.2f) vx *= Mathf.Lerp(0.7f, 1.0f, ty / 0.2f);
                
                vertices[i] = new Vector3(vx, vy, vz);
                uvs[i] = new Vector2(tx, ty);
            }
        }
        
        m.vertices = vertices;
        m.uv = uvs;
        m.triangles = GenerateGridTriangles(segments, segments, true); 
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
                tris[ti] = vi; tris[ti+1] = vi + segX + 1; tris[ti+2] = vi + 1;
                tris[ti+3] = vi + 1; tris[ti+4] = vi + segX + 1; tris[ti+5] = vi + segX + 2;
                if (doubleSided)
                {
                    tris[ti+6] = vi; tris[ti+7] = vi + 1; tris[ti+8] = vi + segX + 1;
                    tris[ti+9] = vi + 1; tris[ti+10] = vi + segX + 2; tris[ti+11] = vi + segX + 1;
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

    static Material CreateOrLoadMaterial(string path, Color color, float glossiness)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) 
        { 
            mat = new Material(Shader.Find("Standard")); 
            AssetDatabase.CreateAsset(mat, path); 
        }
        mat.color = color; 
        mat.SetFloat("_Glossiness", glossiness);
        return mat;
    }
}
