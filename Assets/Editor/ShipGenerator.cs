using UnityEngine;
using UnityEditor;

public class ShipGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Brig Ship")]
    public static void GenerateShip()
    {
        // 1. Create root object
        GameObject shipRoot = new GameObject("Brig_Ship");
        shipRoot.transform.position = new Vector3(0, 1.5f, 0); // Немного приподнимаем над водой
        
        // Физика (Rigidbody)
        Rigidbody rb = shipRoot.AddComponent<Rigidbody>();
        rb.mass = 2000f; // 2 тонны
        rb.drag = 0.2f;
        rb.angularDrag = 1.0f;
        
        // 2. Создание Материалов
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) 
            AssetDatabase.CreateFolder("Assets", "Materials");

        Material woodMat = CreateOrLoadMaterial("Assets/Materials/Wood.mat", new Color(0.35f, 0.20f, 0.10f));
        Material lightWoodMat = CreateOrLoadMaterial("Assets/Materials/LightWood.mat", new Color(0.55f, 0.40f, 0.25f));
        Material sailMat = CreateOrLoadMaterial("Assets/Materials/Sail.mat", new Color(0.9f, 0.9f, 0.85f));
        Material darkWoodMat = CreateOrLoadMaterial("Assets/Materials/DarkWood.mat", new Color(0.2f, 0.1f, 0.05f));

        // 3. Построение корпуса (Hull)
        GameObject hullGroup = new GameObject("Hull");
        hullGroup.transform.parent = shipRoot.transform;
        hullGroup.transform.localPosition = Vector3.zero;

        // Основной корпус
        CreatePart("MainBody", PrimitiveType.Cube, hullGroup.transform, new Vector3(0, 0, 0), new Vector3(3f, 1.5f, 8f), woodMat);
        
        // Нос корабля (Bow) - повернутый куб, образующий клин
        GameObject bow = CreatePart("Bow", PrimitiveType.Cube, hullGroup.transform, new Vector3(0, 0, 4.5f), new Vector3(2.12f, 1.5f, 2.12f), woodMat);
        bow.transform.localRotation = Quaternion.Euler(0, 45, 0);
        
        // Корма (Stern) - возвышение сзади (капитанский мостик)
        CreatePart("Stern", PrimitiveType.Cube, hullGroup.transform, new Vector3(0, 0.5f, -4.5f), new Vector3(3.2f, 2.5f, 2f), woodMat);
        CreatePart("SternRoof", PrimitiveType.Cube, hullGroup.transform, new Vector3(0, 1.8f, -4.5f), new Vector3(3.4f, 0.2f, 2.2f), darkWoodMat);
        
        // Палуба (Deck)
        CreatePart("Deck", PrimitiveType.Cube, hullGroup.transform, new Vector3(0, 0.76f, 0.5f), new Vector3(2.8f, 0.1f, 9.5f), lightWoodMat);
        
        // Борта (Rails)
        CreatePart("LeftRail", PrimitiveType.Cube, hullGroup.transform, new Vector3(-1.4f, 1.1f, 0), new Vector3(0.2f, 0.5f, 8f), darkWoodMat);
        CreatePart("RightRail", PrimitiveType.Cube, hullGroup.transform, new Vector3(1.4f, 1.1f, 0), new Vector3(0.2f, 0.5f, 8f), darkWoodMat);

        // 4. Мачты (Brig имеет две мачты)
        GameObject mastsGroup = new GameObject("Masts");
        mastsGroup.transform.parent = shipRoot.transform;
        mastsGroup.transform.localPosition = Vector3.zero;

        // Грот-мачта (Mainmast - задняя, самая высокая)
        CreatePart("MainMast", PrimitiveType.Cylinder, mastsGroup.transform, new Vector3(0, 4f, -1.5f), new Vector3(0.3f, 4f, 0.3f), darkWoodMat);
        CreatePart("MainYard", PrimitiveType.Cylinder, mastsGroup.transform, new Vector3(0, 5.5f, -1.3f), new Vector3(0.1f, 2.5f, 0.1f), darkWoodMat).transform.localRotation = Quaternion.Euler(0, 0, 90);
        
        // Фок-мачта (Foremast - передняя)
        CreatePart("ForeMast", PrimitiveType.Cylinder, mastsGroup.transform, new Vector3(0, 3.5f, 2.5f), new Vector3(0.25f, 3.5f, 0.25f), darkWoodMat);
        CreatePart("ForeYard", PrimitiveType.Cylinder, mastsGroup.transform, new Vector3(0, 4.5f, 2.7f), new Vector3(0.1f, 2f, 0.1f), darkWoodMat).transform.localRotation = Quaternion.Euler(0, 0, 90);
        
        // Бушприт (Bowsprit - мачта спереди под углом)
        GameObject bowsprit = CreatePart("Bowsprit", PrimitiveType.Cylinder, mastsGroup.transform, new Vector3(0, 1.5f, 6.5f), new Vector3(0.15f, 2f, 0.15f), darkWoodMat);
        bowsprit.transform.localRotation = Quaternion.Euler(70, 0, 0);

        // 5. Паруса (Sails) - сплюснутые сферы для имитации надутости
        GameObject sailsGroup = new GameObject("Sails");
        sailsGroup.transform.parent = shipRoot.transform;
        sailsGroup.transform.localPosition = Vector3.zero;

        GameObject mainSail = CreatePart("MainSail", PrimitiveType.Sphere, sailsGroup.transform, new Vector3(0, 3.5f, -0.8f), new Vector3(4.8f, 3.8f, 1.0f), sailMat);
        GameObject foreSail = CreatePart("ForeSail", PrimitiveType.Sphere, sailsGroup.transform, new Vector3(0, 2.8f, 3.2f), new Vector3(3.8f, 3.0f, 0.8f), sailMat);
        GameObject jibSail = CreatePart("JibSail", PrimitiveType.Sphere, sailsGroup.transform, new Vector3(0, 2.0f, 5.5f), new Vector3(0.2f, 2.5f, 2.5f), sailMat);

        // 6. Очистка коллайдеров (оставляем коллайдер только на основном корпусе для оптимизации и правильного центра масс)
        foreach (Collider col in shipRoot.GetComponentsInChildren<Collider>())
        {
            if (col.gameObject.name != "MainBody" && col.gameObject.name != "Bow" && col.gameObject.name != "Stern")
            {
                DestroyImmediate(col);
            }
        }

        // Выделяем корабль в иерархии
        Selection.activeGameObject = shipRoot;
        Debug.Log("Brig Ship successfully assembled and painted!");
    }

    // Вспомогательная функция для генерации частей корабля
    static GameObject CreatePart(string name, PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.parent = parent;
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    // Вспомогательная функция для создания или загрузки материала
    static Material CreateOrLoadMaterial(string path, Color color)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            
            // Настраиваем гладкость для дерева и парусины
            mat.SetFloat("_Glossiness", 0.1f);
            
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.color = color; // Обновляем цвет на всякий случай
        }
        return mat;
    }
}
