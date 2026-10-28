using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Меню: ЖКХ → Убрать перчатки и чужой инвентарь (в сцене).
// Удаляет из сцены объекты «Перчатка…» и отключает скрипты с «glove»/«inventory» в названии (кроме PouchInventory и наших Zhkh*).
public static class ZhkhCleanupMenu
{
    static bool IsSteamVRStuff(Transform t)
    {
        MonoBehaviour[] ms = t.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < ms.Length; i++)
        {
            if (ms[i] == null) continue;
            string n = ms[i].GetType().Name;
            if (n.StartsWith("SteamVR_") || n == "Hand") return true;
        }
        return false;
    }

    // Нельзя удалять модели рук SteamVR: всё, где есть SkinnedMeshRenderer, и всё внутри объектов SteamVR/Hand/Player
    static bool IsProtected(Transform t)
    {
        if (t.GetComponentInChildren<SkinnedMeshRenderer>(true) != null) return true;
        for (Transform p = t; p != null; p = p.parent)
        {
            string n = p.name.ToLowerInvariant();
            if (n.StartsWith("vr_") || n.Contains("rendermodel") || n.Contains("render model")) return true;
            MonoBehaviour[] ms = p.GetComponents<MonoBehaviour>();
            for (int i = 0; i < ms.Length; i++)
            {
                if (ms[i] == null) continue;
                string tn = ms[i].GetType().Name;
                if (tn.StartsWith("SteamVR_") || tn == "Hand" || tn == "Player") return true;
            }
        }
        return false;
    }

    static bool HasPouch(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent)
        {
            MonoBehaviour[] ms = p.GetComponents<MonoBehaviour>();
            for (int i = 0; i < ms.Length; i++) if (ms[i] != null && ms[i].GetType().Name == "PouchInventory") return true;
        }
        MonoBehaviour[] cs = t.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < cs.Length; i++) if (cs[i] != null && cs[i].GetType().Name == "PouchInventory") return true;
        return false;
    }

    [MenuItem("ЖКХ/Убрать перчатки и чужой инвентарь")]
    static void Run()
    {
        StringBuilder log = new StringBuilder();
        int objs = 0, scripts = 0;

        MonoBehaviour[] all = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
        for (int i = 0; i < all.Length; i++)
        {
            MonoBehaviour m = all[i];
            if (m == null || EditorUtility.IsPersistent(m) || !m.gameObject.scene.IsValid() || !m.enabled) continue;
            string tn = m.GetType().Name;
            string l = tn.ToLowerInvariant();
            if (l.StartsWith("zhkh") || l.StartsWith("steamvr")) continue;
            bool glove = l.Contains("glove") || l.Contains("перчат") || l.Contains("handvisual");
            bool inventory = l.Contains("inventory") && l != "pouchinventory";
            if (!glove && !inventory) continue;
            Undo.RecordObject(m, "off");
            m.enabled = false;
            scripts++;
            log.AppendLine("Скрипт отключён: " + tn + " (объект «" + m.gameObject.name + "»)");
        }

        List<GameObject> kill = new List<GameObject>();
        Transform[] ts = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < ts.Length; i++)
        {
            Transform t = ts[i];
            if (t == null || EditorUtility.IsPersistent(t) || !t.gameObject.scene.IsValid()) continue;
            string n = t.name.ToLowerInvariant();
            if (n.StartsWith("zhkh")) continue;
            bool glove = (n.Contains("перчат") && t.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
                || (n.Contains("glove") && !IsProtected(t));
            bool inv = (n.Contains("инвентар") || n.Contains("inventory")) && !HasPouch(t);
            if (glove || inv) kill.Add(t.gameObject);
        }
        for (int i = 0; i < kill.Count; i++)
        {
            if (kill[i] == null) continue;
            log.AppendLine("Объект удалён: «" + kill[i].name + "»");
            Undo.DestroyObjectImmediate(kill[i]);
            objs++;
        }

        if (objs + scripts > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("ЖКХ-чистка сцены:\n" + log);
        EditorUtility.DisplayDialog("ЖКХ", "Объектов удалено: " + objs + ", скриптов отключено: " + scripts + ".\n\n" + log + "\nСохраните сцену (Ctrl+S).", "OK");
    }
}
