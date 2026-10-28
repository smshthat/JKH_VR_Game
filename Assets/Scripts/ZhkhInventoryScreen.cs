using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Valve.VR.InteractionSystem;

// Экран инвентаря перед глазами (непрозрачный, с сеткой слотов).
// Открыть/закрыть: поднесите пустую руку к лицу и нажмите курок. В редакторе без шлема: клавиша I.
// Содержимое мешочка читается из вашего PouchInventory.cs без его изменения.
public class ZhkhInventoryScreen : MonoBehaviour
{
    public float openDistance = 0.35f;
    public float screenDistance = 1.05f;
    public float panelScale = 0.85f;
    public KeyCode editorKey = KeyCode.I;
    public float dwellTime = 1f;
    readonly float[] dwell = new float[2];
    readonly bool[] armed = new bool[] { true, true };

    const int Slots = 10;
    static readonly Color cFrame = new Color(0.95f, 0.72f, 0.2f);
    static readonly Color cBg = new Color(0.07f, 0.1f, 0.18f);
    static readonly Color cHeader = new Color(0.14f, 0.34f, 0.7f);
    static readonly Color cFooter = new Color(0.1f, 0.16f, 0.28f);
    static readonly Color cSlot = new Color(0.16f, 0.24f, 0.38f);
    static readonly Color cSlotNext = new Color(0.14f, 0.45f, 0.33f);
    static readonly Color cSlotEmpty = new Color(0.1f, 0.14f, 0.22f);
    static readonly Color cSlotOff = new Color(0.08f, 0.1f, 0.15f);
    static readonly Color cBadgeEmpty = new Color(0.22f, 0.27f, 0.36f);
    static readonly Color[] palette = new Color[]
    {
        new Color(0.9f, 0.4f, 0.3f), new Color(0.3f, 0.7f, 0.9f), new Color(0.95f, 0.75f, 0.25f),
        new Color(0.5f, 0.8f, 0.4f), new Color(0.75f, 0.5f, 0.9f), new Color(0.95f, 0.55f, 0.75f)
    };

    GameObject panel;
    TextMesh headerRight, footerLeft, footerRight, footerHint;
    GameObject[] slotBg = new GameObject[Slots];
    GameObject[] slotBadge = new GameObject[Slots];
    TextMesh[] slotName = new TextMesh[Slots];
    TextMesh[] slotNum = new TextMesh[Slots];

    bool open, keyBroken;
    Component pouch;
    float nextFind, nextRefresh, startTime;
    readonly List<string> names = new List<string>();
    readonly System.Text.StringBuilder sb = new System.Text.StringBuilder();
    GameObject resumeButton, quitButton, cursor;
    TextMesh resumeLabel, quitLabel, menuHint;
    bool confirmExit;
    float menuInputAfter;
    int menuHover = -1;
    Hand menuHand;

    void Start() { startTime = Time.time; }

