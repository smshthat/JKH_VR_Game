using UnityEngine;
using Valve.VR.InteractionSystem;

// Зона задачи: нужно принести предмет и отпустить рядом.
// Примеры: лампочка к лампе, мешок в мусорный бак, квитанция в почтовый ящик.
// Вешается на пустой объект или на место назначения.
public class TaskZone : MonoBehaviour
{
    public Transform[] accepted;        // какие предметы подходят для этой задачи
    public float radius = 0.3f;         // насколько близко надо отпустить предмет
    public GameObject enableOnDone;     // что включить, когда задача выполнена (например, свет лампы)
    public bool hideItemOnDone = true;  // убрать предмет, когда он доставлен

    public bool done;                   // задача выполнена
    public bool useItemBounds;          // модели с pivot у пола, например мешок

    Interactable[] interactables;

    void Start()
    {
        if (accepted == null) accepted = new Transform[0];
        interactables = new Interactable[accepted.Length];
        for (int i = 0; i < accepted.Length; i++)
            if (accepted[i] != null) interactables[i] = accepted[i].GetComponent<Interactable>();

        if (enableOnDone != null) enableOnDone.SetActive(false);
    }

    void Update()
    {
        if (done) return;

        for (int i = 0; i < accepted.Length; i++)
        {
            Transform item = accepted[i];
            if (item == null || !item.gameObject.activeInHierarchy) continue;       // убран или в мешочке
            if (interactables[i] != null && interactables[i].attachedToHand != null) continue;  // ещё в руке

            Vector3 nearest = item.position;
            if (useItemBounds)
            {
                Collider col = item.GetComponent<Collider>();
                if (col != null) nearest = col.ClosestPoint(transform.position);
            }
            if (Vector3.Distance(nearest, transform.position) < radius)
            {
                done = true;
                if (hideItemOnDone) item.gameObject.SetActive(false);
                if (enableOnDone != null) enableOnDone.SetActive(true);
                return;
            }
        }
    }
}
