using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR.InteractionSystem;
using Hand = Valve.VR.InteractionSystem.Hand;

// Экран инвентаря: мини-экран перед глазами, на котором видны предметы из мешочка.
// Создаётся сам при запуске игры, вешать никуда не надо. Мешочек (PouchInventory) не меняется.
//  - Открыть/закрыть: кнопка меню на контроллере Vive
//    ИЛИ поднесите любую руку к лицу и подержите около секунды.
//  - Достать предмет: как раньше, поднесите руку к мешочку у пояса и нажмите грип/курок.
public class InventoryScreen : MonoBehaviour
{
    public float distance = 0.7f;       // на каком расстоянии перед глазами висит экран
    public int columns = 5;             // сколько предметов в ряду
    public int maxSlots = 10;           // сколько предметов показывать

    Transform head;
    Hand[] hands;
    Transform panel;
    TextMesh countText;
    FieldInfo storedField;
    readonly List<GameObject> shown = new List<GameObject>();
    readonly List<Transform> icons = new List<Transform>();

    bool visible;
    bool menuPrev;
    float faceHold;
    bool faceLatch;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        new GameObject("ЭкранИнвентаря").AddComponent<InventoryScreen>();
    }

    void Start()
    {
        head = Player.instance.hmdTransform;
        hands = Player.instance.hands;
        storedField = typeof(PouchInventory).GetField("stored", BindingFlags.NonPublic | BindingFlags.Instance);
        BuildPanel();
        panel.gameObject.SetActive(false);
    }

    void BuildPanel()
    {
        panel = new GameObject("Панель").transform;
        panel.SetParent(transform);

        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(bg.GetComponent<Collider>());
        bg.name = "Фон";
        bg.transform.SetParent(panel, false);
        bg.transform.localScale = new Vector3(0.8f, 0.5f, 1f);
        Material m = new Material(Shader.Find("Sprites/Default"));
        m.color = new Color(0.05f, 0.07f, 0.12f, 0.88f);
        bg.GetComponent<Renderer>().material = m;

        MakeText("Заголовок", "ИНВЕНТАРЬ", new Vector3(-0.3f, 0.21f, -0.005f), 0.004f, TextAnchor.MiddleLeft, Color.white);
        countText = MakeText("Счётчик", "", new Vector3(0.38f, 0.21f, -0.005f), 0.003f, TextAnchor.MiddleRight, new Color(0.8f, 0.9f, 1f));
        MakeText("Подсказка", "Достать: рука к мешочку у пояса + грип", new Vector3(0, -0.22f, -0.005f), 0.0025f, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
    }

    TextMesh MakeText(string name, string text, Vector3 localPos, float charSize, TextAnchor anchor, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(panel, false);
        go.transform.localPosition = localPos;
        TextMesh tm = go.AddComponent<TextMesh>();
        Font f = SimpleSound.TextFont;
        tm.font = f;
        go.GetComponent<MeshRenderer>().sharedMaterial = f.material;
        tm.fontSize = 100;
        tm.characterSize = charSize;
        tm.anchor = anchor;
        tm.color = color;
        tm.text = text;
        return tm;
    }

    void Update()
    {
        if (head == null) return;

        // Кнопка меню на любом контроллере
        bool menuNow = MenuPressed(XRNode.LeftHand) || MenuPressed(XRNode.RightHand);
        if (menuNow && !menuPrev) Toggle();
        menuPrev = menuNow;

        // Жест: рука у лица
        float minDist = 10f;
        for (int i = 0; i < hands.Length; i++)
        {
            float d = Vector3.Distance(hands[i].transform.position, head.position);
            if (d < minDist) minDist = d;
        }
        bool near = minDist < 0.25f;
        if (near && !faceLatch)
        {
            faceHold += Time.deltaTime;
            if (faceHold > 0.9f) { faceLatch = true; faceHold = 0f; Toggle(); }
        }
        else if (!near)
        {
            faceHold = 0f;
            if (minDist > 0.4f) faceLatch = false;
        }

        if (!visible) return;

        // Экран плавно следует за взглядом
        Vector3 target = head.position + head.forward * distance + Vector3.down * 0.03f;
        panel.position = Vector3.Lerp(panel.position, target, 8f * Time.deltaTime);
        Vector3 away = panel.position - head.position;
        if (away.sqrMagnitude > 0.001f) panel.rotation = Quaternion.LookRotation(away);

        RefreshIfChanged();

        // Предметы медленно вращаются
        for (int i = 0; i < icons.Count; i++)
            if (icons[i] != null) icons[i].localRotation = Quaternion.Euler(20f, Time.time * 40f, 0f);
    }

    bool MenuPressed(XRNode node)
    {
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        bool value;
        return device.isValid && device.TryGetFeatureValue(CommonUsages.menuButton, out value) && value;
    }

    void Toggle()
    {
        visible = !visible;
        panel.gameObject.SetActive(visible);
        if (visible)
        {
            panel.position = head.position + head.forward * distance;
            shown.Clear();
            List<GameObject> stored = GetStored();
            if (stored != null) shown.AddRange(stored);
            Rebuild();
        }
    }

    // Список предметов в мешочке (берём из старого скрипта, не меняя его)
    List<GameObject> GetStored()
    {
        if (PouchInventory.Instance == null || storedField == null) return null;
        return storedField.GetValue(PouchInventory.Instance) as List<GameObject>;
    }

    void RefreshIfChanged()
    {
        List<GameObject> stored = GetStored();
        if (stored == null) return;

        bool changed = stored.Count != shown.Count;
        if (!changed)
            for (int i = 0; i < stored.Count; i++)
                if (stored[i] != shown[i]) { changed = true; break; }
        if (!changed) return;

        shown.Clear();
        shown.AddRange(stored);
        Rebuild();
    }

    // Рисуем значки предметов и подписи
    void Rebuild()
    {
        for (int i = panel.childCount - 1; i >= 0; i--)
            if (panel.GetChild(i).name == "Слот") Destroy(panel.GetChild(i).gameObject);
        icons.Clear();

        countText.text = shown.Count + " шт.";
        int count = Mathf.Min(shown.Count, maxSlots);
        for (int i = 0; i < count; i++)
        {
            int col = i % columns;
            int row = i / columns;
            float x = (col - (columns - 1) / 2f) * 0.14f;
            float y = 0.07f - row * 0.18f;

            GameObject slot = new GameObject("Слот");
            slot.transform.SetParent(panel, false);
            slot.transform.localPosition = new Vector3(x, y, -0.01f);

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(frame.GetComponent<Collider>());
            frame.transform.SetParent(slot.transform, false);
            frame.transform.localPosition = new Vector3(0, 0, 0.004f);
            frame.transform.localScale = new Vector3(0.125f, 0.15f, 1f);
            Material fm = new Material(Shader.Find("Sprites/Default"));
            fm.color = new Color(0.15f, 0.2f, 0.3f, 0.9f);
            frame.GetComponent<Renderer>().material = fm;

            MakeIcon(slot.transform, shown[i]);

            GameObject label = new GameObject("Подпись");
            label.transform.SetParent(slot.transform, false);
            label.transform.localPosition = new Vector3(0, -0.058f, -0.002f);
            TextMesh tm = label.AddComponent<TextMesh>();
            Font f = SimpleSound.TextFont;
            tm.font = f;
            label.GetComponent<MeshRenderer>().sharedMaterial = f.material;
            tm.fontSize = 100;
            tm.characterSize = 0.0022f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.white;
            tm.text = CleanName(shown[i].name);
        }
    }

    // Уменьшенная копия модели предмета (только форма и цвет, без физики)
    void MakeIcon(Transform slot, GameObject item)
    {
        MeshFilter mf = item.GetComponentInChildren<MeshFilter>(true);
        if (mf == null || mf.sharedMesh == null) return;
        MeshRenderer mr = mf.GetComponent<MeshRenderer>();
        if (mr == null) return;

        Vector3 scale = mf.transform.lossyScale;
        Vector3 size = Vector3.Scale(mf.sharedMesh.bounds.size, scale);
        float biggest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        if (biggest < 0.0001f) return;
        float k = 0.07f / biggest;

        GameObject icon = new GameObject("Значок");
        icon.transform.SetParent(slot, false);
        icon.transform.localPosition = new Vector3(0, 0.018f, -0.03f);
        icon.transform.localScale = scale * k;
        icon.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
        icon.AddComponent<MeshRenderer>().sharedMaterial = mr.sharedMaterial;
        icons.Add(icon.transform);
    }

    // "Купюра (Дом 3)" -> "Купюра"
    string CleanName(string name)
    {
        int p = name.IndexOf(" (");
        if (p > 0) name = name.Substring(0, p);
        name = name.Replace('_', ' ');
        if (name.Length > 11) name = name.Substring(0, 10) + "…";
        return name;
    }
}
