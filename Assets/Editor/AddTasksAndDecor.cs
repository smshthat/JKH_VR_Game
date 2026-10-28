using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Valve.VR.InteractionSystem;

// Меню Unity: ЖКХ -> Добавить задачи и декор
// Добавляет доску заданий и задачи по теме ЖКХ (лампочка, мусор, вентиль, почтовый ящик)
// и декор: деревья, кусты, цветы, фонари, скамейки, тротуары, разметку, машины и забор.
// Старые объекты и скрипты не меняются.
public static class AddTasksAndDecor
{
    static readonly Color brown = new Color(0.4f, 0.27f, 0.15f);
    static readonly Color[] leafColors =
    {
        new Color(0.2f, 0.5f, 0.2f), new Color(0.28f, 0.58f, 0.25f), new Color(0.18f, 0.42f, 0.22f)
    };

    [MenuItem("ЖКХ/Добавить задачи и декор")]
    public static void Add()
    {
        if (GameObject.Find("ЖКХ_Задачи") != null || GameObject.Find("ЖКХ_Декор") != null)
        {
            Debug.LogWarning("ЖКХ: задачи и декор уже добавлены. Удалите объекты ЖКХ_Задачи и ЖКХ_Декор, если хотите создать заново.");
            return;
        }

        Random.InitState(7);   // чтобы деревья каждый раз вставали одинаково
        BuildTasks();
        BuildDecor();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("ЖКХ: задачи и декор добавлены. Доска заданий стоит слева от стартовой точки.");
    }

