using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Valve.VR.InteractionSystem;

// Меню Unity: ЖКХ -> Главный дом: второй этаж и интерьер
// Достраивает главный дом (тот, что стоит в центре): лестница, второй этаж с окнами, крыша,
// интерьер на обоих этажах, свет и зоны телепорта по всему дому, включая лестницу.
// Старые объекты не удаляются. Единственное, что сдвигается: надпись над домом (выше крыши).
public static class MainHouseSecondFloor
{
    const float F2 = 2.7f;      // высота пола второго этажа
    const float TOP = 5.3f;     // верх стен второго этажа

    static readonly Color wallColor = new Color(0.85f, 0.8f, 0.7f);
    static readonly Color woodFloor = new Color(0.62f, 0.47f, 0.32f);

    [MenuItem("ЖКХ/Главный дом: второй этаж и интерьер")]
    public static void Build()
    {
        if (GameObject.Find("ЖКХ_Дом_этаж2") != null)
        {
            Debug.LogWarning("ЖКХ: второй этаж уже построен. Удалите объект ЖКХ_Дом_этаж2, если хотите создать заново.");
            return;
        }
        if (GameObject.Find("Стена_задняя") == null)
            Debug.LogWarning("ЖКХ: не найдены стены главного дома. Строю по тем же координатам, что и раньше: проверьте результат в сцене.");

        GameObject root = new GameObject("ЖКХ_Дом_этаж2");
        Undo.RegisterCreatedObjectUndo(root, "ЖКХ второй этаж");

        BuildFloorSlab(root);
        BuildStairs(root);
        BuildUpperWalls(root);
        BuildRoof(root);
        BuildGroundInterior(root);
        BuildUpperInterior(root);
        BuildLights(root);
        BuildTeleports(root);
        MoveSign();

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("ЖКХ: главный дом стал двухэтажным. Лестница у правой стены, телепорт работает на обоих этажах и на ступенях.");
    }

    // ---------- Перекрытие между этажами (с проёмом для лестницы) ----------
    static void BuildFloorSlab(GameObject root)
    {
        float y = 2.65f;
        Box(root, "Перекрытие_A", new Vector3(-0.6f, y, 5f), new Vector3(5.2f, 0.1f, 6.4f), woodFloor);
        Box(root, "Перекрытие_B", new Vector3(2.6f, y, 7.25f), new Vector3(1.2f, 0.1f, 1.9f), woodFloor);
        Box(root, "Перекрытие_C", new Vector3(3.05f, y, 4.35f), new Vector3(0.3f, 0.1f, 3.9f), woodFloor);
        Box(root, "Перекрытие_D", new Vector3(2.6f, y, 2.1f), new Vector3(1.2f, 0.1f, 0.6f), woodFloor);
    }

    // ---------- Лестница вдоль правой стены ----------
    static void BuildStairs(GameObject root)
    {
        int n = 14;
        float rise = F2 / n;
        float depth = 0.27f;
        float z0 = 2.5f;
        Color stair = new Color(0.55f, 0.4f, 0.28f);
        for (int i = 0; i < n; i++)
        {
            float h = rise * (i + 1);
            Box(root, "Ступень_" + i, new Vector3(2.45f, h / 2f, z0 + depth * (i + 0.5f)), new Vector3(0.9f, h, depth), stair);
        }
        // Перила вдоль проёма на втором этаже
        Box(root, "Перила", new Vector3(1.98f, F2 + 0.45f, 4.35f), new Vector3(0.05f, 0.9f, 3.9f), new Color(0.35f, 0.25f, 0.18f));
    }

