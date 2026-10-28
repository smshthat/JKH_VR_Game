using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Valve.VR.InteractionSystem;

// Меню Unity: ЖКХ -> Добавить дома и NPC
// Строит дополнительные дома (с крышей, окнами, трубой, мебелью, дверью, светом)
// и в каждом ставит копию вашего NPC (вместе с его моделью, ударом, разговором и анимацией руки)
// со своей купюрой и своей надписью задачи.
// Старые объекты и скрипты не меняются.
public static class AddHousesAndNPCs
{
    // Где стоят новые дома: координата X. Первый дом уже стоит в x = 0 на северной стороне улицы.
    static readonly float[] northX = { -16f, -8f, 8f, 16f };   // та же сторона, что и первый дом
    static readonly float[] southX = { -8f, 0f, 8f };          // противоположная сторона дороги

    static readonly Color[] wallColors =
    {
        new Color(0.85f, 0.8f, 0.7f), new Color(0.75f, 0.85f, 0.8f), new Color(0.9f, 0.82f, 0.6f),
        new Color(0.8f, 0.75f, 0.85f), new Color(0.88f, 0.7f, 0.65f), new Color(0.7f, 0.8f, 0.9f)
    };
    static readonly Color[] roofColors =
    {
        new Color(0.55f, 0.2f, 0.15f), new Color(0.3f, 0.3f, 0.35f), new Color(0.45f, 0.28f, 0.18f)
    };

    [MenuItem("ЖКХ/Добавить дома и NPC")]
    public static void Add()
    {
        if (GameObject.Find("ЖКХ_Дома") != null)
        {
            Debug.LogWarning("ЖКХ: дома уже добавлены. Удалите объект ЖКХ_Дома, если хотите создать их заново.");
            return;
        }

        GameObject origNpc = GameObject.Find("NPC");
        NPCController origCtrl = origNpc != null ? origNpc.GetComponent<NPCController>() : null;
        if (origCtrl == null || origCtrl.money == null || origCtrl.moneyPoint == null)
        {
            Debug.LogWarning("ЖКХ: не найден настроенный NPC (объект NPC с купюрой и точкой купюры). Сначала соберите сцену через ЖКХ -> Собрать сцену.");
            return;
        }

        GameObject root = new GameObject("ЖКХ_Дома");
        Undo.RegisterCreatedObjectUndo(root, "ЖКХ дома");

        // Большая зона телепорта вдоль всей улицы, чтобы можно было дойти до новых домов
        TeleportZone(root, "Телепорт_улица_доп", new Vector3(0, 0.031f, -4.25f), Quaternion.identity, new Vector2(56f, 11.5f));

        int index = 2;   // первый дом уже есть
        foreach (float x in northX)
            BuildHouse(root, index++, new Vector3(x, 0, 0), 0f, origNpc, origCtrl);
        foreach (float x in southX)
            BuildHouse(root, index++, new Vector3(x, 0, -5.1f), 180f, origNpc, origCtrl);

        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("ЖКХ: добавлено домов: " + (index - 2) + ", в каждом есть NPC.");
    }

