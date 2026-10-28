using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Меню: ЖКХ → Добавить руку с купюрой всем NPC (если сцена уже собрана раньше).
public static class ZhkhAddHandRigMenu
{
    [MenuItem("ЖКХ/Добавить руку с купюрой всем NPC")]
    static void Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        NPCFightBack[] all = Object.FindObjectsOfType<NPCFightBack>(true);
        int n = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || EditorUtility.IsPersistent(all[i]) || all[i].gameObject.scene != scene) continue;
            GameObject g = all[i].gameObject;
            if (g.GetComponent<ZhkhNpcAnimator>() == null) Undo.AddComponent<ZhkhNpcAnimator>(g);
            if (g.GetComponent<ZhkhHandRig>() == null) Undo.AddComponent<ZhkhHandRig>(g);
            if (g.GetComponent<ZhkhBillHold>() == null) Undo.AddComponent<ZhkhBillHold>(g);
            n++;
        }
        if (n > 0) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog("ЖКХ", "Рука с купюрой добавлена NPC: " + n + ". Сохраните сцену (Ctrl+S).", "OK");
    }

    [MenuItem("ЖКХ/Вернуть камеры как в SteamVR (near 0.01 / far 1000)")]
    static void ResetCameras()
    {
        Camera[] cams = Object.FindObjectsOfType<Camera>(true);
        int n = 0;
        for (int i = 0; i < cams.Length; i++)
        {
            if (EditorUtility.IsPersistent(cams[i])) continue;
            Undo.RecordObject(cams[i], "cam");
            cams[i].nearClipPlane = 0.01f;
            cams[i].farClipPlane = 1000f;
            n++;
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("ЖКХ", "Камеры возвращены: " + n + ". Сохраните сцену. Чтобы скрипт не менял их при запуске, отключите ZhkhRenderTuner (удалите его строку в ZhkhBootstrap.cs).", "OK");
    }
}