    // ---------- Стены второго этажа (спереди и сзади с окнами) ----------
    static void BuildUpperWalls(GameObject root)
    {
        WindowWall(root, "перед", 1.9f);
        WindowWall(root, "зад", 8.1f);
        Box(root, "Эт2_стена_левая", new Vector3(-3.1f, 4.0f, 5f), new Vector3(0.2f, 2.6f, 6.2f), wallColor);
        Box(root, "Эт2_стена_правая", new Vector3(3.1f, 4.0f, 5f), new Vector3(0.2f, 2.6f, 6.2f), wallColor);
        Box(root, "Потолок", new Vector3(0, TOP, 5f), new Vector3(6.4f, 0.1f, 6.4f), new Color(0.9f, 0.88f, 0.82f));
    }

    static void WindowWall(GameObject root, string tag, float z)
    {
        float t = 0.2f;
        Box(root, "Эт2_" + tag + "_простенок_л", new Vector3(-2.7f, 4.0f, z), new Vector3(0.6f, 2.6f, t), wallColor);
        Box(root, "Эт2_" + tag + "_простенок_ц", new Vector3(0, 4.0f, z), new Vector3(2.4f, 2.6f, t), wallColor);
        Box(root, "Эт2_" + tag + "_простенок_п", new Vector3(2.7f, 4.0f, z), new Vector3(0.6f, 2.6f, t), wallColor);
        foreach (float x in new float[] { -1.8f, 1.8f })
        {
            Box(root, "Эт2_" + tag + "_под_окном", new Vector3(x, 3.15f, z), new Vector3(1.2f, 0.9f, t), wallColor);
            Box(root, "Эт2_" + tag + "_над_окном", new Vector3(x, 5.0f, z), new Vector3(1.2f, 0.6f, t), wallColor);
            Box(root, "Окно_стекло", new Vector3(x, 4.15f, z), new Vector3(1.2f, 1.1f, 0.04f), new Color(0.55f, 0.75f, 0.95f, 0.3f), false, true);
        }
    }

    // ---------- Двускатная крыша ----------
    static void BuildRoof(GameObject root)
    {
        Color roof = new Color(0.55f, 0.2f, 0.15f);
        float halfWidth = 3.5f;
        float rise = 1.3f;
        float slopeLen = Mathf.Sqrt(halfWidth * halfWidth + rise * rise);
        float angle = Mathf.Atan2(rise, halfWidth) * Mathf.Rad2Deg;

        GameObject l = Box(root, "Крыша_левая", new Vector3(-halfWidth / 2f, TOP + rise / 2f, 5f), new Vector3(slopeLen, 0.12f, 7f), roof);
        l.transform.rotation = Quaternion.Euler(0, 0, angle);
        GameObject r = Box(root, "Крыша_правая", new Vector3(halfWidth / 2f, TOP + rise / 2f, 5f), new Vector3(slopeLen, 0.12f, 7f), roof);
        r.transform.rotation = Quaternion.Euler(0, 0, -angle);

        float[] stepWidth = { 5.6f, 3.4f, 1.1f };
        float stepH = rise / 3f;
        for (int i = 0; i < 3; i++)
        {
            float y = TOP + stepH * (i + 0.5f);
            Box(root, "Фронтон_перед_" + i, new Vector3(0, y, 1.9f), new Vector3(stepWidth[i], stepH, 0.2f), wallColor);
            Box(root, "Фронтон_зад_" + i, new Vector3(0, y, 8.1f), new Vector3(stepWidth[i], stepH, 0.2f), wallColor);
        }
        Box(root, "Труба", new Vector3(2f, TOP + 1.6f, 6.5f), new Vector3(0.5f, 1.2f, 0.5f), new Color(0.4f, 0.4f, 0.42f));
    }