    // =====================================================================
    //                             ЗАДАЧИ
    // =====================================================================
    static void BuildTasks()
    {
        GameObject root = new GameObject("ЖКХ_Задачи");
        Undo.RegisterCreatedObjectUndo(root, "ЖКХ задачи");

        // ---------- Задача 1: заменить лампочку у подъезда ----------
        GameObject fixture = Prim(PrimitiveType.Cube, root, "Лампа_основание", new Vector3(-0.9f, 1.7f, 1.74f), new Vector3(0.14f, 0.14f, 0.08f), new Color(0.35f, 0.35f, 0.38f));
        Prim(PrimitiveType.Sphere, root, "Лампа_колба_выкл", new Vector3(-0.9f, 1.7f, 1.68f), new Vector3(0.12f, 0.12f, 0.12f), new Color(0.3f, 0.3f, 0.32f));

        GameObject glow = new GameObject("Лампа_свет");
        glow.transform.SetParent(root.transform, false);
        glow.transform.position = new Vector3(-0.9f, 1.7f, 1.65f);
        GameObject glowBall = Prim(PrimitiveType.Sphere, glow, "Свечение", glow.transform.position, new Vector3(0.16f, 0.16f, 0.16f), new Color(1f, 0.92f, 0.55f), false, true);
        Light lampLight = glow.AddComponent<Light>();
        lampLight.type = LightType.Point;
        lampLight.range = 6f;
        lampLight.intensity = 2f;
        lampLight.color = new Color(1f, 0.9f, 0.65f);
        glow.SetActive(false);

        GameObject toolbox = Prim(PrimitiveType.Cube, root, "Ящик_с_инструментами", new Vector3(-2f, 0.15f, 0.6f), new Vector3(0.5f, 0.3f, 0.3f), new Color(0.75f, 0.25f, 0.15f), true);

        GameObject bulb = Prim(PrimitiveType.Sphere, root, "Лампочка", new Vector3(-1.85f, 0.34f, 0.6f), new Vector3(0.08f, 0.08f, 0.08f), new Color(1f, 0.95f, 0.75f), true);
        MakeGrabbable(bulb, 0.1f);

        GameObject envelope = Prim(PrimitiveType.Cube, root, "Квитанция_для_ящика", new Vector3(-2.15f, 0.31f, 0.6f), new Vector3(0.2f, 0.012f, 0.13f), new Color(0.95f, 0.92f, 0.8f), true);
        MakeGrabbable(envelope, 0.05f);

        TaskZone bulbZone = fixture.AddComponent<TaskZone>();
        bulbZone.radius = 0.3f;
        bulbZone.accepted = new Transform[] { bulb.transform };
        bulbZone.enableOnDone = glow;

        // ---------- Задача 2: выбросить мусор в бак ----------
        GameObject bag = Prim(PrimitiveType.Sphere, root, "Мешок_с_мусором", new Vector3(2.4f, 0.2f, 0.95f), new Vector3(0.35f, 0.4f, 0.35f), new Color(0.1f, 0.18f, 0.1f), true);
        MakeGrabbable(bag, 1f);

        Prim(PrimitiveType.Cylinder, root, "Мусорный_бак", new Vector3(4.2f, 0.4f, 0.95f), new Vector3(0.5f, 0.4f, 0.5f), new Color(0.25f, 0.4f, 0.3f), true);
        GameObject binZoneObj = new GameObject("Зона_бака");
        binZoneObj.transform.SetParent(root.transform, false);
        binZoneObj.transform.position = new Vector3(4.2f, 0.95f, 0.95f);
        TaskZone trashZone = binZoneObj.AddComponent<TaskZone>();
        trashZone.radius = 0.4f;
        trashZone.accepted = new Transform[] { bag.transform };

        // ---------- Задача 3: перекрыть вентиль ----------
        Color pipeColor = new Color(0.5f, 0.5f, 0.55f);
        Prim(PrimitiveType.Cylinder, root, "Труба", new Vector3(-4.8f, 0.6f, 0.9f), new Vector3(0.1f, 0.6f, 0.1f), pipeColor);
        GameObject stub = Prim(PrimitiveType.Cylinder, root, "Труба_отвод", new Vector3(-4.8f, 0.95f, 0.8f), new Vector3(0.06f, 0.1f, 0.06f), pipeColor);
        stub.transform.rotation = Quaternion.Euler(90f, 0, 0);

        GameObject valveRoot = new GameObject("Вентиль");
        valveRoot.transform.SetParent(root.transform, false);
        valveRoot.transform.position = new Vector3(-4.8f, 0.95f, 0.7f);
        GameObject wheel = new GameObject("Колесо");
        wheel.transform.SetParent(valveRoot.transform, false);
        wheel.transform.localPosition = Vector3.zero;
        wheel.transform.localRotation = Quaternion.Euler(90f, 0, 0);     // ось колеса смотрит вдоль Z
        Color red = new Color(0.8f, 0.15f, 0.12f);
        GameObject disc = Prim(PrimitiveType.Cylinder, wheel, "Диск", valveRoot.transform.position, new Vector3(0.3f, 0.015f, 0.3f), red, true);
        disc.transform.SetParent(wheel.transform, true);
        GameObject bar = Prim(PrimitiveType.Cube, wheel, "Ручка", valveRoot.transform.position, new Vector3(0.3f, 0.025f, 0.04f), new Color(0.6f, 0.1f, 0.08f));
        bar.transform.SetParent(wheel.transform, true);
        bar.transform.localPosition = new Vector3(0, 0.02f, 0);

        // Утечка воды (исчезнет, когда вентиль закрыт)
        GameObject leak = new GameObject("Утечка");
        leak.transform.SetParent(root.transform, false);
        Color water = new Color(0.2f, 0.45f, 0.9f);
        Prim(PrimitiveType.Cube, leak, "Струя", new Vector3(-4.62f, 0.45f, 0.9f), new Vector3(0.04f, 0.7f, 0.04f), water);
        Prim(PrimitiveType.Cylinder, leak, "Лужа", new Vector3(-4.62f, 0.015f, 0.9f), new Vector3(0.8f, 0.01f, 0.8f), water);

        valveRoot.AddComponent<Interactable>();
        ValveTask valve = valveRoot.AddComponent<ValveTask>();
        valve.wheel = wheel.transform;
        valve.leak = leak;

        // ---------- Задача 4: отнести квитанцию в почтовый ящик ----------
        Prim(PrimitiveType.Cylinder, root, "Почта_стойка", new Vector3(3.4f, 0.5f, -0.6f), new Vector3(0.08f, 0.5f, 0.08f), new Color(0.35f, 0.35f, 0.38f));
        Prim(PrimitiveType.Cube, root, "Почтовый_ящик", new Vector3(3.4f, 1.05f, -0.6f), new Vector3(0.3f, 0.22f, 0.4f), new Color(0.75f, 0.15f, 0.15f), true);
        GameObject mailZoneObj = new GameObject("Зона_почты");
        mailZoneObj.transform.SetParent(root.transform, false);
        mailZoneObj.transform.position = new Vector3(3.4f, 1.1f, -0.6f);
        TaskZone mailZone = mailZoneObj.AddComponent<TaskZone>();
        mailZone.radius = 0.3f;
        mailZone.accepted = new Transform[] { envelope.transform };

        // ---------- Доска заданий ----------
        GameObject board = new GameObject("Доска_заданий");
        board.transform.SetParent(root.transform, false);
        board.transform.position = new Vector3(-3.3f, 1.9f, -0.2f);

        GameObject textObj = new GameObject("Текст");
        textObj.transform.SetParent(board.transform, false);
        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = "ЗАДАНИЯ НА СМЕНУ";
        tm.fontSize = 100;
        tm.characterSize = 0.015f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Left;
        tm.color = Color.white;

        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "Фон";
        Object.DestroyImmediate(bg.GetComponent<Collider>());
        bg.transform.SetParent(board.transform, false);
        bg.transform.localPosition = new Vector3(0, 0, 0.03f);
        bg.transform.localScale = new Vector3(3.8f, 1.6f, 1f);
        bg.GetComponent<Renderer>().sharedMaterial = GetMat(new Color(0.08f, 0.1f, 0.14f), false);

        TaskBoard tb = board.AddComponent<TaskBoard>();
        tb.text = tm;
        tb.bulbZone = bulbZone;
        tb.trashZone = trashZone;
        tb.mailZone = mailZone;
        tb.valve = valve;
    }

