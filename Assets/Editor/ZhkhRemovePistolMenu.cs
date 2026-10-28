using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Меню: ЖКХ → Убрать пистолет и убийство. Удаляет из открытой сцены объект пистолета.
public static class ZhkhRemovePistolMenu
{
    [MenuItem("ЖКХ/Убрать пистолет и убийство")]
    static void Run()
    {
        List<MonoBehaviour> targets = new List<MonoBehaviour>();
        MonoBehaviour[] all = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
        for (int i = 0; i < all.Length; i++)
        {
            MonoBehaviour m = all[i];
            if (m == null) continue;
            if (EditorUtility.IsPersistent(m)) continue;
            if (!m.gameObject.scene.IsValid()) continue;
            if (m.GetType().Name != "PistolController") continue;
            targets.Add(m);
        }
        int removed = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            MonoBehaviour m = targets[i];
            if (m == null) continue;
            if (m.transform.childCount > 6) Undo.DestroyObjectImmediate(m);       // на всякий случай не удаляем большие объекты целиком
            else Undo.DestroyObjectImmediate(m.gameObject);
            removed++;
        }
        if (removed > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("ЖКХ", removed > 0
            ? "Пистолет убран из сцены (" + removed + " шт.). Сохраните сцену (Ctrl+S)."
            : "Пистолет в открытой сцене не найден.", "OK");
    }
}
