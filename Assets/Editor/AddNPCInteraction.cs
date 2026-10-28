using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valve.VR.InteractionSystem;

// Меню Unity: ЖКХ -> Добавить бой и разговор NPC
// Вешает на существующий объект NPC новые компоненты. Ничего другого в сцене не меняет.
public static class AddNPCInteraction
{
    [MenuItem("ЖКХ/Добавить бой и разговор NPC")]
    public static void Add()
    {
        GameObject npc = GameObject.Find("NPC");
        if (npc == null)
        {
            Debug.LogWarning("ЖКХ: объект с именем NPC не найден в сцене.");
            return;
        }

        if (npc.GetComponent<Interactable>() == null) Undo.AddComponent<Interactable>(npc);
        if (npc.GetComponent<NPCInteraction>() == null) Undo.AddComponent<NPCInteraction>(npc);

        // Для наведения руки нужен коллайдер; если у модели его нет, добавляем капсулу
        if (npc.GetComponentInChildren<Collider>() == null)
        {
            CapsuleCollider c = Undo.AddComponent<CapsuleCollider>(npc);
            c.center = new Vector3(0, 0.9f, 0);
            c.height = 1.8f;
            c.radius = 0.3f;
        }

        EditorSceneManager.MarkSceneDirty(npc.scene);
        Debug.Log("ЖКХ: NPC теперь можно бить и говорить с ним.");
    }
}