    // ---------- Интерьер первого этажа (старая мебель остаётся) ----------
    static void BuildGroundInterior(GameObject root)
    {
        GameObject g = Group(root, "Интерьер_этаж1");

        // Диван у задней стены и журнальный столик
        Color sofa = new Color(0.25f, 0.4f, 0.6f);
        Box(g, "Диван", new Vector3(0, 0.25f, 7.55f), new Vector3(1.8f, 0.5f, 0.8f), sofa);
        Box(g, "Диван_спинка", new Vector3(0, 0.65f, 7.85f), new Vector3(1.8f, 0.5f, 0.15f), sofa);
        Box(g, "Журнальный_столик", new Vector3(0, 0.2f, 6.5f), new Vector3(0.9f, 0.4f, 0.5f), new Color(0.5f, 0.33f, 0.2f));

        // Тумба с телевизором у левой стены
        Box(g, "ТВ_тумба", new Vector3(-2.8f, 0.25f, 5f), new Vector3(0.4f, 0.5f, 1.2f), new Color(0.35f, 0.25f, 0.18f));
        Box(g, "Телевизор", new Vector3(-2.85f, 0.85f, 5f), new Vector3(0.08f, 0.6f, 1.0f), new Color(0.05f, 0.05f, 0.07f), false);

        // Кухонный блок справа от двери
        Box(g, "Кухня_тумба", new Vector3(1.3f, 0.45f, 2.4f), new Vector3(1.4f, 0.9f, 0.6f), new Color(0.9f, 0.9f, 0.88f));
        Box(g, "Кухня_столешница", new Vector3(1.3f, 0.92f, 2.4f), new Vector3(1.4f, 0.04f, 0.62f), new Color(0.2f, 0.2f, 0.22f), false);
        GameObject plate1 = Cyl(g, "Конфорка", new Vector3(0.95f, 0.945f, 2.4f), new Vector3(0.2f, 0.01f, 0.2f), Color.black);
        GameObject plate2 = Cyl(g, "Конфорка", new Vector3(1.3f, 0.945f, 2.4f), new Vector3(0.2f, 0.01f, 0.2f), Color.black);
        Box(g, "Раковина", new Vector3(1.7f, 0.935f, 2.4f), new Vector3(0.4f, 0.02f, 0.35f), new Color(0.7f, 0.75f, 0.8f), false);
        Box(g, "Кухня_шкаф_навесной", new Vector3(1.3f, 1.75f, 2.18f), new Vector3(1.4f, 0.6f, 0.35f), new Color(0.9f, 0.9f, 0.88f));

        // Растение в углу
        Cyl(g, "Горшок", new Vector3(-2.6f, 0.2f, 7.7f), new Vector3(0.3f, 0.2f, 0.3f), new Color(0.6f, 0.3f, 0.2f));
        Sphere(g, "Растение", new Vector3(-2.6f, 0.75f, 7.7f), new Vector3(0.55f, 0.55f, 0.55f), new Color(0.2f, 0.5f, 0.2f));
    }

