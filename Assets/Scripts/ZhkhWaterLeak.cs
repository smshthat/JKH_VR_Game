using UnityEngine;

// Протечка: труба, фонтан, лужа растёт. Поднесите гаечный ключ (в руке) и подержите несколько секунд.
public class ZhkhWaterLeak : MonoBehaviour
{
    public float fixTime = 4f;
    public float reach = 1.1f;

    static AudioClip hiss;
    static Material spriteMat;
    static readonly Collider[] buf = new Collider[24];

    Transform puddle;
    TextMesh label;
    ParticleSystem fountain;
    AudioSource snd;
    float progress, birth, nextCheck;
    bool done;
    string lastText;

    public static ZhkhWaterLeak Create(Vector3 p)
    {
        GameObject go = new GameObject("ZhkhWaterLeak");
        go.transform.position = p;
        ZhkhWaterLeak l = go.AddComponent<ZhkhWaterLeak>();
        l.Build();
        ZhkhLeakSpawner.Active++;
        return l;
    }

    void Build()
    {
        birth = Time.time;
        ZhkhVR.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.12f, 0.25f, 0.12f), new Color(0.35f, 0.35f, 0.38f), false);
        puddle = ZhkhVR.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.6f, 0.005f, 0.6f), new Color(0.2f, 0.45f, 0.8f), false).transform;

        GameObject fg = new GameObject("Фонтан");
        fg.transform.SetParent(transform, false);
        fg.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        fg.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        fountain = fg.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = fountain.main;
        main.loop = true;
        main.startLifetime = 0.9f;
        main.startSpeed = 2.2f;
        main.startSize = 0.06f;
        main.gravityModifier = 1f;
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(0.5f, 0.75f, 1f, 0.85f);
        ParticleSystem.EmissionModule em = fountain.emission;
        em.rateOverTime = 50f;
        ParticleSystem.ShapeModule sh = fountain.shape;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 12f;
        sh.radius = 0.02f;
        if (spriteMat == null)
        {
            Shader s = Shader.Find("Sprites/Default");
            if (s != null) spriteMat = new Material(s);
        }
        if (spriteMat != null) fg.GetComponent<ParticleSystemRenderer>().sharedMaterial = spriteMat;

        label = ZhkhVR.MakeText("ПРОТЕЧКА!\nНужен гаечный ключ", transform, new Vector3(0f, 1.4f, 0f), 0.005f, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
        label.gameObject.AddComponent<ZhkhKeepVisible>();

        snd = gameObject.AddComponent<AudioSource>();
        snd.clip = Hiss();
        snd.loop = true;
        snd.spatialBlend = 1f;
        snd.minDistance = 1f;
        snd.maxDistance = 15f;
        snd.rolloffMode = AudioRolloffMode.Linear;
        snd.volume = 0.4f;
        snd.Play();
    }

    static AudioClip Hiss()
    {
        if (hiss != null) return hiss;
        int rate = 22050;
        float[] d = new float[rate];
        System.Random rnd = new System.Random(7);
        float prev = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float w = (float)(rnd.NextDouble() * 2.0 - 1.0);
            prev = prev * 0.7f + w * 0.3f;
            d[i] = prev * 0.35f;
        }
        hiss = AudioClip.Create("zhkh_hiss", rate, 1, rate, false);
        hiss.SetData(d, 0);
        return hiss;
    }

    void Update()
    {
        Transform head = ZhkhVR.Head();
        if (label != null && head != null)
        {
            Vector3 d = label.transform.position - head.position;
            if (d.sqrMagnitude > 0.01f) label.transform.rotation = Quaternion.LookRotation(d);
        }
        if (puddle != null)
        {
            float grow = Mathf.Clamp01((Time.time - birth) / 150f);
            float s = Mathf.Lerp(0.6f, 2.2f, grow) * (1f - progress);
            puddle.localScale = new Vector3(s, 0.005f, s);
        }
        if (done || Time.time < nextCheck) return;
        nextCheck = Time.time + 0.2f;
        Check();
    }

    void Check()
    {
        int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.5f, reach, buf, ~0, QueryTriggerInteraction.Ignore);
        bool wrench = false;
        for (int i = 0; i < n; i++)
        {
            Collider c = buf[i];
            if (c == null) continue;
            ZhkhWrench w = c.GetComponentInParent<ZhkhWrench>();
            if (w != null && w.IsHeld) { wrench = true; break; }
        }
        if (wrench) progress += 0.2f / fixTime;
        else progress -= 0.1f / fixTime;
        progress = Mathf.Clamp01(progress);

        if (progress >= 1f) { Finish(); return; }
        SetText(wrench ? "Чиню... " + Mathf.RoundToInt(progress * 100f) + "%" : "ПРОТЕЧКА!\nНужен гаечный ключ");
    }

    void Finish()
    {
        done = true;
        ZhkhLeakSpawner.FixedCount++;
        if (fountain != null) fountain.Stop();
        if (snd != null) snd.Stop();
        SetText("Починено!");
        Destroy(gameObject, 1.5f);
    }

    void SetText(string t)
    {
        if (label == null || t == lastText) return;
        lastText = t;
        label.text = t;
    }

    void OnDestroy()
    {
        if (ZhkhLeakSpawner.Active > 0) ZhkhLeakSpawner.Active--;
    }
}