    void Update()
    {
        Transform head = ZhkhVR.Head();
        if (head == null) return;
        if (Toggled(head))
        {
            open = !open;
            if (open) Open(head);
            else if (panel != null) panel.SetActive(false);
        }
        if (!open || panel == null) return;
        Follow(head, false);
        UpdateMenu(head);
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.3f;
            Refresh();
        }
    }

    bool Toggled(Transform head)
    {
        if (!keyBroken)
        {
            try { if (Input.GetKeyDown(editorKey)) return true; }
            catch (System.Exception) { keyBroken = true; }
        }
        if (!ZhkhVR.InputReady) return false;
        Player p = ZhkhVR.P;
        if (p == null || p.hands == null) return false;
        for (int i = 0; i < p.hands.Length && i < 2; i++)
        {
            Hand h = p.hands[i];
            if (h == null || !h.gameObject.activeInHierarchy) continue;
            bool near = h.currentAttachedObject == null
                && (h.transform.position - head.position).sqrMagnitude <= openDistance * openDistance;
            if (!near) { dwell[i] = 0f; armed[i] = true; continue; }
            // 1) курок рядом с лицом
            if (h.grabPinchAction != null && h.grabPinchAction.GetStateDown(h.handType)) { armed[i] = false; dwell[i] = 0f; return true; }
            // 2) рука у лица около секунды
            if (armed[i])
            {
                dwell[i] += Time.deltaTime;
                if (dwell[i] >= dwellTime) { armed[i] = false; dwell[i] = 0f; return true; }
            }
        }
        return false;
    }

    void Open(Transform head)
    {
        if (panel == null) Build();
        panel.SetActive(true);
        Follow(head, true);
        nextRefresh = 0f;
        confirmExit = false;
        menuInputAfter = Time.unscaledTime + 0.3f;
        UpdateMenuLabels();
    }

    void Build()
    {
        panel = new GameObject("ZhkhInventoryPanel");
        panel.transform.localScale = Vector3.one * panelScale;
        panel.AddComponent<ZhkhKeepVisible>();
        Transform t = panel.transform;

        ZhkhUI.Quad(t, new Vector3(0f, -0.1f, 0f), 0.96f, 1.06f, cFrame, 0, false);
        ZhkhUI.Quad(t, new Vector3(0f, -0.1f, -0.001f), 0.94f, 1.04f, cBg, 1, false);
        ZhkhUI.Quad(t, new Vector3(0f, 0.36f, -0.002f), 0.94f, 0.12f, cHeader, 2, false);
        ZhkhUI.Text(t, "ИНВЕНТАРЬ", new Vector3(-0.44f, 0.36f, -0.01f), 0.008f, Color.white, TextAnchor.MiddleLeft, 20);
        headerRight = ZhkhUI.Text(t, "Мешочек", new Vector3(0.44f, 0.36f, -0.01f), 0.0055f, new Color(0.85f, 0.93f, 1f), TextAnchor.MiddleRight, 20);

        float[] colX = new float[] { -0.235f, 0.235f };
        for (int i = 0; i < Slots; i++)
        {
            int col = i / 5, row = i % 5;
            float cx = colX[col];
            float cy = 0.23f - row * 0.095f;
            slotBg[i] = ZhkhUI.Quad(t, new Vector3(cx, cy, -0.003f), 0.45f, 0.085f, cSlotEmpty, 3, false);
            slotBadge[i] = ZhkhUI.Quad(t, new Vector3(cx - 0.18f, cy, -0.004f), 0.07f, 0.07f, cBadgeEmpty, 4, false);
            slotNum[i] = ZhkhUI.Text(t, (i + 1).ToString(), new Vector3(cx - 0.18f, cy, -0.01f), 0.0045f, Color.white, TextAnchor.MiddleCenter, 20);
            slotName[i] = ZhkhUI.Text(t, "", new Vector3(cx - 0.13f, cy, -0.01f), 0.004f, Color.white, TextAnchor.MiddleLeft, 20);
        }

        ZhkhUI.Quad(t, new Vector3(0f, -0.335f, -0.002f), 0.94f, 0.14f, cFooter, 2, false);
        footerLeft = ZhkhUI.Text(t, "", new Vector3(-0.44f, -0.31f, -0.01f), 0.0045f, new Color(0.7f, 0.9f, 1f), TextAnchor.MiddleLeft, 20);
        footerRight = ZhkhUI.Text(t, "", new Vector3(0.44f, -0.31f, -0.01f), 0.0045f, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleRight, 20);
        footerHint = ZhkhUI.Text(t, "Зелёный слот достанется первым. Закрыть: рука к лицу + курок", new Vector3(0f, -0.375f, -0.01f), 0.003f, new Color(0.65f, 0.7f, 0.8f), TextAnchor.MiddleCenter, 20);
        resumeButton = ZhkhUI.Quad(t, new Vector3(-0.235f, -0.48f, -0.003f), 0.43f, 0.10f, cHeader, 5, false);
        quitButton = ZhkhUI.Quad(t, new Vector3(0.235f, -0.48f, -0.003f), 0.43f, 0.10f, new Color(0.55f, 0.15f, 0.16f), 5, false);
        resumeLabel = ZhkhUI.Text(t, "ПРОДОЛЖИТЬ", new Vector3(-0.235f, -0.48f, -0.014f), 0.004f, Color.white, TextAnchor.MiddleCenter, 25);
        quitLabel = ZhkhUI.Text(t, "ВЫХОД ИЗ ИГРЫ", new Vector3(0.235f, -0.48f, -0.014f), 0.004f, Color.white, TextAnchor.MiddleCenter, 25);
        menuHint = ZhkhUI.Text(t, "Наведите контроллер или взгляд и нажмите курок", new Vector3(0, -0.575f, -0.014f), 0.003f, Color.white, TextAnchor.MiddleCenter, 25);
        cursor = ZhkhUI.Quad(t, Vector3.zero, 0.013f, 0.013f, Color.white, 30, false);
        cursor.SetActive(false);
    }

    void UpdateMenu(Transform head)
    {
        menuHover = -1; menuHand = null;
        Vector3 point = Vector3.zero;
        Player player = ZhkhVR.P;
        if (player != null && player.hands != null)
            foreach (Hand hand in player.hands)
            {
                if (hand == null || !hand.gameObject.activeInHierarchy || hand.currentAttachedObject != null) continue;
                int hit = MenuHit(new Ray(hand.transform.position, hand.transform.forward), out point);
                if (hit < 0) continue;
                menuHover = hit; menuHand = hand; break;
            }
        if (menuHover < 0) menuHover = MenuHit(new Ray(head.position, head.forward), out point);
        cursor.SetActive(menuHover >= 0);
        if (menuHover >= 0) cursor.transform.localPosition = new Vector3(point.x, point.y, -0.025f);
        ZhkhUI.SetColor(resumeButton, menuHover == 0 ? new Color(0.22f, 0.5f, 0.9f) : cHeader);
        ZhkhUI.SetColor(quitButton, menuHover == 1 ? new Color(0.78f, 0.24f, 0.25f) : new Color(0.55f, 0.15f, 0.16f));
        if (Time.unscaledTime < menuInputAfter || menuHover < 0) return;
        bool clicked = MenuPressed(menuHand);
        if (menuHand == null && player != null && player.hands != null)
            foreach (var hand in player.hands) clicked |= MenuPressed(hand);
        if (clicked) ActivateMenu(menuHover);
    }

    int MenuHit(Ray ray, out Vector3 local)
    {
        local = Vector3.zero;
        Plane plane = new Plane(panel.transform.forward, panel.transform.TransformPoint(new Vector3(0, 0, -0.014f)));
        float distance;
        if (!plane.Raycast(ray, out distance) || distance > 3f) return -1;
        local = panel.transform.InverseTransformPoint(ray.GetPoint(distance));
        if (Mathf.Abs(local.y + 0.48f) > 0.05f) return -1;
        if (Mathf.Abs(local.x + 0.235f) <= 0.215f) return 0;
        if (Mathf.Abs(local.x - 0.235f) <= 0.215f) return 1;
        return -1;
    }

    static bool MenuPressed(Hand hand)
    {
        return ZhkhVR.InputReady && hand != null && hand.gameObject.activeInHierarchy &&
            ((hand.uiInteractAction != null && hand.uiInteractAction.GetStateDown(hand.handType)) ||
             (hand.grabPinchAction != null && hand.grabPinchAction.GetStateDown(hand.handType)));
    }

    void ActivateMenu(int index)
    {
        menuInputAfter = Time.unscaledTime + 0.3f;
        if (index == 0)
        {
            if (confirmExit) { confirmExit = false; UpdateMenuLabels(); }
            else { open = false; panel.SetActive(false); }
            return;
        }
        if (!confirmExit) { confirmExit = true; UpdateMenuLabels(); return; }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void UpdateMenuLabels()
    {
        if (quitLabel == null) return;
        resumeLabel.text = confirmExit ? "ОТМЕНА" : "ПРОДОЛЖИТЬ";
        quitLabel.text = confirmExit ? "ДА, ВЫЙТИ" : "ВЫХОД ИЗ ИГРЫ";
        menuHint.text = confirmExit ? "Выйти? Текущая смена не сохраняется." : "Наведите контроллер или взгляд и нажмите курок";
    }

    void Follow(Transform head, bool snap)
    {
        Vector3 f = head.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.01f) f = head.forward;
        f.Normalize();
        Vector3 target = head.position + f * screenDistance + Vector3.down * 0.05f;
        Transform t = panel.transform;
        if (snap || (t.position - target).sqrMagnitude > 4f) t.position = target;
        else t.position = Vector3.Lerp(t.position, target, Mathf.Clamp01(Time.deltaTime * 6f));
        Vector3 dir = t.position - head.position;
        if (dir.sqrMagnitude > 0.0001f) t.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    void Refresh()
    {
        int capacity;
        int state = ReadPouch(out capacity); // 0 нет мешочка, 1 не прочитать, 2 ок
        if (capacity > Slots) capacity = Slots;

        headerRight.text = state == 2 ? "Мешочек  " + names.Count + " / " + capacity : "Мешочек  ?";

        for (int i = 0; i < Slots; i++)
        {
            bool enabledSlot = i < capacity;
            bool filled = state == 2 && i < names.Count;
            if (filled)
            {
                string n = names[i];
                ZhkhUI.SetColor(slotBg[i], i == 0 ? cSlotNext : cSlot);
                ZhkhUI.SetColor(slotBadge[i], palette[(n.Length > 0 ? (n[0] + n.Length) : 0) % palette.Length]);
                slotName[i].text = n;
                slotName[i].color = Color.white;
            }
            else
            {
                ZhkhUI.SetColor(slotBg[i], enabledSlot ? cSlotEmpty : cSlotOff);
                ZhkhUI.SetColor(slotBadge[i], cBadgeEmpty);
                slotName[i].text = enabledSlot ? "пусто" : "";
                slotName[i].color = new Color(0.45f, 0.5f, 0.6f);
            }
        }

        footerLeft.text = "Починено протечек: " + ZhkhLeakSpawner.FixedCount;
        int sec = Mathf.FloorToInt(Time.time - startTime);
        sb.Length = 0;
        sb.Append("Смена: ").Append(sec / 60).Append(':').Append((sec % 60).ToString("00"));
        footerRight.text = sb.ToString();
        footerHint.text = state == 2 ? "Зелёный слот достанется первым. Закрыть: рука к лицу + курок"
            : (state == 0 ? "Мешочек не найден в сцене" : "Содержимое мешочка не прочиталось");
    }

    int ReadPouch(out int capacity)
    {
        capacity = 10; names.Clear();
        if (PouchInventory.Instance != null)
        {
            var inventory = PouchInventory.Instance;
            capacity = inventory.capacity;
            for (int i = 0; i < inventory.StoredCount; i++) names.Add(Clean(inventory.ItemName(i)));
            return 2;
        }
        if (pouch == null && Time.unscaledTime >= nextFind)
        {
            nextFind = Time.unscaledTime + 5f;
            pouch = FindPouch();
        }
        if (pouch == null) return 0;

        List<string> best = null;
        bool bestFirst = false;
        FieldInfo[] fields = pouch.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo f = fields[i];
            object v = null;
            try { v = f.GetValue(pouch); } catch (System.Exception) { continue; }
            if (v == null) continue;
            if (f.FieldType == typeof(int))
            {
                string n = f.Name.ToLowerInvariant();
                if (n.Contains("capacity") || n.Contains("max"))
                {
                    int c = (int)v;
                    if (c > 0 && c <= 100) capacity = c;
                }
                continue;
            }
            if (v is string || v is Object) continue;
            System.Collections.IEnumerable en = v as System.Collections.IEnumerable;
            if (en == null) continue;
            List<string> list = new List<string>();
            foreach (object e in en)
            {
                string s = Describe(e);
                if (s != null) list.Add(s);
            }
            if (best == null || list.Count > best.Count)
            {
                best = list;
                string tn = f.FieldType.Name;
                bestFirst = tn.StartsWith("Stack") || tn.StartsWith("Queue");
            }
        }

        if (best == null || best.Count == 0)
        {
            Transform t = pouch.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform c = t.GetChild(i);
                if (!c.gameObject.activeSelf && c.GetComponent<TextMesh>() == null)
                {
                    if (best == null) best = new List<string>();
                    best.Add(Clean(c.name));
                    bestFirst = false;
                }
            }
        }
        if (best == null) return 1;
        if (!bestFirst) best.Reverse();
        names.AddRange(best);
        return 2;
    }

    Component FindPouch()
    {
        MonoBehaviour[] all;
#if UNITY_2023_1_OR_NEWER
        all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        all = FindObjectsOfType<MonoBehaviour>(true);
#endif
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].GetType().Name == "PouchInventory") return all[i];
        return null;
    }

    static string Describe(object e)
    {
        if (e == null) return null;
        if (e is Object)
        {
            Object uo = (Object)e;
            if (uo == null) return null;
            GameObject g = uo as GameObject;
            if (g != null) return Clean(g.name);
            Component c = uo as Component;
            if (c != null) return Clean(c.gameObject.name);
            return Clean(uo.name);
        }
        string s = e as string;
        if (s != null) return Clean(s);
        FieldInfo[] fs = e.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < fs.Length; i++)
        {
            object fv = null;
            try { fv = fs[i].GetValue(e); } catch (System.Exception) { }
            GameObject g2 = fv as GameObject;
            if (g2 != null) return Clean(g2.name);
            Component c2 = fv as Component;
            if (c2 != null) return Clean(c2.gameObject.name);
        }
        return Clean(e.ToString());
    }

    static string Clean(string s)
    {
        if (s == null) return "?";
        s = s.Replace("(Clone)", "").Trim();
        if (s.Length > 22) s = s.Substring(0, 21) + "…";
        return s;
    }
}
