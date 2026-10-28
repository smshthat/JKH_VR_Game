using UnityEngine;
using UnityEngine.XR;
using Valve.VR.InteractionSystem;

// Конец смены: когда выполнены все задания, перед глазами появляется экран «СМЕНА ЗАВЕРШЕНА»
// с итогами, звёздами за скорость, фанфарами и конфетти.
// Задания смены: собрать оплату у ВСЕХ жильцов + все задачи на доске заданий (лампочка, мусор, вентиль, почта).
// Создаётся само при запуске игры, вешать никуда не надо.
public class ShiftManager : MonoBehaviour
{
    public float threeStarsTime = 480f;     // быстрее этого времени (сек): 3 звезды
    public float twoStarsTime = 840f;       // быстрее этого времени: 2 звезды, иначе 1
    public float showTime = 20f;            // сколько секунд висит экран

    public static bool shiftEnded;          // смена завершена

    NPCController[] npcs;
    TaskZone[] zones;
    ValveTask[] valves;
    Transform head;
    Transform panel;
    ParticleSystem confetti;
    float startTime;
    float checkTimer;
    float endDelay = -1f;
    float shownFor;
    bool showing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        shiftEnded = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        new GameObject("КонецСмены").AddComponent<ShiftManager>();
    }

    void Start()
    {
        head = Player.instance.hmdTransform;
        npcs = FindObjectsByType<NPCController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        zones = FindObjectsByType<TaskZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        valves = FindObjectsByType<ValveTask>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        startTime = Time.time;
    }

    void Update()
    {
        if (head == null) return;

        if (!shiftEnded)
        {
            checkTimer -= Time.deltaTime;
            if (checkTimer <= 0f)
            {
                checkTimer = 0.25f;
                if (AllDone())
                {
                    if (endDelay < 0f) endDelay = 1.5f;     // небольшая пауза перед экраном
                }
            }
            if (endDelay >= 0f)
            {
                endDelay -= Time.deltaTime;
                if (endDelay < 0f) EndShift();
            }
        }

        if (showing)
        {
            // Экран плавно следует за взглядом
            Vector3 target = head.position + head.forward * 1.3f + Vector3.down * 0.05f;
            panel.position = Vector3.Lerp(panel.position, target, 4f * Time.deltaTime);
            Vector3 away = panel.position - head.position;
            if (away.sqrMagnitude > 0.001f) panel.rotation = Quaternion.LookRotation(away);

            shownFor += Time.deltaTime;
            if (shownFor > showTime)
            {
                showing = false;
                panel.gameObject.SetActive(false);
            }
        }
    }

    // Все ли задания выполнены
    bool AllDone()
    {
        if (npcs.Length == 0) return false;
        foreach (NPCController n in npcs)
            if (n == null || n.money == null || !n.money.wasTaken) return false;
        foreach (TaskZone z in zones)
            if (z != null && !z.done) return false;
        foreach (ValveTask v in valves)
            if (v != null && !v.done) return false;
        return true;
    }

    void EndShift()
    {
        shiftEnded = true;
        BuildPanel();
        showing = true;
        shownFor = 0f;

        // Фанфары и конфетти
        AudioSource.PlayClipAtPoint(SimpleSound.Fanfare(), head.position, 1f);
        BuildConfetti();
        confetti.transform.position = head.position + Vector3.up * 1.8f;
        confetti.Play();
    }

    // ---------- Экран итогов ----------
    void BuildPanel()
    {
        panel = new GameObject("ЭкранКонцаСмены").transform;
        panel.position = head.position + head.forward * 1.3f;

        float elapsed = Time.time - startTime;
        int stars = elapsed <= threeStarsTime ? 3 : (elapsed <= twoStarsTime ? 2 : 1);

        int taken = 0;
        foreach (NPCController n in npcs)
            if (n != null && n.money != null && n.money.wasTaken) taken++;
        int tasksDone = 0;
        int tasksTotal = zones.Length + valves.Length;
        foreach (TaskZone z in zones) if (z != null && z.done) tasksDone++;
        foreach (ValveTask v in valves) if (v != null && v.done) tasksDone++;

        // Рамка и фон
        Quad("Рамка", new Vector3(0, 0, 0.002f), new Vector3(1.46f, 0.96f, 1f), new Color(1f, 0.8f, 0.2f, 1f));
        Quad("Фон", new Vector3(0, 0, 0f), new Vector3(1.4f, 0.9f, 1f), new Color(0.05f, 0.07f, 0.14f, 0.96f));

        // Заголовок
        Text("Заголовок", "СМЕНА ЗАВЕРШЕНА", new Vector3(0, 0.33f, -0.005f), 0.012f, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.25f));
        Text("Подзаголовок", "Отличная работа!", new Vector3(0, 0.22f, -0.005f), 0.0065f, TextAnchor.MiddleCenter, Color.white);

        // Звёзды (ромбики)
        for (int i = 0; i < 3; i++)
        {
            GameObject star = Quad("Звезда", new Vector3((i - 1) * 0.2f, 0.1f, -0.006f), new Vector3(0.13f, 0.13f, 1f),
                                   i < stars ? new Color(1f, 0.82f, 0.2f, 1f) : new Color(0.3f, 0.32f, 0.38f, 1f));
            star.transform.localRotation = Quaternion.Euler(0, 0, 45f);
        }

        // Итоги
        string time = Mathf.FloorToInt(elapsed / 60f).ToString("00") + ":" + Mathf.FloorToInt(elapsed % 60f).ToString("00");
        string stats = "Оплата собрана: " + taken + "/" + npcs.Length + "\n"
                     + "Задания выполнены: " + tasksDone + "/" + tasksTotal + "\n"
                     + "Время смены: " + time;
        Text("Итоги", stats, new Vector3(-0.6f, -0.12f, -0.005f), 0.0055f, TextAnchor.MiddleLeft, Color.white);

        Text("Подвал", "Можно продолжить гулять по городу", new Vector3(0, -0.4f, -0.005f), 0.0033f, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
    }

    GameObject Quad(string name, Vector3 localPos, Vector3 scale, Color color)
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(q.GetComponent<Collider>());
        q.name = name;
        q.transform.SetParent(panel, false);
        q.transform.localPosition = localPos;
        q.transform.localScale = scale;
        Material m = new Material(Shader.Find("Sprites/Default"));
        m.color = color;
        q.GetComponent<Renderer>().material = m;
        return q;
    }

    TextMesh Text(string name, string text, Vector3 localPos, float charSize, TextAnchor anchor, Color color)
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

    // ---------- Конфетти (частицы создаются кодом) ----------
    void BuildConfetti()
    {
        GameObject go = new GameObject("Конфетти");
        confetti = go.AddComponent<ParticleSystem>();
        confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = confetti.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 5f;
        main.startSpeed = 1.5f;
        main.startSize = 0.07f;
        main.gravityModifier = 0.25f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.3f, 0.3f), 0f),
                new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.25f),
                new GradientColorKey(new Color(0.3f, 0.9f, 0.4f), 0.5f),
                new GradientColorKey(new Color(0.3f, 0.7f, 1f), 0.75f),
                new GradientColorKey(new Color(0.8f, 0.4f, 1f), 1f)
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        ParticleSystem.MinMaxGradient sc = main.startColor;
        sc.mode = ParticleSystemGradientMode.RandomColor;
        sc.gradient = grad;
        main.startColor = sc;

        var emission = confetti.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 160) });

        var shape = confetti.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(3f, 0.1f, 3f);

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.material = new Material(Shader.Find("Sprites/Default"));
    }
}