    // Предмет, который берётся рукой и падает с физикой
    static void MakeGrabbable(GameObject go, float mass)
    {
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = mass;
        go.AddComponent<Interactable>();
        go.AddComponent<Throwable>();
    }

    // =====================================================================
    //                              ДЕКОР
    // =====================================================================
    static void BuildDecor()
    {
        GameObject root = new GameObject("ЖКХ_Декор");
        Undo.RegisterCreatedObjectUndo(root, "ЖКХ декор");

        // ---------- Тротуары и разметка дороги ----------
        GameObject roads = Group(root, "Дорога_и_тротуары");
        Color walk = new Color(0.7f, 0.7f, 0.72f);
        Prim(PrimitiveType.Cube, roads, "Тротуар_северный", new Vector3(0, 0.015f, -0.4f), new Vector3(56f, 0.03f, 3.0f), walk);
        Prim(PrimitiveType.Cube, roads, "Тротуар_южный", new Vector3(0, 0.015f, -6.5f), new Vector3(56f, 0.03f, 0.95f), walk);
        for (float x = -26f; x <= 26f; x += 4f)
            Prim(PrimitiveType.Cube, roads, "Разметка", new Vector3(x, 0.022f, -4f), new Vector3(1.6f, 0.01f, 0.12f), Color.white);

        // ---------- Деревья ----------
        GameObject trees = Group(root, "Деревья");
        for (float x = -24f; x <= 24f; x += 8f)
        {
            Tree(trees, new Vector3(x + Random.Range(-1.5f, 1.5f), 0, 11.5f + Random.Range(-0.5f, 0.5f)));
            Tree(trees, new Vector3(x + 4f + Random.Range(-1.5f, 1.5f), 0, 15.5f + Random.Range(-0.5f, 0.5f)));
            Tree(trees, new Vector3(x + Random.Range(-1.5f, 1.5f), 0, -15.5f + Random.Range(-0.5f, 0.5f)));
            Tree(trees, new Vector3(x + 4f + Random.Range(-1.5f, 1.5f), 0, -19.5f + Random.Range(-0.5f, 0.5f)));
        }
        for (float z = -10f; z <= 10f; z += 10f)
        {
            Tree(trees, new Vector3(-26f, 0, z));
            Tree(trees, new Vector3(26f, 0, z));
        }

        // ---------- Кусты и цветы у домов ----------
        GameObject plants = Group(root, "Кусты_и_цветы");
        float[] northHouses = { -16f, -8f, 0f, 8f, 16f };
        float[] southHouses = { -8f, 0f, 8f };
        foreach (float x in northHouses)
        {
            Bush(plants, new Vector3(x - 3.0f, 0, 1.45f));
            Bush(plants, new Vector3(x + 3.0f, 0, 1.45f));
        }
        foreach (float x in southHouses)
        {
            Bush(plants, new Vector3(x - 3.0f, 0, -6.5f));
            Bush(plants, new Vector3(x + 3.0f, 0, -6.5f));
        }

        // ---------- Фонари вдоль улицы ----------
        GameObject lamps = Group(root, "Фонари");
        for (float x = -20f; x <= 20f; x += 8f)
        {
            Prim(PrimitiveType.Cylinder, lamps, "Столб", new Vector3(x, 1.6f, -1.7f), new Vector3(0.1f, 1.6f, 0.1f), new Color(0.2f, 0.2f, 0.22f));
            Prim(PrimitiveType.Sphere, lamps, "Фонарь", new Vector3(x, 3.3f, -1.7f), new Vector3(0.4f, 0.4f, 0.4f), new Color(1f, 0.93f, 0.6f), false, true);
        }

        // ---------- Скамейки ----------
        GameObject benches = Group(root, "Скамейки");
        Bench(benches, new Vector3(-7f, 0, -1.3f));
        Bench(benches, new Vector3(11f, 0, -1.3f));

        // ---------- Машины ----------
        GameObject cars = Group(root, "Машины");
        Car(cars, new Vector3(-12f, 0, -5.2f), new Color(0.75f, 0.15f, 0.15f));
        Car(cars, new Vector3(14f, 0, -5.2f), new Color(0.2f, 0.35f, 0.7f));

        // ---------- Забор по краю карты ----------
        GameObject fence = Group(root, "Забор");
        Color wood = new Color(0.8f, 0.72f, 0.55f);
        float e = 28.5f;
        for (float t = -e; t <= e + 0.01f; t += 3f)
        {
            Prim(PrimitiveType.Cube, fence, "Столб", new Vector3(t, 0.6f, e), new Vector3(0.12f, 1.2f, 0.12f), wood);
            Prim(PrimitiveType.Cube, fence, "Столб", new Vector3(t, 0.6f, -e), new Vector3(0.12f, 1.2f, 0.12f), wood);
            Prim(PrimitiveType.Cube, fence, "Столб", new Vector3(e, 0.6f, t), new Vector3(0.12f, 1.2f, 0.12f), wood);
            Prim(PrimitiveType.Cube, fence, "Столб", new Vector3(-e, 0.6f, t), new Vector3(0.12f, 1.2f, 0.12f), wood);
        }
        foreach (float y in new float[] { 0.45f, 0.9f })
        {
            Prim(PrimitiveType.Cube, fence, "Перекладина", new Vector3(0, y, e), new Vector3(2f * e, 0.08f, 0.05f), wood);
            Prim(PrimitiveType.Cube, fence, "Перекладина", new Vector3(0, y, -e), new Vector3(2f * e, 0.08f, 0.05f), wood);
            Prim(PrimitiveType.Cube, fence, "Перекладина", new Vector3(e, y, 0), new Vector3(0.05f, 0.08f, 2f * e), wood);
            Prim(PrimitiveType.Cube, fence, "Перекладина", new Vector3(-e, y, 0), new Vector3(0.05f, 0.08f, 2f * e), wood);
        }
    }

