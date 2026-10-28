using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Valve.VR.InteractionSystem;

// Меню Unity: ЖКХ -> Собрать сцену
// Создаёт улицу, дом, мебель, дверь, NPC, купюру, квитанцию и надпись из примитивов,
// вешает скрипты и ставит префабы Player и Teleporting из ассета SteamVR.
// ВАЖНО: сначала должен быть установлен ассет SteamVR.
public static class BuildZhkhScene
{
    const string PlayerPrefab = "Assets/SteamVR/InteractionSystem/Core/Prefabs/Player.prefab";
    const string TeleportingPrefab = "Assets/SteamVR/InteractionSystem/Teleport/Prefabs/Teleporting.prefab";

    [MenuItem("ЖКХ/Собрать сцену")]
    public static void Build()
    {
        // Старая камера не нужна: у Player из SteamVR есть своя
        GameObject oldCam = GameObject.FindWithTag("MainCamera");
        if (oldCam != null) Object.DestroyImmediate(oldCam);

        GameObject root = new GameObject("ЖКХ_Мир");

        // ---------- Улица ----------
        Box(root, "Земля", new Vector3(0, -0.05f, 0), new Vector3(60, 0.1f, 60), new Color(0.35f, 0.55f, 0.3f));
        Box(root, "Дорога", new Vector3(0, 0.01f, -4f), new Vector3(60, 0.02f, 4f), new Color(0.25f, 0.25f, 0.28f));

        // ---------- Дом: пол и стены (внутри x -3..3, z 2..8) ----------
        Color wall = new Color(0.85f, 0.8f, 0.7f);
        Box(root, "Пол", new Vector3(0, -0.05f, 5), new Vector3(6, 0.1f, 6), new Color(0.6f, 0.45f, 0.3f));
        Box(root, "Стена_задняя", new Vector3(0, 1.3f, 8.1f), new Vector3(6.4f, 2.6f, 0.2f), wall);
        Box(root, "Стена_левая", new Vector3(-3.1f, 1.3f, 5), new Vector3(0.2f, 2.6f, 6.2f), wall);
        Box(root, "Стена_правая", new Vector3(3.1f, 1.3f, 5), new Vector3(0.2f, 2.6f, 6.2f), wall);
        Box(root, "Стена_передняя_лево", new Vector3(-1.75f, 1.3f, 1.9f), new Vector3(2.5f, 2.6f, 0.2f), wall);
        Box(root, "Стена_передняя_право", new Vector3(1.75f, 1.3f, 1.9f), new Vector3(2.5f, 2.6f, 0.2f), wall);
        Box(root, "Стена_над_дверью", new Vector3(0, 2.3f, 1.9f), new Vector3(1f, 0.6f, 0.2f), wall);

        // ---------- Зоны телепорта (невидимые плоскости чуть выше земли и пола) ----------
        TeleportZone(root, "Телепорт_улица", new Vector3(0, 0.03f, -4f), new Vector2(30, 11));
        TeleportZone(root, "Телепорт_дом", new Vector3(0, 0.03f, 5f), new Vector2(5.6f, 5.6f));

        // ---------- Дверь: петля + полотно ----------
        GameObject hinge = new GameObject("Дверь_петля");
        hinge.transform.SetParent(root.transform);
        hinge.transform.position = new Vector3(-0.5f, 0, 1.9f);
        GameObject panel = Box(hinge, "Дверь_полотно", Vector3.zero, new Vector3(1f, 2f, 0.08f), new Color(0.45f, 0.28f, 0.15f));
        panel.transform.localPosition = new Vector3(0.5f, 1f, 0);
        hinge.AddComponent<Interactable>();
        hinge.AddComponent<DoorController>();

        // ---------- Мебель ----------
        Box(root, "Стол", new Vector3(-2f, 0.4f, 6.8f), new Vector3(1.4f, 0.8f, 0.8f), new Color(0.55f, 0.35f, 0.2f));
        Box(root, "Стул", new Vector3(-2f, 0.25f, 5.8f), new Vector3(0.5f, 0.5f, 0.5f), new Color(0.5f, 0.3f, 0.18f));
        Box(root, "Шкаф", new Vector3(2.5f, 1f, 7.4f), new Vector3(0.8f, 2f, 1.2f), new Color(0.4f, 0.25f, 0.15f));
        Box(root, "Кровать", new Vector3(-2.2f, 0.25f, 3.3f), new Vector3(1f, 0.5f, 2f), new Color(0.7f, 0.7f, 0.85f));
        GameObject carpet = Box(root, "Ковёр", new Vector3(0, 0.01f, 5f), new Vector3(2.5f, 0.02f, 1.8f), new Color(0.7f, 0.2f, 0.2f));
        Object.DestroyImmediate(carpet.GetComponent<BoxCollider>());
        GameObject picture = GameObject.CreatePrimitive(PrimitiveType.Quad);
        picture.name = "Картина";
        picture.transform.SetParent(root.transform);
        picture.transform.position = new Vector3(0, 1.5f, 7.98f);
        picture.transform.localScale = new Vector3(1.2f, 0.8f, 1f);
        Paint(picture, new Color(0.2f, 0.4f, 0.7f));

        // ---------- NPC: корень + тело + точка для купюры ----------
        GameObject npc = new GameObject("NPC");
        npc.transform.SetParent(root.transform);
        npc.transform.position = new Vector3(0.6f, 0, 5.5f);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "NPC_тело";
        body.transform.SetParent(npc.transform);
        body.transform.localPosition = new Vector3(0, 0.9f, 0);
        body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
        Paint(body, new Color(0.9f, 0.7f, 0.3f));
        GameObject moneyPoint = new GameObject("Точка_купюры");
        moneyPoint.transform.SetParent(npc.transform);
        moneyPoint.transform.localPosition = new Vector3(0.2f, 1.1f, 0.5f);

        // ---------- Купюра (Interactable + Throwable + Rigidbody) ----------
        GameObject money = Box(root, "Купюра", moneyPoint.transform.position, new Vector3(0.16f, 0.03f, 0.08f), new Color(0.3f, 0.75f, 0.3f));
        money.AddComponent<Rigidbody>();
        money.AddComponent<Interactable>();
        money.AddComponent<Throwable>();
        GrabbableMoney grabbable = money.AddComponent<GrabbableMoney>();

        NPCController npcCtrl = npc.AddComponent<NPCController>();
        npcCtrl.money = grabbable;
        npcCtrl.moneyPoint = moneyPoint.transform;

        // ---------- Квитанция на ящике у входа ----------
        Box(root, "Ящик", new Vector3(1.5f, 0.4f, 0.4f), new Vector3(0.8f, 0.8f, 0.8f), new Color(0.6f, 0.5f, 0.35f));
        GameObject receipt = Box(root, "Квитанция", new Vector3(1.5f, 0.84f, 0.4f), new Vector3(0.21f, 0.01f, 0.15f), Color.white);
        receipt.AddComponent<Rigidbody>();
        receipt.AddComponent<Interactable>();
        receipt.AddComponent<Throwable>();

        // ---------- Надпись в мире ----------
        GameObject sign = new GameObject("Надпись");
        sign.transform.SetParent(root.transform);
        sign.transform.position = new Vector3(0, 3.4f, 2.5f);
        TextMesh tm = sign.AddComponent<TextMesh>();
        tm.text = "Возьмите оплату за ЖКХ";
        tm.fontSize = 100;
        tm.characterSize = 0.04f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;

        // ---------- TaskManager ----------
        GameObject tmObj = new GameObject("TaskManager");
        tmObj.transform.SetParent(root.transform);
        TaskManager task = tmObj.AddComponent<TaskManager>();
        task.money = grabbable;
        task.taskText = tm;

        // ---------- Player и Teleporting из ассета SteamVR ----------
        GameObject playerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        GameObject teleportAsset = AssetDatabase.LoadAssetAtPath<GameObject>(TeleportingPrefab);
        if (playerAsset == null || teleportAsset == null)
        {
            Debug.LogWarning("ЖКХ: не найдены префабы SteamVR (Player / Teleporting). Сцена собрана без них. Перетащите их из Assets/SteamVR/InteractionSystem вручную: Core/Prefabs/Player и Teleport/Prefabs/Teleporting.");
            return;
        }
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerAsset);
        player.transform.position = new Vector3(0, 0, -1f);
        player.AddComponent<PlayerController>();
        PrefabUtility.InstantiatePrefab(teleportAsset);
        Debug.Log("ЖКХ: сцена собрана. Не забудьте Window -> SteamVR Input -> Save and Generate.");
    }

    // Невидимая плоскость, на которую можно телепортироваться
    static void TeleportZone(GameObject parent, string name, Vector3 pos, Vector2 size)
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = name;
        q.transform.SetParent(parent.transform);
        q.transform.position = pos;
        q.transform.rotation = Quaternion.Euler(90, 0, 0);
        q.transform.localScale = new Vector3(size.x, size.y, 1);
        q.AddComponent<TeleportArea>();
    }

    // Создаёт куб с цветом
    static GameObject Box(GameObject parent, string name, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        go.transform.localScale = scale;
        Paint(go, color);
        return go;
    }

    // Красит объект: создаёт материал и сохраняет его в Assets/Materials
    static void Paint(GameObject go, Color color)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        Shader shader = GraphicsSettings.currentRenderPipeline != null
            ? Shader.Find("Universal Render Pipeline/Lit")
            : Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = color;
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Materials/Zhkh_" + go.name + ".mat");
        AssetDatabase.CreateAsset(mat, path);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
