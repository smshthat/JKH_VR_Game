using UnityEngine;
using Valve.VR.InteractionSystem;

// Прячет уличные надписи «Возьмите оплату за ЖКХ» над домами, пока игрок находится внутри этого дома.
// Снаружи надписи видны как раньше. Создаётся само при запуске, вешать никуда не надо.
public class HouseSignHider : MonoBehaviour
{
    TaskManager[] tasks;
    Transform head;
    float timer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        new GameObject("СкрытиеНадписей").AddComponent<HouseSignHider>();
    }

    void Start()
    {
        head = Player.instance.hmdTransform;
        tasks = FindObjectsByType<TaskManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = 0.1f;

        foreach (TaskManager tm in tasks)
        {
            if (tm == null || tm.taskText == null) continue;
            MeshRenderer r = tm.taskText.GetComponent<MeshRenderer>();
            if (r == null) continue;

            // Положение головы в координатах дома (так подходят и повёрнутые дома)
            Transform house = tm.taskText.transform.parent;
            Vector3 p = house != null ? house.InverseTransformPoint(head.position) : head.position;

            bool inside = p.x > -3.2f && p.x < 3.2f && p.z > 1.8f && p.z < 8.2f && p.y > -1f && p.y < 7f;
            r.enabled = !inside;
        }
    }
}