    // Один дом. Все координаты внутри дома такие же, как у первого дома.
    static void BuildHouse(GameObject root, int idx, Vector3 pos, float yaw, GameObject origNpc, NPCController origCtrl)
    {
        GameObject house = new GameObject("Дом_" + idx);
        house.transform.SetParent(root.transform);
        house.transform.position = pos;
        house.transform.rotation = Quaternion.Euler(0, yaw, 0);

        Color wall = wallColors[idx % wallColors.Length];
        Color roof = roofColors[idx % roofColors.Length];

        // ---------- Пол и стены ----------
        Box(house, "Пол", new Vector3(0, -0.05f, 5), new Vector3(6, 0.1f, 6), new Color(0.6f, 0.45f, 0.3f));
        Box(house, "Стена_задняя", new Vector3(0, 1.3f, 8.1f), new Vector3(6.4f, 2.6f, 0.2f), wall);
        Box(house, "Стена_левая", new Vector3(-3.1f, 1.3f, 5), new Vector3(0.2f, 2.6f, 6.2f), wall);
        Box(house, "Стена_правая", new Vector3(3.1f, 1.3f, 5), new Vector3(0.2f, 2.6f, 6.2f), wall);
        Box(house, "Стена_передняя_лево", new Vector3(-1.75f, 1.3f, 1.9f), new Vector3(2.5f, 2.6f, 0.2f), wall);
        Box(house, "Стена_передняя_право", new Vector3(1.75f, 1.3f, 1.9f), new Vector3(2.5f, 2.6f, 0.2f), wall);
        Box(house, "Стена_над_дверью", new Vector3(0, 2.3f, 1.9f), new Vector3(1f, 0.6f, 0.2f), wall);

        // ---------- Окна снаружи (украшение) ----------
        Color glass = new Color(0.55f, 0.75f, 0.95f);
        NoCollider(Box(house, "Окно_лево", new Vector3(-1.75f, 1.4f, 1.78f), new Vector3(0.9f, 0.9f, 0.04f), glass));
        NoCollider(Box(house, "Окно_право", new Vector3(1.75f, 1.4f, 1.78f), new Vector3(0.9f, 0.9f, 0.04f), glass));
        NoCollider(Box(house, "Окно_сзади", new Vector3(0, 1.4f, 8.22f), new Vector3(1.2f, 0.9f, 0.04f), glass));

        // ---------- Двускатная крыша ----------
        float halfWidth = 3.5f;                 // половина ширины крыши с навесом
        float rise = 1.3f;                      // подъём крыши
        float slopeLen = Mathf.Sqrt(halfWidth * halfWidth + rise * rise);
        float angle = Mathf.Atan2(rise, halfWidth) * Mathf.Rad2Deg;
        float baseY = 2.6f;
        GameObject slabL = Box(house, "Крыша_левая", new Vector3(-halfWidth / 2f, baseY + rise / 2f, 5f), new Vector3(slopeLen, 0.12f, 7f), roof);
        slabL.transform.localRotation = Quaternion.Euler(0, 0, angle);
        GameObject slabR = Box(house, "Крыша_правая", new Vector3(halfWidth / 2f, baseY + rise / 2f, 5f), new Vector3(slopeLen, 0.12f, 7f), roof);
        slabR.transform.localRotation = Quaternion.Euler(0, 0, -angle);

        // Фронтоны (ступенчатые треугольники под крышей, спереди и сзади)
        float[] stepWidth = { 5.6f, 3.4f, 1.1f };
        float stepH = rise / 3f;
        for (int i = 0; i < 3; i++)
        {
            float y = baseY + stepH * (i + 0.5f);
            Box(house, "Фронтон_перед_" + i, new Vector3(0, y, 1.9f), new Vector3(stepWidth[i], stepH, 0.2f), wall);
            Box(house, "Фронтон_зад_" + i, new Vector3(0, y, 8.1f), new Vector3(stepWidth[i], stepH, 0.2f), wall);
        }

        // Труба
        Box(house, "Труба", new Vector3(2f, 3.6f, 6.5f), new Vector3(0.5f, 1.2f, 0.5f), new Color(0.4f, 0.4f, 0.42f));

        // ---------- Свет внутри, чтобы под крышей не было темно ----------
        GameObject lightObj = new GameObject("Свет");
        lightObj.transform.SetParent(house.transform, false);
        lightObj.transform.localPosition = new Vector3(0, 2.3f, 5f);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 9f;
        light.intensity = 1.2f;
        light.color = new Color(1f, 0.93f, 0.8f);
        light.shadows = LightShadows.None;

        // ---------- Зона телепорта внутри дома ----------
        TeleportZone(house, "Телепорт_дом", new Vector3(0, 0.03f, 5f), Quaternion.Euler(90, 0, 0), new Vector2(5.6f, 5.6f));

        // ---------- Дверь ----------
        GameObject hinge = new GameObject("Дверь_петля");
        hinge.transform.SetParent(house.transform, false);
        hinge.transform.localPosition = new Vector3(-0.5f, 0, 1.9f);
        GameObject panel = Box(hinge, "Дверь_полотно", Vector3.zero, new Vector3(1f, 2f, 0.08f), new Color(0.45f, 0.28f, 0.15f));
        panel.transform.localPosition = new Vector3(0.5f, 1f, 0);
        hinge.AddComponent<Interactable>();
        hinge.AddComponent<DoorController>();

        // ---------- Мебель ----------
        Box(house, "Стол", new Vector3(-2f, 0.4f, 6.8f), new Vector3(1.4f, 0.8f, 0.8f), new Color(0.55f, 0.35f, 0.2f));
        Box(house, "Стул", new Vector3(-2f, 0.25f, 5.8f), new Vector3(0.5f, 0.5f, 0.5f), new Color(0.5f, 0.3f, 0.18f));
        Box(house, "Шкаф", new Vector3(2.5f, 1f, 7.4f), new Vector3(0.8f, 2f, 1.2f), new Color(0.4f, 0.25f, 0.15f));
        Box(house, "Кровать", new Vector3(-2.2f, 0.25f, 3.3f), new Vector3(1f, 0.5f, 2f), new Color(0.7f, 0.7f, 0.85f));
        NoCollider(Box(house, "Ковёр", new Vector3(0, 0.01f, 5f), new Vector3(2.5f, 0.02f, 1.8f), new Color(0.7f, 0.2f, 0.2f)));
        GameObject picture = GameObject.CreatePrimitive(PrimitiveType.Quad);
        picture.name = "Картина";
        picture.transform.SetParent(house.transform, false);
        picture.transform.localPosition = new Vector3(0, 1.5f, 7.98f);
        picture.transform.localScale = new Vector3(1.2f, 0.8f, 1f);
        picture.GetComponent<Renderer>().sharedMaterial = GetMat(new Color(0.2f, 0.4f, 0.7f));

        // ---------- NPC: копия вашего NPC (с моделью и всеми компонентами) ----------
        GameObject npc = Object.Instantiate(origNpc);
        npc.name = "NPC (Дом " + idx + ")";
        npc.transform.SetParent(house.transform, false);
        npc.transform.localPosition = new Vector3(0.6f, 0, 5.5f);
        npc.transform.localRotation = Quaternion.identity;

        // Точка купюры у копии: ищем тот же объект по месту в иерархии
        Transform moneyPoint = null;
        Transform[] orig = origNpc.GetComponentsInChildren<Transform>(true);
        Transform[] copy = npc.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < orig.Length && i < copy.Length; i++)
        {
            if (orig[i] == origCtrl.moneyPoint) { moneyPoint = copy[i]; break; }
        }
        if (moneyPoint == null)
        {
            GameObject mp = new GameObject("Точка_купюры");
            mp.transform.SetParent(npc.transform, false);
            mp.transform.localPosition = origNpc.transform.InverseTransformPoint(origCtrl.moneyPoint.position);
            moneyPoint = mp.transform;
        }

