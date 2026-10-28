using UnityEngine;

// Сам создаёт объект "ZhkhExtras" при нажатии Play. Добавлять в сцену ничего не нужно.
public static class ZhkhBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (GameObject.Find("ZhkhExtras") != null) return;
        GameObject go = new GameObject("ZhkhExtras");
        go.AddComponent<ZhkhPistolCleaner>();
        go.AddComponent<ZhkhLabelHider>();
        go.AddComponent<ZhkhInventoryScreen>();
        go.AddComponent<ZhkhToolKit>();
        go.AddComponent<ZhkhLeakSpawner>();
        go.AddComponent<ZhkhDialogue>();
        go.AddComponent<ZhkhNpcSetup>();
        go.AddComponent<ZhkhRenderTuner>();
        go.AddComponent<ZhkhCleanup>();
    }
}