    // ---------- Интерьер второго этажа ----------
    static void BuildUpperInterior(GameObject root)
    {
        GameObject g = Group(root, "Интерьер_этаж2");
        Color partition = new Color(0.88f, 0.84f, 0.76f);

        // Перегородки: ванная (слева сзади) и проход между комнатами
        Box(g, "Перегородка_спальня", new Vector3(-1.9f, 4.0f, 5.0f), new Vector3(2.2f, 2.6f, 0.15f), partition);
        Box(g, "Перегородка_холл", new Vector3(1.6f, 4.0f, 5.0f), new Vector3(0.8f, 2.6f, 0.15f), partition);
        Box(g, "Перегородка_ванная", new Vector3(-0.8f, 4.0f, 7.0f), new Vector3(0.12f, 2.6f, 2.0f), partition);

        // Спальня (впереди слева)
        Color wood = new Color(0.5f, 0.33f, 0.2f);
        Box(g, "Кровать", new Vector3(-1.9f, F2 + 0.175f, 3.5f), new Vector3(2.0f, 0.35f, 1.5f), wood);
        Box(g, "Матрас", new Vector3(-1.8f, F2 + 0.45f, 3.5f), new Vector3(1.9f, 0.2f, 1.4f), new Color(0.85f, 0.85f, 0.95f));
        Box(g, "Подушка", new Vector3(-2.65f, F2 + 0.6f, 3.1f), new Vector3(0.4f, 0.12f, 0.5f), Color.white, false);
        Box(g, "Подушка", new Vector3(-2.65f, F2 + 0.6f, 3.9f), new Vector3(0.4f, 0.12f, 0.5f), Color.white, false);
        Box(g, "Изголовье", new Vector3(-2.95f, F2 + 0.55f, 3.5f), new Vector3(0.1f, 1.1f, 1.6f), wood);
        Box(g, "Тумбочка", new Vector3(-2.7f, F2 + 0.25f, 2.35f), new Vector3(0.45f, 0.5f, 0.45f), wood);
        Sphere(g, "Ночник", new Vector3(-2.7f, F2 + 0.62f, 2.35f), new Vector3(0.18f, 0.18f, 0.18f), new Color(1f, 0.93f, 0.6f), false, true);
        Box(g, "Шкаф_спальня", new Vector3(-1.9f, F2 + 1.0f, 4.65f), new Vector3(1.6f, 2.0f, 0.5f), new Color(0.4f, 0.27f, 0.17f));
        Box(g, "Ковёр_спальня", new Vector3(0.3f, F2 + 0.011f, 3.9f), new Vector3(1.8f, 0.02f, 1.4f), new Color(0.2f, 0.3f, 0.6f), false);

        // Рабочий стол у окна
        Box(g, "Стол_рабочий", new Vector3(1.0f, F2 + 0.38f, 2.4f), new Vector3(1.4f, 0.76f, 0.6f), new Color(0.75f, 0.65f, 0.5f));
        Box(g, "Монитор", new Vector3(1.0f, F2 + 0.98f, 2.35f), new Vector3(0.5f, 0.32f, 0.04f), new Color(0.05f, 0.05f, 0.08f), false);
        Box(g, "Клавиатура", new Vector3(1.0f, F2 + 0.775f, 2.55f), new Vector3(0.4f, 0.02f, 0.15f), new Color(0.15f, 0.15f, 0.17f), false);
        Box(g, "Стул_сиденье", new Vector3(1.0f, F2 + 0.25f, 3.1f), new Vector3(0.45f, 0.06f, 0.45f), new Color(0.2f, 0.2f, 0.22f));
        Box(g, "Стул_спинка", new Vector3(1.0f, F2 + 0.55f, 3.33f), new Vector3(0.45f, 0.5f, 0.05f), new Color(0.2f, 0.2f, 0.22f));

        // Ванная (слева сзади)
        Color white = new Color(0.95f, 0.95f, 0.97f);
        Box(g, "Ванна", new Vector3(-2.55f, F2 + 0.25f, 6.9f), new Vector3(0.8f, 0.5f, 1.8f), white);
        Cyl(g, "Унитаз", new Vector3(-1.4f, F2 + 0.2f, 7.6f), new Vector3(0.32f, 0.2f, 0.32f), white);
        Box(g, "Бачок", new Vector3(-1.4f, F2 + 0.55f, 7.88f), new Vector3(0.4f, 0.4f, 0.2f), white);
        Box(g, "Раковина_тумба", new Vector3(-1.6f, F2 + 0.4f, 5.35f), new Vector3(0.6f, 0.8f, 0.4f), new Color(0.8f, 0.8f, 0.85f));
        Cyl(g, "Раковина_чаша", new Vector3(-1.6f, F2 + 0.83f, 5.35f), new Vector3(0.38f, 0.05f, 0.38f), white);
        GameObject mirror = GameObject.CreatePrimitive(PrimitiveType.Quad);
        mirror.name = "Зеркало";
        Object.DestroyImmediate(mirror.GetComponent<Collider>());
        mirror.transform.SetParent(g.transform, false);
        mirror.transform.position = new Vector3(-1.6f, F2 + 1.5f, 5.09f);
        mirror.transform.rotation = Quaternion.Euler(0, 180f, 0);
        mirror.transform.localScale = new Vector3(0.7f, 0.9f, 1f);
        mirror.GetComponent<Renderer>().sharedMaterial = GetMat(new Color(0.7f, 0.85f, 0.95f), false);

        // Холл (справа сзади): стеллаж, кресло, столик, ковёр, растение
        Box(g, "Стеллаж", new Vector3(0.4f, F2 + 1.0f, 7.8f), new Vector3(1.6f, 2.0f, 0.3f), new Color(0.45f, 0.3f, 0.18f));
        Color[] books = { new Color(0.7f, 0.15f, 0.15f), new Color(0.15f, 0.4f, 0.7f), new Color(0.2f, 0.55f, 0.25f), new Color(0.85f, 0.7f, 0.15f) };
        for (int i = 0; i < 8; i++)
            Box(g, "Книга", new Vector3(-0.2f + i * 0.17f, F2 + 1.3f, 7.62f), new Vector3(0.1f, 0.3f, 0.2f), books[i % books.Length], false);
        Box(g, "Кресло", new Vector3(-0.2f, F2 + 0.225f, 6.4f), new Vector3(0.7f, 0.45f, 0.7f), new Color(0.6f, 0.25f, 0.25f));
        Box(g, "Кресло_спинка", new Vector3(-0.2f, F2 + 0.6f, 6.1f), new Vector3(0.7f, 0.5f, 0.12f), new Color(0.6f, 0.25f, 0.25f));
        Box(g, "Столик_холл", new Vector3(1.0f, F2 + 0.15f, 6.5f), new Vector3(0.5f, 0.3f, 0.5f), new Color(0.5f, 0.33f, 0.2f));
        Box(g, "Ковёр_холл", new Vector3(0.5f, F2 + 0.011f, 6.4f), new Vector3(1.8f, 0.02f, 1.4f), new Color(0.25f, 0.5f, 0.35f), false);
        Cyl(g, "Горшок_холл", new Vector3(1.7f, F2 + 0.2f, 7.6f), new Vector3(0.3f, 0.2f, 0.3f), new Color(0.6f, 0.3f, 0.2f));
        Sphere(g, "Растение_холл", new Vector3(1.7f, F2 + 0.75f, 7.6f), new Vector3(0.55f, 0.55f, 0.55f), new Color(0.2f, 0.5f, 0.2f));

        // Картины на стенах
        Picture(g, new Vector3(-2.97f, F2 + 1.7f, 3.5f), Quaternion.Euler(0, -90f, 0), new Vector3(1.0f, 0.7f, 1f), new Color(0.8f, 0.5f, 0.2f));
        Picture(g, new Vector3(1.0f, F2 + 1.7f, 7.97f), Quaternion.identity, new Vector3(0.9f, 0.6f, 1f), new Color(0.2f, 0.45f, 0.7f));
    }

