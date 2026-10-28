using UnityEditor;
using UnityEngine;

// Удаляет пистолет из сцены и его скрипты (PistolController, UpdatePistol, AddFightPouchPistol).
// Вызывается из меню ЖКХ -> ПОЛНАЯ ДОСТРОЙКА, отдельного пункта меню нет.
public static class RemovePistol
{
    public static void Run()
    {
        GameObject gun = GameObject.Find("Пистолет");
        if (gun != null) Undo.DestroyObjectImmediate(gun);

        string[] files =
        {
            "Assets/Scripts/PistolController.cs",
            "Assets/Editor/UpdatePistol.cs",
            "Assets/Editor/AddFightPouchPistol.cs"
        };
        int removed = 0;
        foreach (string f in files)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(f) != null && AssetDatabase.DeleteAsset(f))
                removed++;
        }
        AssetDatabase.Refresh();
        Debug.Log("ЖКХ: пистолет убран, удалено файлов: " + removed);
    }
}
