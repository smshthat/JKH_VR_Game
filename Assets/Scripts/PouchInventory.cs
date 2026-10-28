using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

// Инвентарь-мешочек. Создаётся сам при запуске игры, вешать никуда не надо.
// Мешочек висит у пояса справа и следует за игроком.
//  - Положить: отпустите предмет над мешочком, он исчезнет внутри.
//  - Достать: поднесите пустую руку к мешочку и нажмите грип/триггер, последний предмет окажется в руке.
public class PouchInventory : MonoBehaviour
{
    public static PouchInventory Instance;

    public Vector3 offset = new Vector3(0.3f, -0.6f, 0.05f);  // где мешочек относительно головы
    public float radius = 0.22f;                              // размер зоны мешочка
    public int capacity = 10;                                 // сколько предметов влезает

    readonly List<GameObject> stored = new List<GameObject>();
    Transform head;
    Transform bag;
    TextMesh label;
    Hand[] hands;
    float scanTimer;
    Throwable[] nearbyCandidates;
    float nextCandidateRefresh;

    public int StoredCount { get { return stored.Count; } }
    public string ItemName(int index)
    {
        int i = stored.Count - 1 - index;
        return i >= 0 && i < stored.Count && stored[i] != null ? stored[i].name : "";
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        new GameObject("Мешочек").AddComponent<PouchInventory>();
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        head = Player.instance.hmdTransform;
        hands = Player.instance.hands;

        // Внешний вид мешочка: коричневый шарик с завязкой
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(body.GetComponent<Collider>());
        body.name = "Мешочек_тело";
        body.transform.localScale = new Vector3(0.24f, 0.2f, 0.2f);
        body.GetComponent<Renderer>().material.color = new Color(0.5f, 0.32f, 0.16f);
        bag = body.transform;
        bag.SetParent(transform);

        GameObject tie = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(tie.GetComponent<Collider>());
        tie.name = "Завязка";
        tie.transform.SetParent(bag, false);
        tie.transform.localPosition = new Vector3(0, 0.5f, 0);
        tie.transform.localScale = new Vector3(0.5f, 0.15f, 0.5f);
        tie.GetComponent<Renderer>().material.color = new Color(0.3f, 0.18f, 0.08f);

        // Надпись со счётчиком предметов
        GameObject txt = new GameObject("Счётчик");
        txt.transform.SetParent(transform);
        label = txt.AddComponent<TextMesh>();
        Font font = Font.CreateDynamicFontFromOSFont("Arial", 64);
        label.font = font;
        txt.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        label.fontSize = 100;
        label.characterSize = 0.012f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = Color.white;

        bag.position = head.position;
        UpdateLabel();
    }

    void Update()
    {
        FollowBody();
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = 0.2f;
            StoreNearbyItems();
        }
        TakeOutWithHand();

        // Счётчик над мешочком, повёрнут к игроку
        label.transform.position = bag.position + Vector3.up * 0.2f;
        Vector3 away = label.transform.position - head.position;
        if (away.sqrMagnitude > 0.001f) label.transform.rotation = Quaternion.LookRotation(away);
    }

    // Мешочек следует за телом игрока (поворот только по горизонтали)
    void FollowBody()
    {
        Vector3 fwd = head.forward;
        fwd.y = 0;
        if (fwd.sqrMagnitude < 0.01f) return;
        Quaternion yaw = Quaternion.LookRotation(fwd.normalized);
        Vector3 target = head.position + yaw * offset;
        bag.position = Vector3.Lerp(bag.position, target, 8f * Time.deltaTime);
        bag.rotation = Quaternion.Slerp(bag.rotation, yaw, 8f * Time.deltaTime);
    }

    // Предмет, отпущенный рядом с мешочком, прячется внутрь
    void StoreNearbyItems()
    {
        if (stored.Count >= capacity) return;
        if (nearbyCandidates == null || Time.unscaledTime >= nextCandidateRefresh)
        {
            nearbyCandidates = FindObjectsByType<Throwable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            nextCandidateRefresh = Time.unscaledTime + 3f;
        }
        foreach (Throwable t in nearbyCandidates)
        {
            if (stored.Count >= capacity) break;
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            Interactable inter = t.GetComponent<Interactable>();
            if (inter != null && inter.attachedToHand != null) continue;   // в руке: не трогаем
            if (Vector3.Distance(t.transform.position, bag.position) < radius)
            {
                t.gameObject.SetActive(false);
                stored.Add(t.gameObject);
                UpdateLabel();
            }
        }
    }

    // Пустая рука у мешочка + грип/триггер -> достаём последний предмет
    void TakeOutWithHand()
    {
        if (stored.Count == 0) return;
        foreach (Hand h in hands)
        {
            if (h == null || !h.gameObject.activeInHierarchy || !ZhkhVR.InputReady) continue;
            if (h.currentAttachedObject != null) continue;
            if (Vector3.Distance(h.transform.position, bag.position) > radius * 1.2f) continue;

            GrabTypes grab = h.GetGrabStarting();
            if (grab == GrabTypes.None) continue;

            GameObject item = stored[stored.Count - 1];
            stored.RemoveAt(stored.Count - 1);
            item.transform.position = h.transform.position;
            item.SetActive(true);
            h.AttachObject(item, grab);
            UpdateLabel();
            return;
        }
    }

    void UpdateLabel()
    {
        label.text = "Мешочек: " + stored.Count;
    }
}