    static void Picture(GameObject parent, Vector3 pos, Quaternion rot, Vector3 scale, Color color)
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Картина";
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.SetParent(parent.transform, false);
        q.transform.position = pos;
        q.transform.rotation = rot;
        q.transform.localScale = scale;
        q.GetComponent<Renderer>().sharedMaterial = GetMat(color, false);
    }

    // ---------- Свет: под крышей и без окон на первом этаже было бы темно ----------
    static void BuildLights(GameObject root)
    {
        GameObject g = Group(root, "Свет");
        AddLight(g, "Свет_этаж1", new Vector3(0, 2.2f, 5f), 9f, 1.2f);
        AddLight(g, "Свет_спальня", new Vector3(-0.5f, 4.9f, 3.5f), 7f, 1.1f);
        AddLight(g, "Свет_холл", new Vector3(0.5f, 4.9f, 6.5f), 7f, 1.1f);
        AddLight(g, "Свет_ванная", new Vector3(-1.9f, 4.9f, 6.8f), 4f, 1.0f);
    }

    static void AddLight(GameObject parent, string name, Vector3 pos, float range, float intensity)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = range;
        l.intensity = intensity;
        l.color = new Color(1f, 0.93f, 0.8f);
        l.shadows = LightShadows.None;
    }

    // ---------- Телепорт по всему дому ----------
    static void BuildTeleports(GameObject root)
    {
        GameObject g = Group(root, "Телепорт_дом");

        // Второй этаж: комнаты и холл у верха лестницы
        TeleportZone(g, "Телепорт_этаж2_комнаты", new Vector3(-0.475f, F2 + 0.03f, 5f), new Vector2(4.85f, 5.8f));
        TeleportZone(g, "Телепорт_этаж2_верх_лестницы", new Vector3(2.5f, F2 + 0.03f, 7.15f), new Vector2(0.8f, 1.5f));

        // Каждая ступень лестницы
        int n = 14;
        float rise = F2 / n;
        float depth = 0.27f;
        float z0 = 2.5f;
        for (int i = 0; i < n; i++)
            TeleportZone(g, "Телепорт_ступень_" + i, new Vector3(2.45f, rise * (i + 1) + 0.03f, z0 + depth * (i + 0.5f)), new Vector2(0.8f, 0.24f));
    }

    static void TeleportZone(GameObject parent, string name, Vector3 pos, Vector2 size)
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = name;
        q.transform.SetParent(parent.transform, false);
        q.transform.position = pos;
        q.transform.rotation = Quaternion.Euler(90, 0, 0);
        q.transform.localScale = new Vector3(size.x, size.y, 1);
        q.AddComponent<TeleportArea>();
    }

    // ---------- Надпись над домом поднимаем выше крыши ----------
    static void MoveSign()
    {
        GameObject world = GameObject.Find("ЖКХ_Мир");
        if (world == null) return;
        Transform sign = world.transform.Find("Надпись");
        if (sign == null) return;
        Undo.RecordObject(sign, "Надпись выше крыши");
        sign.position = new Vector3(0, 7.8f, 2.2f);
    }

    // ---------- Вспомогательное ----------
    static GameObject Group(GameObject parent, string name)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent.transform, false);
        return g;
    }

    static GameObject Box(GameObject parent, string name, Vector3 pos, Vector3 scale, Color color, bool keepCollider = true, bool unlit = false)
    {
        return Prim(PrimitiveType.Cube, parent, name, pos, scale, color, keepCollider, unlit);
    }

    static GameObject Cyl(GameObject parent, string name, Vector3 pos, Vector3 scale, Color color)
    {
        return Prim(PrimitiveType.Cylinder, parent, name, pos, scale, color, false, false);
    }

    static GameObject Sphere(GameObject parent, string name, Vector3 pos, Vector3 scale, Color color, bool keepCollider = false, bool unlit = false)
    {
        return Prim(PrimitiveType.Sphere, parent, name, pos, scale, color, keepCollider, unlit);
    }

    static GameObject Prim(PrimitiveType type, GameObject parent, string name, Vector3 pos, Vector3 scale, Color color, bool keepCollider, bool unlit)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (!keepCollider)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
        go.GetComponent<Renderer>().sharedMaterial = GetMat(color, unlit);
        return go;
    }

    // Один материал на каждый цвет (чтобы не плодить сотни файлов)
    static Material GetMat(Color c, bool unlit)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        string path = "Assets/Materials/Zhkh5_" + (unlit ? "unlit_" : "") + ColorUtility.ToHtmlStringRGBA(c) + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        Shader shader;
        if (unlit) shader = Shader.Find("Sprites/Default");
        else shader = GraphicsSettings.currentRenderPipeline != null
            ? Shader.Find("Universal Render Pipeline/Lit")
            : Shader.Find("Standard");
        m = new Material(shader);
        m.color = c;
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
