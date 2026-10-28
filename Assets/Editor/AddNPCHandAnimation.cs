using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Меню Unity: ЖКХ -> Добавить анимацию руки NPC
// Вешает NPCHandAnimation на существующий объект NPC. Больше ничего не меняет.
public static class AddNPCHandAnimation
{
    [MenuItem("ЖКХ/Добавить анимацию руки NPC")]
    public static void Add()
    {
        GameObject npc = GameObject.Find("NPC");
        if (npc == null)
        {
            Debug.LogWarning("ЖКХ: объект с именем NPC не найден в сцене.");
            return;
        }

        Animator anim = npc.GetComponentInChildren<Animator>();
        if (anim == null)
            Debug.LogWarning("ЖКХ: внутри NPC нет модели с Animator. Сначала поставьте модель мужчины внутрь NPC.");
        else if (!anim.isHuman)
            Debug.LogWarning("ЖКХ: у модели не Humanoid. Выберите FBX -> Rig -> Animation Type: Humanoid -> Apply.");

        if (npc.GetComponent<NPCHandAnimation>() == null) Undo.AddComponent<NPCHandAnimation>(npc);
        EditorSceneManager.MarkSceneDirty(npc.scene);
        Debug.Log("ЖКХ: анимация руки NPC добавлена.");
    }
}
