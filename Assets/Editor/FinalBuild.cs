using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Valve.VR.InteractionSystem;

// Меню Unity: ЖКХ -> ПОЛНАЯ ДОСТРОЙКА (всё сразу)
// Одним нажатием:
//  1. убирает драки и пистолет (игра больше не зависает от боя);
//  2. настраивает каждого NPC: универсальная анимация подачи денег (NPCAnimator);
//  3. исправляет мерцание полов и стен (z-fighting);
//  4. делает небо, туман, мягкий свет и тени;
//  5. подключает шейдеры воды и свечения и добавляет блеск материалам;
//  6. чистит «Missing Script».
// Можно нажимать повторно, лишнего не создаётся.
public static class FinalBuild
{
    [MenuItem("ЖКХ/ПОЛНАЯ ДОСТРОЙКА (всё сразу)")]
    public static void Run()
    {
        UpgradeNpcs();
        FixFlicker();
        SetupLightingAndSky();
        ApplyShadersAndMaterials();
        CleanMissingScripts();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        RemovePistol.Run();   // в самом конце: удаляет файлы, после этого Unity перекомпилируется
        Debug.Log("ЖКХ: полная достройка выполнена. Сохраните сцену (Ctrl+S).");
    }

    // ---------- 1-2. NPC: убираем бой, ставим универсальную анимацию ----------
    static void UpgradeNpcs()
    {
        NPCController[] npcs = Object.FindObjectsByType<NPCController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        foreach (NPCController c in npcs)
        {
            GameObject npc = c.gameObject;
            foreach (MonoBehaviour mb in npc.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                if (mb.GetType().Name == "NPCHandAnimation" || mb is NPCFightBack)
                    Undo.DestroyObjectImmediate(mb);
            }
            if (npc.GetComponent<NPCAnimator>() == null) Undo.AddComponent<NPCAnimator>(npc);
            count++;
        }
        Debug.Log("ЖКХ: NPC настроено: " + count);
    }

    // ---------- 3. Мерцание (z-fighting): поверхности в одной плоскости ----------
    static void FixFlicker()
    {
        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform t in all)
        {
            switch (t.name)
            {
                case "Земля":
                    // верх земли ниже верха пола домов (раньше они лежали в одной плоскости)
                    Undo.RecordObject(t, "Земля ниже");
                    t.position = new Vector3(t.position.x, -0.065f, t.position.z);
                    break;

                case "Стена_левая":
                case "Стена_правая":
                case "Эт2_стена_левая":
                case "Эт2_стена_правая":
                    // боковые стены не заходят в заднюю (раньше углы перекрывались)
                    Undo.RecordObject(t, "Стена короче");
                    t.localScale = new Vector3(t.localScale.x, t.localScale.y, 6.1f);
                    t.localPosition = new Vector3(t.localPosition.x, t.localPosition.y, 4.95f);
                    break;

                case "Потолок":
                    Undo.RecordObject(t, "Потолок уже");
                    t.localScale = new Vector3(6.3f, t.localScale.y, 6.3f);
                    break;

                case "Лужа":
                    // лужа лежала внутри тротуара
                    Undo.RecordObject(t, "Лужа выше");
                    t.position = new Vector3(t.position.x, 0.036f, t.position.z);
                    break;
            }
        }