        // ---------- Своя купюра для этого NPC ----------
        GameObject money = Object.Instantiate(origCtrl.money.gameObject);
        money.name = "Купюра (Дом " + idx + ")";
        money.SetActive(true);
        money.transform.SetParent(house.transform, false);
        money.transform.position = moneyPoint.position;
        money.transform.rotation = moneyPoint.rotation;
        GrabbableMoney grabbable = money.GetComponent<GrabbableMoney>();

        NPCController ctrl = npc.GetComponent<NPCController>();
        ctrl.money = grabbable;
        ctrl.moneyPoint = moneyPoint;

        // ---------- Надпись задачи этого дома ----------
        GameObject sign = new GameObject("Надпись");
        sign.transform.SetParent(house.transform, false);
        sign.transform.localPosition = new Vector3(0, 4.4f, 2.5f);
        TextMesh tm = sign.AddComponent<TextMesh>();
        tm.text = "Возьмите оплату за ЖКХ";
        tm.fontSize = 100;
        tm.characterSize = 0.04f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;

        GameObject tmObj = new GameObject("TaskManager");
        tmObj.transform.SetParent(house.transform, false);
        TaskManager task = tmObj.AddComponent<TaskManager>();
        task.money = grabbable;
        task.taskText = tm;
    }

    // Невидимая плоскость, на которую можно телепортироваться
    static void TeleportZone(GameObject parent, string name, Vector3 localPos, Quaternion localRot, Vector2 size)
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = name;
        q.transform.SetParent(parent.transform, false);
        q.transform.localPosition = localPos;
        // Если нужна плоскость вдоль земли
        q.transform.localRotation = localRot == Quaternion.identity ? Quaternion.Euler(90, 0, 0) : localRot;
        q.transform.localScale = new Vector3(size.x, size.y, 1);
        q.AddComponent<TeleportArea>();
    }

    static void NoCollider(GameObject go)
    {
        Collider c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
    }

    // Куб с цветом; координаты локальные (относительно дома)
    static GameObject Box(GameObject parent, string name, Vector3 localPos, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = GetMat(color);
        return go;
    }

    // Один материал на каждый цвет (чтобы не плодить сотни файлов)
    static Material GetMat(Color c)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        string path = "Assets/Materials/Zhkh2_" + ColorUtility.ToHtmlStringRGB(c) + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        Shader shader = GraphicsSettings.currentRenderPipeline != null
            ? Shader.Find("Universal Render Pipeline/Lit")
            : Shader.Find("Standard");
        m = new Material(shader);
        m.color = c;
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
