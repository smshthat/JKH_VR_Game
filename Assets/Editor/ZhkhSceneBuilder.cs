using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Valve.VR.InteractionSystem;

// Меню: ЖКХ → Собрать сцену (всё)
// Делает за один раз: копия сцены, удаление пистолета, настройка NPC (анимации + купюра в руке),
// исправление мерцания полов и дублей, настройка камер, освещение/небо/туман, красивые шейдеры.
public static class ZhkhSceneBuilder
{
    const string MatFolder = "Assets/ZhkhMaterials";

    [MenuItem("ЖКХ/Собрать сцену (всё)")]
    static void BuildAll()
    {
        if (!EditorUtility.DisplayDialog("ЖКХ: сборка сцены",
            "Будет сделана копия сцены в Assets/Zhkh_Backup, затем сцена изменится: пистолет, NPC, полы, камеры, освещение, материалы.\n\nПродолжить?",
            "Собрать", "Отмена")) return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.GetActiveScene();
        StringBuilder log = new StringBuilder();
        Backup(scene, log);

        EditorUtility.DisplayProgressBar("ЖКХ", "Пистолет", 0.05f);
        RemovePistols(log);
        EditorUtility.DisplayProgressBar("ЖКХ", "NPC", 0.2f);
        SetupNpcs(scene, log);
        EditorUtility.DisplayProgressBar("ЖКХ", "Мерцание полов", 0.4f);
        FixZFighting(scene, log);
        EditorUtility.DisplayProgressBar("ЖКХ", "Камеры", 0.6f);
        SetupCameras(log);
        EditorUtility.DisplayProgressBar("ЖКХ", "Освещение и небо", 0.75f);
        SetupEnvironment(log);
        EditorUtility.DisplayProgressBar("ЖКХ", "Шейдеры", 0.9f);
        UpgradeMaterials(scene, log);
        EditorUtility.ClearProgressBar();

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ЖКХ сборка сцены:\n" + log);
        EditorUtility.DisplayDialog("ЖКХ: готово", log + "\nСохраните сцену (Ctrl+S).", "OK");
    }

    static void Backup(Scene scene, StringBuilder log)
    {
        if (string.IsNullOrEmpty(scene.path)) { log.AppendLine("Копия сцены: сцена не сохранена на диск, копия не сделана."); return; }
        if (!AssetDatabase.IsValidFolder("Assets/Zhkh_Backup")) AssetDatabase.CreateFolder("Assets", "Zhkh_Backup");
        string dst = AssetDatabase.GenerateUniqueAssetPath("Assets/Zhkh_Backup/" + Path.GetFileNameWithoutExtension(scene.path) + "_before_build.unity");
        AssetDatabase.CopyAsset(scene.path, dst);
        log.AppendLine("Копия сцены: " + dst);
    }

    static bool InScene(Component c, Scene scene)
    {
        return c != null && !EditorUtility.IsPersistent(c) && c.gameObject.scene == scene;
    }