        // Невидимые зоны телепорта на разной высоте, чтобы не лежали в одной плоскости с полом и друг с другом
        TeleportArea[] areas = Object.FindObjectsByType<TeleportArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < areas.Length; i++)
        {
            Transform t = areas[i].transform;
            if (t.name.StartsWith("Телепорт_ступень")) continue;
            Vector3 p = t.position;
            float offset = (i % 12) * 0.002f;
            if (p.y < 0.5f) { Undo.RecordObject(t, "Зона телепорта"); t.position = new Vector3(p.x, 0.045f + offset, p.z); }
            else if (p.y > 2.6f && p.y < 2.9f) { Undo.RecordObject(t, "Зона телепорта"); t.position = new Vector3(p.x, 2.75f + offset, p.z); }
        }
    }

    // ---------- 4. Небо, туман, свет, тени ----------
    static void SetupLightingAndSky()
    {
        // Солнце
        Light sun = RenderSettings.sun;
        if (sun == null)
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Light l in lights)
                if (l.type == LightType.Directional) { sun = l; break; }
        }
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            sun.intensity = 1.15f;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.shadows = LightShadows.Soft;
            sun.shadowBias = 0.03f;          // меньше «ряби» теней на полах
            sun.shadowNormalBias = 0.5f;
            sun.shadowStrength = 0.9f;
            RenderSettings.sun = sun;
        }

        // Небо
        Shader skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            EnsureMaterialsFolder();
            string path = "Assets/Materials/ZhkhSky.mat";
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null)
            {
                sky = new Material(skyShader);
                AssetDatabase.CreateAsset(sky, path);
            }
            sky.SetFloat("_SunSize", 0.04f);
            sky.SetFloat("_AtmosphereThickness", 1.0f);
            sky.SetColor("_SkyTint", new Color(0.5f, 0.55f, 0.65f));
            sky.SetColor("_GroundColor", new Color(0.45f, 0.5f, 0.45f));
            sky.SetFloat("_Exposure", 1.25f);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1f;
        }

        // Лёгкий туман скрывает край карты
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.75f, 0.82f, 0.9f);
        RenderSettings.fogDensity = 0.006f;

        // Камеры показывают небо
        Camera[] cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Camera cam in cams) cam.clearFlags = CameraClearFlags.Skybox;

        QualitySettings.shadowDistance = 45f;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        DynamicGI.UpdateEnvironment();
    }

    // ---------- 5. Шейдеры и блеск материалов ----------
    static void ApplyShadersAndMaterials()
    {
        Shader water = Shader.Find("ZHKH/Water");
        Shader glow = Shader.Find("ZHKH/Glow");
        if (water == null || glow == null)
            Debug.LogWarning("ЖКХ: шейдеры ZHKH/Water и ZHKH/Glow не найдены (проверьте папку Assets/Shaders). Пропускаю.");

        Material waterMat = null;
        Material glowMat = null;
        if (water != null) waterMat = MakeMat("Assets/Materials/ZhkhWater.mat", water, new Color(0.2f, 0.5f, 0.9f, 0.65f));
        if (glow != null) glowMat = MakeMat("Assets/Materials/ZhkhGlow.mat", glow, new Color(1f, 0.92f, 0.55f, 1f));

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Renderer r in renderers)
        {
            string n = r.gameObject.name;
            if (waterMat != null && (n == "Струя" || n == "Лужа"))
            {
                Undo.RecordObject(r, "Вода");
                r.sharedMaterial = waterMat;
            }
            else if (glowMat != null && (n == "Фонарь" || n == "Свечение" || n == "Ночник"))
            {
                Undo.RecordObject(r, "Свечение");
                r.sharedMaterial = glowMat;
            }

            // Блеск: гладкость и металличность по названию предмета
            switch (n)
            {
                case "Кузов": Surface(r, 0.75f, 0.5f); break;
                case "Кабина": Surface(r, 0.9f, 0f); break;
                case "Крыша_левая":
                case "Крыша_правая": Surface(r, 0.25f, 0f); break;
                case "Пол": Surface(r, 0.35f, 0f); break;
                case "Телевизор":
                case "Монитор": Surface(r, 0.9f, 0f); break;
                case "Ванна":
                case "Унитаз":
                case "Раковина_чаша": Surface(r, 0.8f, 0f); break;
                case "Дорога": Surface(r, 0.3f, 0f); break;
            }
        }
    }

    static Material MakeMat(string path, Shader shader, Color color)
    {
        EnsureMaterialsFolder();
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = shader;
        m.SetColor("_Color", color);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void Surface(Renderer r, float smoothness, float metallic)
    {
        Material m = r.sharedMaterial;
        if (m == null) return;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);   // URP
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);   // Standard
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(m);
    }

    static void EnsureMaterialsFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
    }

    // ---------- 6. Missing Script ----------
    static void CleanMissingScripts()
    {
        int removed = 0;
        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform t in all)
        {
            try { removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject); }
            catch (System.Exception) { }
        }
        if (removed > 0) Debug.Log("ЖКХ: удалено пустых скриптов (Missing Script): " + removed);
    }
}
