using UnityEngine;
using UnityEngine.EventSystems;

// Исправление ошибки "You are trying to read Input using the UnityEngine.Input class".
// Убирает старый модуль ввода мыши (StandaloneInputModule) из EventSystem.
// В игре нет меню и кнопок, поэтому он не нужен. Запускается сам, вешать никуда не надо.
public class FixEventSystem : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        GameObject go = new GameObject("FixEventSystem");
        DontDestroyOnLoad(go);
        go.AddComponent<FixEventSystem>();
    }

    void Update()
    {
        // Ищем и удаляем старый модуль ввода, если он есть (в том числе созданный позже)
        StandaloneInputModule[] modules = FindObjectsByType<StandaloneInputModule>(FindObjectsSortMode.None);
        foreach (StandaloneInputModule m in modules)
            Destroy(m);
    }
}
