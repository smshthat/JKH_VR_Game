using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

// Диалоги с жильцами. Подойдите к NPC ближе 2 м и посмотрите на него: появится панель с репликами.
// Выбор: посмотрите на ответ (он подсветится) и нажмите курок. В редакторе: клавиши 1-3.
// Купюра у NPC не берётся рукой, пока вы не убедите его заплатить.
public class ZhkhDialogue : MonoBehaviour
{
    public float talkDistance = 2.2f;
    public float closeDistance = 4.5f;
    public float lookAngle = 50f;
    public float lookTime = 0.8f;
    public int needTrust = 2;
    public float failCooldown = 40f;

    class Opt { public string text; public int next; public int trust; public Opt(string t, int n, int tr) { text = t; next = n; trust = tr; } }
    class Node { public string line; public int action; public Opt[] opts; public Node(string l, int a, params Opt[] o) { line = l; action = a; opts = o; } }
    class Bill { public GameObject go; public List<Collider> cols = new List<Collider>(); public bool locked; }
    class Npc
    {
        public Transform root; public NPCFightBack fight; public NPCController ctrl; public List<Bill> bills = new List<Bill>();
        public int trust; public float cooldown; public bool paid; public float look; public int variant;
    }

    static readonly Color cFrame = new Color(0.95f, 0.72f, 0.2f);
    static readonly Color cBg = new Color(0.07f, 0.1f, 0.18f);
    static readonly Color cHeader = new Color(0.14f, 0.34f, 0.7f);
    static readonly Color cSpeech = new Color(0.12f, 0.17f, 0.28f);
    static readonly Color cBtn = new Color(0.16f, 0.25f, 0.42f);
    static readonly Color cBtnHover = new Color(0.2f, 0.55f, 0.9f);

    readonly List<Npc> npcs = new List<Npc>();
    readonly HashSet<Transform> known = new HashSet<Transform>();
    static readonly List<Collider> tmpCols = new List<Collider>();
    static readonly RaycastHit[] hits = new RaycastHit[24];
    float nextScan, nextCheck, nextMaintain, inputBlock;
    bool keyBroken;

    Npc cur;
    Node[] nodes;
    int nodeIdx, hover = -1;
    GameObject panel;
    TextMesh nameText, lineText;
    TextMesh[] optText = new TextMesh[3];
    GameObject[] optRoot = new GameObject[3];
    GameObject[] optBg = new GameObject[3];
    int optCount;

    void Update()
    {
        Transform head = ZhkhVR.Head();
        if (head == null || Time.time < 2f) return;
        float now = Time.unscaledTime;

        if (now >= nextScan) { nextScan = now + (npcs.Count == 0 ? 5f : 30f); Scan(); }
        if (now >= nextMaintain) { nextMaintain = now + 0.25f; Maintain(); }

        if (cur != null) { UpdateDialogue(head); return; }
        if (now < nextCheck) return;
        nextCheck = now + 0.2f;
        CheckStart(head);
    }

    // ---------- поиск NPC и блокировка купюр ----------

    void Scan()
    {
        NPCController[] all;
#if UNITY_2023_1_OR_NEWER
        all = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
#else
        all = FindObjectsOfType<NPCController>();
#endif
        for (int i = 0; i < all.Length; i++)
        {
            NPCController controller = all[i];
            if (controller == null) continue;
            Transform r = controller.transform;
            if (!known.Add(r)) continue;
            Npc n = new Npc();
            n.root = r;
            n.fight = controller.GetComponent<NPCFightBack>();
            n.ctrl = controller;
            n.variant = Mathf.Abs(r.gameObject.GetInstanceID()) % 3;
            npcs.Add(n);
        }
        for (int i = 0; i < npcs.Count; i++)
            if (npcs[i].root != null && !npcs[i].paid) LockBills(npcs[i]);
    }

    static bool Blocked(Npc n)
    {
        return n.fight != null && (n.fight.angry || n.fight.knockedOut || n.fight.dead);
    }

    static bool MoneyTaken(Npc n)
    {
        return n.ctrl != null && n.ctrl.money != null && n.ctrl.money.wasTaken;
    }

