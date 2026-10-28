using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Valve.VR.InteractionSystem;

// Меню Unity: ЖКХ -> Добавить отпор, мешочек и пистолет
// 1) вешает NPCFightBack на всех NPC в сцене (и в новых домах тоже);
// 2) создаёт пистолет рядом с квитанцией у входа.
// Мешочек и здоровье игрока создаются сами при запуске игры.
// Старые объекты и скрипты не меняются.
public static class AddFightPouchPistol
{
    [MenuItem("ЖКХ/Добавить отпор, мешочек и пистолет")]
    public static void Add()
    {
        // ---------- Отпор для всех NPC ----------
        int npcCount = 0;
        NPCController[] npcs = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        foreach (NPCController c in npcs)
        {
            GameObject npc = c.gameObject;
            if (npc.GetComponent<NPCFightBack>() == null)
            {
                Undo.AddComponent<NPCFightBack>(npc);
                npcCount++;
            }
            // Чтобы пули попадали в NPC, нужен коллайдер
            if (npc.GetComponentInChildren<Collider>() == null)
            {
                CapsuleCollider cap = Undo.AddComponent<CapsuleCollider>(npc);
                cap.center = new Vector3(0, 0.9f, 0);
                cap.height = 1.8f;
                cap.radius = 0.3f;
            }
        }

        // ---------- Пистолет ----------
        if (GameObject.Find("Пистолет") == null)
            CreatePistol();

        if (npcCount > 0 || npcs.Length > 0)
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("ЖКХ: отпор добавлен NPC: " + npcCount + ", пистолет лежит у входа. Мешочек появится при запуске игры.");
    }

    static void CreatePistol()
    {
        // Кладём пистолет на ящик рядом с квитанцией, если ящик есть
        Vector3 pos = new Vector3(1.75f, 0.9f, 0.4f);
        GameObject crate = GameObject.Find("Ящик");
        if (crate != null) pos = crate.transform.position + new Vector3(0.25f, 0.45f, 0f);

        GameObject gun = new GameObject("Пистолет");
        gun.transform.position = pos;

        Color dark = new Color(0.12f, 0.12f, 0.14f);
        Part(gun, "Затвор", new Vector3(0, 0.03f, 0.02f), new Vector3(0.03f, 0.04f, 0.2f), dark);
        GameObject grip = Part(gun, "Рукоять", new Vector3(0, -0.03f, -0.05f), new Vector3(0.03f, 0.09f, 0.045f), dark);
        grip.transform.localRotation = Quaternion.Euler(15f, 0, 0);

        GameObject muzzle = new GameObject("Дуло");
        muzzle.transform.SetParent(gun.transform, false);
        muzzle.transform.localPosition = new Vector3(0, 0.03f, 0.12f);

        // Один общий коллайдер, чтобы пистолет нормально падал и отскакивал
        BoxCollider box = gun.AddComponent<BoxCollider>();
        box.center = new Vector3(0, 0, 0);
        box.size = new Vector3(0.04f, 0.12f, 0.22f);

        Rigidbody rb = gun.AddComponent<Rigidbody>();
        rb.mass = 0.6f;

        gun.AddComponent<Interactable>();
        PistolController pc = gun.AddComponent<PistolController>();
        pc.muzzle = muzzle.transform;

        Undo.RegisterCreatedObjectUndo(gun, "Пистолет");
    }

    static GameObject Part(GameObject parent, string name, Vector3 localPos, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = GetMat(color);
        return go;
    }

    static Material GetMat(Color c)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        string path = "Assets/Materials/Zhkh3_" + ColorUtility.ToHtmlStringRGB(c) + ".mat";
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