    // ---------- 1. пистолет ----------
    static void RemovePistols(StringBuilder log)
    {
        List<MonoBehaviour> targets = new List<MonoBehaviour>();
        MonoBehaviour[] all = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
        for (int i = 0; i < all.Length; i++)
        {
            MonoBehaviour m = all[i];
            if (m == null || EditorUtility.IsPersistent(m) || !m.gameObject.scene.IsValid()) continue;
            if (m.GetType().Name == "PistolController") targets.Add(m);
        }
        int n = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;
            if (targets[i].transform.childCount > 6) Undo.DestroyObjectImmediate(targets[i]);
            else Undo.DestroyObjectImmediate(targets[i].gameObject);
            n++;
        }
        log.AppendLine("Пистолет: убрано " + n);
    }

    // ---------- 2. NPC ----------
    static void SetupNpcs(Scene scene, StringBuilder log)
    {
        NPCFightBack[] all = Object.FindObjectsOfType<NPCFightBack>(true);
        int n = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (!InScene(all[i], scene)) continue;
            GameObject g = all[i].gameObject;
            if (g.GetComponent<ZhkhNpcAnimator>() == null) Undo.AddComponent<ZhkhNpcAnimator>(g);
            if (g.GetComponent<ZhkhHandRig>() == null) Undo.AddComponent<ZhkhHandRig>(g);
            if (g.GetComponent<ZhkhBillHold>() == null) Undo.AddComponent<ZhkhBillHold>(g);
            n++;
        }
        log.AppendLine("NPC: анимации, рука и купюра в руке добавлены " + n + " персонажам");
    }

    // ---------- 3. мерцание ----------
    class Slab
    {
        public MeshRenderer r; public float minX, maxX, minZ, maxZ, top, area, lift;
    }

    static bool IsDynamic(Transform t)
    {
        Rigidbody rb = t.GetComponentInParent<Rigidbody>();
        if (rb != null && !rb.isKinematic) return true;
        return t.GetComponentInParent<Interactable>() != null
            || t.GetComponentInParent<NPCFightBack>() != null
            || t.GetComponentInParent<Player>() != null
            || t.GetComponentInParent<TeleportArea>() != null
            || t.GetComponentInParent<TeleportPoint>() != null
            || t.GetComponentInChildren<Player>(true) != null;
    }

    static int Q(float f) { return Mathf.RoundToInt(f * 100f); }

    static void FixZFighting(Scene scene, StringBuilder log)
    {
        MeshRenderer[] all = Object.FindObjectsOfType<MeshRenderer>();
        List<MeshRenderer> list = new List<MeshRenderer>();
        for (int i = 0; i < all.Length; i++)
        {
            MeshRenderer r = all[i];
            if (!InScene(r, scene) || !r.enabled) continue;
            if (r.GetComponent<TextMesh>() != null) continue;
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            if (IsDynamic(r.transform)) continue;
            list.Add(r);
        }

        // а) полные дубли (одинаковый меш, материал, место и размер)
        Dictionary<string, MeshRenderer> seen = new Dictionary<string, MeshRenderer>();
        int dup = 0;
        for (int i = 0; i < list.Count; i++)
        {
            MeshRenderer r = list[i];
            Bounds b = r.bounds;
            Material m = r.sharedMaterial;
            string key = r.GetComponent<MeshFilter>().sharedMesh.GetInstanceID() + "|" + (m != null ? m.GetInstanceID() : 0) + "|"
                + Q(b.center.x) + "," + Q(b.center.y) + "," + Q(b.center.z) + "|" + Q(b.size.x) + "," + Q(b.size.y) + "," + Q(b.size.z);
            if (seen.ContainsKey(key)) { Undo.RecordObject(r, "dup"); r.enabled = false; dup++; }
            else seen[key] = r;
        }

        // б) плоские плиты на одной высоте (полы, дорожки, разметка, ковры)
        List<Slab> slabs = new List<Slab>();
        for (int i = 0; i < list.Count; i++)
        {
            MeshRenderer r = list[i];
            if (!r.enabled) continue;
            Bounds b = r.bounds;
            if (b.size.y > 0.35f || b.size.x < 0.2f || b.size.z < 0.2f) continue;
            Slab s = new Slab();
            s.r = r; s.minX = b.min.x; s.maxX = b.max.x; s.minZ = b.min.z; s.maxZ = b.max.z;
            s.top = b.max.y; s.area = b.size.x * b.size.z;
            slabs.Add(s);
        }
        slabs.Sort(delegate (Slab a, Slab b2) { return b2.area.CompareTo(a.area); });

        int lifted = 0;
        for (int i = 0; i < slabs.Count; i++)
        {
            Slab c = slabs[i];
            for (int pass = 0; pass < 8; pass++)
            {
                bool moved = false;
                for (int j = 0; j < i; j++)
                {
                    Slab p = slabs[j];
                    float ox = Mathf.Min(c.maxX, p.maxX) - Mathf.Max(c.minX, p.minX);
                    float oz = Mathf.Min(c.maxZ, p.maxZ) - Mathf.Max(c.minZ, p.minZ);
                    if (ox <= 0.02f || oz <= 0.02f) continue;
                    if (Mathf.Abs(c.top - p.top) >= 0.005f) continue;
                    float delta = p.top + 0.006f - c.top;
                    if (c.lift + delta > 0.03f) continue;
                    c.top += delta; c.lift += delta;
                    moved = true;
                }
                if (!moved) break;
            }
            if (c.lift > 0f)
            {
                Undo.RecordObject(c.r.transform, "zfix");
                c.r.transform.position += Vector3.up * c.lift;
                lifted++;
            }
        }
        log.AppendLine("Мерцание: отключено дублей " + dup + ", приподнято плит на одной высоте " + lifted);
    }

    // ---------- 4. камеры ----------
    static void SetupCameras(StringBuilder log)
    {
        Camera[] cams = Object.FindObjectsOfType<Camera>(true);
        int n = 0;
        for (int i = 0; i < cams.Length; i++)
        {
            if (EditorUtility.IsPersistent(cams[i])) continue;
            Undo.RecordObject(cams[i], "cam");
            cams[i].nearClipPlane = 0.05f;
            cams[i].farClipPlane = 500f;
            cams[i].allowMSAA = true;
            n++;
        }
        log.AppendLine("Камеры: near 0.05 / far 500 у " + n + " шт. (точнее глубина, меньше мерцания)");
    }

    // ---------- 5. свет, небо, туман ----------
    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(MatFolder)) AssetDatabase.CreateFolder("Assets", "ZhkhMaterials");
    }

    static void SetupEnvironment(StringBuilder log)
    {
        EnsureFolder();
        Shader sky = Shader.Find("Skybox/Procedural");
        if (sky != null)
        {
            string path = MatFolder + "/ZhkhSky.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sky); AssetDatabase.CreateAsset(m, path); }
            m.SetFloat("_SunSize", 0.045f);
            m.SetFloat("_AtmosphereThickness", 1.05f);
            m.SetColor("_SkyTint", new Color(0.5f, 0.55f, 0.62f));
            m.SetColor("_GroundColor", new Color(0.45f, 0.43f, 0.4f));
            m.SetFloat("_Exposure", 1.25f);
            EditorUtility.SetDirty(m);
            RenderSettings.skybox = m;
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.7f, 0.95f);
        RenderSettings.ambientEquatorColor = new Color(0.72f, 0.75f, 0.76f);
        RenderSettings.ambientGroundColor = new Color(0.34f, 0.32f, 0.3f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.005f;
        RenderSettings.fogColor = new Color(0.74f, 0.82f, 0.9f);
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = 0.8f;

        Light sun = RenderSettings.sun;
        if (sun == null)
        {
            Light[] ls = Object.FindObjectsOfType<Light>(true);
            for (int i = 0; i < ls.Length; i++)
                if (ls[i].type == LightType.Directional && !EditorUtility.IsPersistent(ls[i])) { sun = ls[i]; break; }
        }
        if (sun == null)
        {
            GameObject g = new GameObject("ZhkhSun");
            sun = g.AddComponent<Light>();
            sun.type = LightType.Directional;
            g.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Undo.RegisterCreatedObjectUndo(g, "sun");
        }
        Undo.RecordObject(sun, "sun");
        sun.color = new Color(1f, 0.96f, 0.88f);
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.85f;
        RenderSettings.sun = sun;
        DynamicGI.UpdateEnvironment();
        log.AppendLine("Освещение: процедурное небо, тройной ambient, лёгкий туман, мягкие тени от солнца");
    }

    // ---------- 6. материалы и шейдеры ----------
    static bool IsConvertible(Material m)
    {
        if (m == null || m.shader == null) return false;
        string n = m.shader.name;
        if (n != "Standard" && n != "Diffuse" && n != "Legacy Shaders/Diffuse" && n != "Mobile/Diffuse" && n != "Legacy Shaders/Bumped Diffuse") return false;
        if (m.HasProperty("_Mode") && m.GetFloat("_Mode") > 0.5f) return false; // прозрачные и вырезные не трогаем
        if (m.renderQueue >= 2450) return false;
        return true;
    }

    static string Safe(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s;
    }

    static Material Convert(Material src, Shader sh, string suffix, Dictionary<Material, Material> cache)
    {
        Material m;
        if (cache.TryGetValue(src, out m)) return m;
        m = new Material(sh);
        if (src.HasProperty("_Color")) m.SetColor("_Color", src.GetColor("_Color"));
        if (src.HasProperty("_MainTex"))
        {
            m.SetTexture("_MainTex", src.GetTexture("_MainTex"));
            m.SetTextureScale("_MainTex", src.GetTextureScale("_MainTex"));
            m.SetTextureOffset("_MainTex", src.GetTextureOffset("_MainTex"));
        }
        if (m.HasProperty("_BumpMap") && src.HasProperty("_BumpMap") && src.GetTexture("_BumpMap") != null)
            m.SetTexture("_BumpMap", src.GetTexture("_BumpMap"));
        if (m.HasProperty("_Glossiness") && src.HasProperty("_Glossiness"))
            m.SetFloat("_Glossiness", Mathf.Clamp(src.GetFloat("_Glossiness"), 0.05f, 0.7f));
        if (m.HasProperty("_Metallic") && src.HasProperty("_Metallic")) m.SetFloat("_Metallic", src.GetFloat("_Metallic"));
        if (m.HasProperty("_EmissionColor") && src.HasProperty("_EmissionColor") && src.IsKeywordEnabled("_EMISSION"))
            m.SetColor("_EmissionColor", src.GetColor("_EmissionColor"));
        string path = AssetDatabase.GenerateUniqueAssetPath(MatFolder + "/" + Safe(src.name) + suffix + ".mat");
        AssetDatabase.CreateAsset(m, path);
        cache[src] = m;
        return m;
    }

    static bool LooksLikeFoliage(Renderer r, Material m)
    {
        if (!m.HasProperty("_Color")) return false;
        Color c = m.GetColor("_Color");
        Bounds b = r.bounds;
        return c.g > c.r * 1.15f && c.g > c.b * 1.1f && b.center.y > 1.2f && b.size.y > 0.3f && b.size.y < 8f;
    }

    static void UpgradeMaterials(Scene scene, StringBuilder log)
    {
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            log.AppendLine("Шейдеры: в проекте URP/HDRP, свои шейдеры (для Built-in) не применялись. Свет и камеры настроены.");
            return;
        }
        Shader pretty = Shader.Find("ZHKH/Pretty");
        Shader foliage = Shader.Find("ZHKH/Foliage");
        if (pretty == null || foliage == null)
        {
            log.AppendLine("Шейдеры: ZHKH/Pretty или ZHKH/Foliage не найдены (проверьте консоль на ошибки компиляции шейдеров).");
            return;
        }
        EnsureFolder();
        Dictionary<Material, Material> cacheP = new Dictionary<Material, Material>();
        Dictionary<Material, Material> cacheF = new Dictionary<Material, Material>();
        Renderer[] all = Object.FindObjectsOfType<Renderer>(true);
        int changed = 0, trees = 0;
        for (int i = 0; i < all.Length; i++)
        {
            Renderer r = all[i];
            if (!InScene(r, scene)) continue;
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            if (r.GetComponent<TextMesh>() != null) continue;
            Material[] mats = r.sharedMaterials;
            bool dirty = false;
            for (int k = 0; k < mats.Length; k++)
            {
                Material m = mats[k];
                if (!IsConvertible(m)) continue;
                if (LooksLikeFoliage(r, m)) { mats[k] = Convert(m, foliage, "_leaf", cacheF); trees++; }
                else mats[k] = Convert(m, pretty, "_zhkh", cacheP);
                dirty = true;
            }
            if (dirty) { Undo.RecordObject(r, "mats"); r.sharedMaterials = mats; changed++; }
        }
        log.AppendLine("Шейдеры: обновлено объектов " + changed + " (из них листва с ветром: " + trees + "), материалы в " + MatFolder);
    }
}