    // Купюра NPC (ctrl.money): пока NPC не убеждён, отключаем её коллайдеры, и рука не может её схватить.
    void LockBills(Npc n)
    {
        if (n.ctrl == null || n.ctrl.money == null) return;
        if (n.ctrl.money.wasTaken) { n.paid = true; return; }
        GameObject go = n.ctrl.money.gameObject;
        for (int k = 0; k < n.bills.Count; k++) if (n.bills[k].go == go) return;
        Bill b = new Bill();
        b.go = go;
        b.locked = true;
        n.bills.Add(b);
        DisableCols(b);
    }

    static void DisableCols(Bill b)
    {
        b.go.GetComponentsInChildren<Collider>(true, tmpCols);
        for (int i = 0; i < tmpCols.Count; i++)
        {
            Collider c = tmpCols[i];
            if (c != null && c.enabled) { c.enabled = false; if (!b.cols.Contains(c)) b.cols.Add(c); }
        }
    }

    static void Unlock(Bill b)
    {
        for (int i = 0; i < b.cols.Count; i++) if (b.cols[i] != null) b.cols[i].enabled = true;
        b.cols.Clear();
        b.locked = false;
    }

    // Раз в четверть секунды: держим блокировку; если NPC в нокауте/мёртв или купюра выпала, разблокируем,
    // чтобы купюра не провалилась сквозь пол и её можно было подобрать.
    void Maintain()
    {
        for (int i = 0; i < npcs.Count; i++)
        {
            Npc n = npcs[i];
            if (n.root == null) continue;
            if (!n.paid && MoneyTaken(n)) n.paid = true;
            for (int k = n.bills.Count - 1; k >= 0; k--)
            {
                Bill b = n.bills[k];
                if (b.go == null) { n.bills.RemoveAt(k); continue; }
                if (!b.locked) continue;
                bool dropped = false;
                if (n.fight != null && (n.fight.dead || n.fight.knockedOut)) dropped = true;
                else
                {
                    Rigidbody rb = b.go.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic && rb.useGravity && !b.go.transform.IsChildOf(n.root)) dropped = true;
                }
                if (dropped) Unlock(b);
                else DisableCols(b);
            }
        }
    }

    // ---------- начало диалога ----------

    void CheckStart(Transform head)
    {
        for (int i = 0; i < npcs.Count; i++)
        {
            Npc n = npcs[i];
            if (n.root == null || n.paid || MoneyTaken(n)) continue;
            if (Time.time < n.cooldown) { n.look = 0f; continue; }
            Vector3 d = n.root.position - head.position;
            float dy = Mathf.Abs(d.y);
            d.y = 0f;
            if (d.magnitude > talkDistance || dy > 2.5f || n.root.up.y < 0.7f || Blocked(n)) { n.look = 0f; continue; }
            Vector3 hf = head.forward; hf.y = 0f;
            if (Vector3.Angle(hf, d) > lookAngle) { n.look = 0f; continue; }
            n.look += 0.2f;
            if (n.look >= lookTime) { StartDialogue(n, head); return; }
        }
    }

    void StartDialogue(Npc n, Transform head)
    {
        cur = n;
        n.trust = 0;
        n.look = 0f;
        nodes = BuildNodes(n.variant);
        if (panel == null) BuildPanel();
        nameText.text = CleanName(n.root.name);
        ZhkhNpcAnimator an = n.root.GetComponent<ZhkhNpcAnimator>();
        if (an != null) an.talking = true;

        Vector3 dir = n.root.position - head.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = head.forward;
        dir.Normalize();
        Vector3 pos = head.position + dir * 0.85f;
        pos.y = head.position.y - 0.1f;
        panel.transform.position = pos;
        panel.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z), Vector3.up);
        panel.SetActive(true);
        inputBlock = Time.unscaledTime + 0.6f;
        ShowNode(0);
    }

    static string CleanName(string s)
    {
        s = s.Replace("(Clone)", "").Replace("_", " ").Trim();
        return s.Length > 28 ? s.Substring(0, 28) : s;
    }

    // ---------- панель ----------

    void BuildPanel()
    {
        panel = new GameObject("ZhkhDialoguePanel");
        panel.AddComponent<ZhkhKeepVisible>();
        Transform t = panel.transform;
        ZhkhUI.Quad(t, new Vector3(0f, 0f, 0f), 1.0f, 0.78f, cFrame, 0, false);
        ZhkhUI.Quad(t, new Vector3(0f, 0f, -0.001f), 0.98f, 0.76f, cBg, 1, false);
        ZhkhUI.Quad(t, new Vector3(0f, 0.33f, -0.002f), 0.94f, 0.1f, cHeader, 2, false);
        nameText = ZhkhUI.Text(t, "", new Vector3(-0.44f, 0.33f, -0.01f), 0.0065f, Color.white, TextAnchor.MiddleLeft, 20);
        ZhkhUI.Quad(t, new Vector3(0f, 0.16f, -0.002f), 0.94f, 0.2f, cSpeech, 2, false);
        lineText = ZhkhUI.Text(t, "", new Vector3(-0.44f, 0.25f, -0.01f), 0.0045f, Color.white, TextAnchor.UpperLeft, 20);

        for (int i = 0; i < 3; i++)
        {
            float y = -0.04f - i * 0.13f;
            GameObject root = new GameObject("Opt" + i);
            root.transform.SetParent(t, false);
            optRoot[i] = root;
            GameObject bg = ZhkhUI.Quad(root.transform, new Vector3(0f, y, -0.003f), 0.94f, 0.115f, cBtn, 3, true);
            ZhkhDialogueButton btn = bg.AddComponent<ZhkhDialogueButton>();
            btn.index = i;
            optBg[i] = bg;
            ZhkhUI.Quad(root.transform, new Vector3(-0.41f, y, -0.004f), 0.07f, 0.07f, cFrame, 4, false);
            ZhkhUI.Text(root.transform, (i + 1).ToString(), new Vector3(-0.41f, y, -0.01f), 0.0055f, new Color(0.1f, 0.1f, 0.15f), TextAnchor.MiddleCenter, 20);
            optText[i] = ZhkhUI.Text(root.transform, "", new Vector3(-0.355f, y, -0.01f), 0.0042f, Color.white, TextAnchor.MiddleLeft, 20);
        }
        ZhkhUI.Text(t, "Посмотрите на ответ и нажмите курок", new Vector3(0f, -0.372f, -0.01f), 0.003f, new Color(0.65f, 0.7f, 0.8f), TextAnchor.MiddleCenter, 20);
    }

    void ShowNode(int idx)
    {
        nodeIdx = idx;
        Node nd = nodes[idx];
        if (nd.action == 1) GiveMoney(cur);
        else if (nd.action == 2) { cur.cooldown = Time.time + failCooldown; cur.trust = 0; }
        else if (nd.action == 3) cur.trust = 0;

        lineText.text = ZhkhUI.Wrap(nd.line, 46);
        optCount = nd.opts.Length;
        for (int i = 0; i < 3; i++)
        {
            bool on = i < optCount;
            optRoot[i].SetActive(on);
            if (on) optText[i].text = ZhkhUI.Wrap(nd.opts[i].text, 52);
        }
        hover = -1;
        for (int i = 0; i < optCount; i++) ZhkhUI.SetColor(optBg[i], cBtn);
    }

    static void GiveMoney(Npc n)
    {
        n.paid = true;
        ZhkhHandRig rg = n.root != null ? n.root.GetComponent<ZhkhHandRig>() : null;
        if (rg != null) rg.give = true;
        for (int i = 0; i < n.bills.Count; i++)
            if (n.bills[i].go != null && n.bills[i].locked) Unlock(n.bills[i]);
    }

    // ---------- ход диалога ----------

    void UpdateDialogue(Transform head)
    {
        if (cur.root == null) { Close(); return; }
        Vector3 d = cur.root.position - head.position; d.y = 0f;
        if (d.magnitude > closeDistance || (Blocked(cur) && !cur.paid)) { Close(); return; }

        int h = FindHover(head);
        if (h != hover)
        {
            if (hover >= 0 && hover < optCount) ZhkhUI.SetColor(optBg[hover], cBtn);
            hover = h;
            if (hover >= 0 && hover < optCount) ZhkhUI.SetColor(optBg[hover], cBtnHover);
        }
        if (Time.unscaledTime < inputBlock) return;

        int pick = -1;
        if (hover >= 0 && TriggerDown()) pick = hover;
        if (!keyBroken)
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) pick = 0;
                else if (Input.GetKeyDown(KeyCode.Alpha2)) pick = 1;
                else if (Input.GetKeyDown(KeyCode.Alpha3)) pick = 2;
            }
            catch (System.Exception) { keyBroken = true; }
        }
        if (pick >= 0 && pick < optCount) Choose(pick);
    }

    int FindHover(Transform head)
    {
        Player p = ZhkhVR.P;
        int n = Physics.RaycastNonAlloc(head.position, head.forward, hits, 6f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        int idx = -1;
        for (int i = 0; i < n; i++)
        {
            Collider c = hits[i].collider;
            if (c == null) continue;
            if (p != null && c.transform.IsChildOf(p.transform)) continue;
            if (hits[i].distance >= best) continue;
            best = hits[i].distance;
            ZhkhDialogueButton b = c.GetComponent<ZhkhDialogueButton>();
            idx = b != null ? b.index : -1;
        }
        return idx;
    }

    static bool TriggerDown()
    {
        if (!ZhkhVR.InputReady) return false;
        Player p = ZhkhVR.P;
        if (p == null || p.hands == null) return false;
        for (int i = 0; i < p.hands.Length; i++)
        {
            Hand h = p.hands[i];
            if (h != null && h.gameObject.activeInHierarchy && h.grabPinchAction != null && h.grabPinchAction.GetStateDown(h.handType)) return true;
        }
        return false;
    }

    void Choose(int pick)
    {
        Opt o = nodes[nodeIdx].opts[pick];
        cur.trust += o.trust;
        int next = o.next;
        if (next == -2) next = cur.trust >= needTrust ? 4 : 6;
        inputBlock = Time.unscaledTime + 0.3f;
        if (next == -1) { Close(); return; }
        ShowNode(next);
    }

    void Close()
    {
        if (cur != null)
        {
            if (!cur.paid && cur.cooldown < Time.time + 8f) cur.cooldown = Time.time + 8f;
            cur.trust = 0;
            cur.look = 0f;
        }
        if (cur != null)
        {
            ZhkhNpcAnimator an = cur.root != null ? cur.root.GetComponent<ZhkhNpcAnimator>() : null;
            if (an != null) an.talking = false;
        }
        cur = null;
        if (panel != null) panel.SetActive(false);
    }

    // ---------- реплики ----------

    Node[] BuildNodes(int v)
    {
        string[] complaint = new string[]
        {
            "Платить? А за что? Горячая вода еле тёплая, а в подъезде вечно темно.",
            "Опять платёжка выросла! Объясните, из чего складывается сумма.",
            "Я же платил в прошлом месяце. Почему снова требуют?"
        };
        return new Node[]
        {
            // 0 приветствие
            new Node("Здравствуйте. Вы по какому вопросу?", 0,
                new Opt("Добрый день! Я из управляющей компании, пришёл за оплатой.", 1, 0),
                new Opt("Давайте деньги, у меня нет времени.", 5, -2),
                new Opt("Добрый день! Хотел узнать, как у вас дела с водой и отоплением.", 2, 1)),
            // 1 жалоба
            new Node(complaint[v], 0,
                new Opt("Понимаю вас. Сейчас объясню, что входит в платёж.", 3, 1),
                new Opt("Тариф утверждён, платите и всё.", 5, -2),
                new Opt("Давайте вместе посмотрим вашу квитанцию.", 3, 1)),
            // 2 разговор по душам
            new Node("Спасибо, что спросили! Не каждый интересуется. Вода бывает холодной, а батареи в углах не греют.", 0,
                new Opt("Запишу жалобу и передам мастеру, обещаю.", 3, 2),
                new Opt("Это не мой вопрос.", 5, -1),
                new Opt("А по оплате за коммуналку у вас всё в порядке?", 1, 0)),
            // 3 сомнения
            new Node("Хм... Допустим. Но вы точно меня не обманываете? Мне нужны гарантии.", 0,
                new Opt("Вот расчёт и печать компании, всё честно.", -2, 1),
                new Opt("Если будут вопросы, приходите ко мне в любой момент.", -2, 1),
                new Opt("Платите, иначе будут штрафы.", 5, -2)),
            // 4 оплата
            new Node("Ладно, вы меня убедили. Держите оплату, пожалуйста.", 1,
                new Opt("Спасибо! Всего доброго.", -1, 0)),
            // 5 грубость
            new Node("С таким тоном я разговаривать не буду. Приходите позже.", 2,
                new Opt("Хорошо, зайду позже.", -1, 0)),
            // 6 не убедили
            new Node("Не знаю... Вы меня не убедили. Давайте поговорим ещё раз.", 3,
                new Opt("Хорошо, давайте начнём заново.", 0, 0))
        };
    }
}