    static void Tree(GameObject parent, Vector3 p)
    {
        float h = Random.Range(2.2f, 3.4f);
        Prim(PrimitiveType.Cylinder, parent, "Ствол", p + Vector3.up * (h / 2f), new Vector3(0.35f, h / 2f, 0.35f), brown);

        Color leaf = leafColors[Random.Range(0, leafColors.Length)];
        float r = Random.Range(2.2f, 3.0f);
        Prim(PrimitiveType.Sphere, parent, "Крона", p + Vector3.up * (h + r * 0.35f), new Vector3(r, r * 0.9f, r), leaf);
        Prim(PrimitiveType.Sphere, parent, "Крона_2", p + new Vector3(Random.Range(-0.8f, 0.8f), h + r * 0.1f, Random.Range(-0.8f, 0.8f)),
             new Vector3(r * 0.7f, r * 0.6f, r * 0.7f), leaf);
    }

    static void Bush(GameObject parent, Vector3 p)
    {
        float r = Random.Range(0.7f, 0.95f);
        Prim(PrimitiveType.Sphere, parent, "Куст", p + Vector3.up * (r * 0.4f), new Vector3(r, r * 0.8f, r), leafColors[Random.Range(0, leafColors.Length)]);

        // Несколько цветов рядом
        Color[] flowers =
        {
            new Color(0.9f, 0.2f, 0.25f), new Color(1f, 0.85f, 0.2f), new Color(0.95f, 0.95f, 0.95f), new Color(0.9f, 0.4f, 0.7f)
        };
        for (int i = 0; i < 4; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.7f, 0.7f), 0.08f, Random.Range(-0.7f, 0.7f));
            Prim(PrimitiveType.Sphere, parent, "Цветок", p + offset, new Vector3(0.14f, 0.14f, 0.14f), flowers[i]);
        }
    }

    static void Bench(GameObject parent, Vector3 p)
    {
        Color wood = new Color(0.55f, 0.37f, 0.2f);
        Color iron = new Color(0.2f, 0.2f, 0.22f);
        Prim(PrimitiveType.Cube, parent, "Сиденье", p + new Vector3(0, 0.45f, 0), new Vector3(1.4f, 0.08f, 0.45f), wood);
        Prim(PrimitiveType.Cube, parent, "Спинка", p + new Vector3(0, 0.75f, 0.2f), new Vector3(1.4f, 0.4f, 0.06f), wood);
        Prim(PrimitiveType.Cube, parent, "Ножка", p + new Vector3(-0.6f, 0.22f, 0), new Vector3(0.08f, 0.45f, 0.4f), iron);
        Prim(PrimitiveType.Cube, parent, "Ножка", p + new Vector3(0.6f, 0.22f, 0), new Vector3(0.08f, 0.45f, 0.4f), iron);
    }

    static void Car(GameObject parent, Vector3 p, Color body)
    {
        Prim(PrimitiveType.Cube, parent, "Кузов", p + new Vector3(0, 0.6f, 0), new Vector3(4f, 0.6f, 1.8f), body);
        Prim(PrimitiveType.Cube, parent, "Кабина", p + new Vector3(-0.2f, 1.15f, 0), new Vector3(2f, 0.5f, 1.6f), new Color(0.6f, 0.75f, 0.85f));
        Color tire = new Color(0.08f, 0.08f, 0.08f);
        float[] xs = { -1.3f, 1.3f };
        float[] zs = { -0.85f, 0.85f };
        foreach (float x in xs)
            foreach (float z in zs)
            {
                GameObject w = Prim(PrimitiveType.Cylinder, parent, "Колесо", p + new Vector3(x, 0.35f, z), new Vector3(0.7f, 0.12f, 0.7f), tire);
                w.transform.rotation = Quaternion.Euler(90f, 0, 0);
            }
    }

    // =====================================================================
    //                         Вспомогательное
    // =====================================================================
    static GameObject Group(GameObject parent, string name)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent.transform, false);
        return g;
    }

    // Фигура с цветом. Позиция задаётся в мировых координатах (родители стоят в нуле).
    static GameObject Prim(PrimitiveType type, GameObject parent, string name, Vector3 pos, Vector3 scale, Color color, bool keepCollider = false, bool unlit = false)
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
        string path = "Assets/Materials/Zhkh4_" + (unlit ? "unlit_" : "") + ColorUtility.ToHtmlStringRGB(c) + ".mat";
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
