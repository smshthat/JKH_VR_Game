using UnityEngine;

// Адаптивная рука с купюрой для любого NPC.
//  • Если у модели есть цепочка плечо-локоть-кисть (Humanoid или кости с понятными названиями), рука двигается через IK по костям.
//  • Если руки нет или она одним куском (модель из кубиков), создаётся своя рука (плечо, локоть, кисть),
//    цвет берётся с модели; родная правая рука на это время прячется.
// Позы: прижата к груди с купюрой (NPC ещё не согласился) и протянута к игроку (после «Держите оплату»).
// Купюру привязывает к точке HoldPoint скрипт ZhkhBillHold.
[DefaultExecutionOrder(170)]
public class ZhkhHandRig : MonoBehaviour
{
    public bool offer;   // NPC держит купюру (ставит ZhkhBillHold)
    public bool give;    // NPC протягивает купюру игроку (ставит диалог)
    public float giveTimeout = 25f;

    public Transform HoldPoint { get { return hold; } }
    public bool Ready { get { return built; } }

    ZhkhNpcAnimator anim;
    Transform upper, lower, hand, hold;
    bool chain, built, procVisible;
    float l1, l2, reachHold, reachGive, giveTimer, height;
    Vector3 tgtS;
    bool tgtInit;

    // процедурная рука
    GameObject holder;
    Transform seg1, seg2, ball1, ball2, ball0, palm, thumb;
    Renderer[] hiddenArm;
    float thick;

    void LateUpdate()
    {
        if (!built)
        {
            if (anim == null) anim = GetComponent<ZhkhNpcAnimator>();
            if (anim != null && anim.Ready) Build();
            return;
        }
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        NPCController c = GetComponent<NPCController>();
        if (give)
        {
            giveTimer += dt;
            if (giveTimer > giveTimeout || (c != null && c.money != null && c.money.wasTaken)) { give = false; giveTimer = 0f; }
        }
        reachHold = Mathf.MoveTowards(reachHold, (offer && !give) ? 1f : 0f, dt * 2f);
        reachGive = Mathf.MoveTowards(reachGive, give ? 1f : 0f, dt * 1.5f);
        float reach = Mathf.Max(reachHold, reachGive);

        Transform pl = ZhkhVR.Head();
        if (chain) UpdateChain(pl, dt, reach);
        else UpdateProc(pl, dt, reach, c);
    }

    // ---------- построение ----------

    void Build()
    {
        try
        {
            Animator an = GetComponentInChildren<Animator>();
            if (an != null && an.isHuman)
            {
                upper = an.GetBoneTransform(HumanBodyBones.RightUpperArm);
                lower = an.GetBoneTransform(HumanBodyBones.RightLowerArm);
                hand = an.GetBoneTransform(HumanBodyBones.RightHand);
            }
            else
            {
                upper = anim.RightArmTransform;
                if (upper != null) FindChain();
            }
            chain = upper != null && lower != null && hand != null
                && lower.IsChildOf(upper) && hand.IsChildOf(lower)
                && Vector3.Distance(upper.position, lower.position) > 0.03f
                && Vector3.Distance(lower.position, hand.position) > 0.03f;

            height = Mathf.Max(anim.BodyHeight, 0.8f);
            if (chain)
            {
                l1 = Vector3.Distance(upper.position, lower.position);
                l2 = Vector3.Distance(lower.position, hand.position);
                GameObject h = new GameObject("ZhkhHold");
                h.transform.position = hand.position + (hand.position - lower.position).normalized * 0.05f;
                h.transform.SetParent(hand, true);
                hold = h.transform;
            }
            else BuildProcedural();
            built = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("ZhkhHandRig: не удалось настроить " + name + ": " + e.Message);
            enabled = false;
        }
    }

    void FindChain()
    {
        // от плеча идём вглубь: берём ветку с наибольшим числом потомков
        lower = BestChild(upper);
        hand = lower != null ? BestChild(lower) : null;
        if (lower != null && hand == null && lower.childCount > 0) hand = lower.GetChild(0);
    }

    Transform BestChild(Transform p)
    {
        Transform best = null; int bestCount = -1;
        for (int i = 0; i < p.childCount; i++)
        {
            Transform c = p.GetChild(i);
            if (c.GetComponent<TextMesh>() != null || c.name.Contains("Реплика") || c.name == "ZhkhHand" || c.name == "ZhkhHold") continue;
            NPCController nc = GetComponent<NPCController>();
            if (nc != null && nc.money != null && c.IsChildOf(nc.money.transform)) continue;
            int n = c.GetComponentsInChildren<Transform>(true).Length;
            if (n > bestCount) { bestCount = n; best = c; }
        }
        return best;
    }

    static Color MatColor(Renderer r, Color fallback)
    {
        Material m = r.sharedMaterial;
        if (m == null) return fallback;
        Color c = fallback;
        if (m.HasProperty("_Color")) c = m.GetColor("_Color");
        else if (m.HasProperty("_BaseColor")) c = m.GetColor("_BaseColor");
        c.a = 1f;
        return c;
    }

    void BuildProcedural()
    {
        l1 = 0.19f * height;
        l2 = 0.17f * height;
        thick = 0.045f * height;

        Color skin = new Color(0.93f, 0.76f, 0.64f);
        Color sleeve = new Color(0.3f, 0.4f, 0.6f);
        Renderer[] rs = GetComponentsInChildren<Renderer>(false);
        Renderer top = null, big = null; float topY = -1e9f, bigV = 0f;
        for (int i = 0; i < rs.Length; i++)
        {
            Renderer r = rs[i];
            if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || r.GetComponent<TextMesh>() != null) continue;
            if (r.transform.name.Contains("Реплика")) continue;
            Bounds b = r.bounds;
            if (b.center.y > topY) { topY = b.center.y; top = r; }
            float v = b.size.x * b.size.y * b.size.z;
            if (v > bigV) { bigV = v; big = r; }
        }
        if (big != null) sleeve = MatColor(big, sleeve);
        if (top != null && top != big) skin = MatColor(top, skin);

        holder = new GameObject("ZhkhArm_" + name);
        Transform t = holder.transform;
        seg1 = ZhkhVR.Prim(PrimitiveType.Cylinder, t, Vector3.zero, Vector3.one, sleeve, false).transform;
        seg2 = ZhkhVR.Prim(PrimitiveType.Cylinder, t, Vector3.zero, Vector3.one, sleeve, false).transform;
        ball0 = ZhkhVR.Prim(PrimitiveType.Sphere, t, Vector3.zero, Vector3.one * thick * 1.15f, sleeve, false).transform;
        ball1 = ZhkhVR.Prim(PrimitiveType.Sphere, t, Vector3.zero, Vector3.one * thick * 1.1f, sleeve, false).transform;
        ball2 = ZhkhVR.Prim(PrimitiveType.Sphere, t, Vector3.zero, Vector3.one * thick * 0.95f, sleeve, false).transform;
        palm = ZhkhVR.Prim(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.05f, 0.07f, 0.03f) * height, skin, false).transform;
        thumb = ZhkhVR.Prim(PrimitiveType.Sphere, t, Vector3.zero, Vector3.one * 0.022f * height, skin, false).transform;
        GameObject h = new GameObject("ZhkhHold");
        h.transform.SetParent(t, false);
        hold = h.transform;
        holder.SetActive(false);

        // родная правая рука-кусок прячется, пока видна наша
        Transform arm = anim.RightArmTransform;
        if (arm != null)
        {
            Renderer[] ar = arm.GetComponentsInChildren<Renderer>(true);
            System.Collections.Generic.List<Renderer> keep = new System.Collections.Generic.List<Renderer>();
            NPCController nc = GetComponent<NPCController>();
            for (int i = 0; i < ar.Length; i++)
            {
                if (ar[i] is SkinnedMeshRenderer || ar[i] is ParticleSystemRenderer) continue;
                if (ar[i].GetComponent<TextMesh>() != null) continue;
                if (nc != null && nc.money != null && ar[i].transform.IsChildOf(nc.money.transform)) continue;
                keep.Add(ar[i]);
            }
            hiddenArm = keep.ToArray();
        }
    }

    // ---------- расчёт руки ----------

    Vector3 Targets(Vector3 S, Vector3 rest, Transform pl, float dt, float reach)
    {
        float L = l1 + l2;
        Vector3 fwd = transform.forward, up = transform.up, right = transform.right;
        Vector3 holdT = S + fwd * (0.55f * L) - up * (0.5f * L) + right * (0.05f * L);
        Vector3 giveT = S + fwd * (0.9f * L) - up * (0.35f * L);
        if (pl != null)
        {
            Vector3 d = pl.position + Vector3.down * 0.55f - S;
            float m = d.magnitude;
            if (m > 0.05f) giveT = S + d / m * Mathf.Min(L * 0.97f, m * 0.7f);
        }
        Vector3 tgt = rest;
        tgt = Vector3.Lerp(tgt, holdT, reachHold);
        tgt = Vector3.Lerp(tgt, giveT, reachGive);
        tgt += up * (Mathf.Sin(Time.time * 1.8f) * 0.01f * reach);
        if (!tgtInit || reach < 0.001f) { tgtS = tgt; tgtInit = true; }
        else tgtS = Vector3.Lerp(tgtS, tgt, 1f - Mathf.Exp(-12f * dt));
        return tgtS;
    }

    void Solve(Vector3 S, Vector3 T, out Vector3 elbow, out Vector3 wrist)
    {
        Vector3 st = T - S;
        float d = Mathf.Clamp(st.magnitude, Mathf.Abs(l1 - l2) + 0.01f, l1 + l2 - 0.005f);
        Vector3 dir = st.sqrMagnitude > 1e-8f ? st.normalized : transform.forward;
        float a = (l1 * l1 - l2 * l2 + d * d) / (2f * d);
        float h = Mathf.Sqrt(Mathf.Max(l1 * l1 - a * a, 0f));
        float side = Vector3.Dot(S - transform.position, transform.right) >= 0f ? 1f : -1f;
        Vector3 pole = -transform.up * 0.6f + transform.right * side * 0.8f;
        Vector3 po = Vector3.ProjectOnPlane(pole, dir);
        if (po.sqrMagnitude < 1e-6f) po = Vector3.ProjectOnPlane(-transform.up, dir);
        po.Normalize();
        elbow = S + dir * a + po * h;
        wrist = S + dir * d;
    }

    void UpdateChain(Transform pl, float dt, float reach)
    {
        if (upper == null || lower == null || hand == null) return;
        if (reach < 0.001f) { tgtInit = false; return; }
        Vector3 S = upper.position;
        Vector3 T = Targets(S, hand.position, pl, dt, reach);
        Vector3 elbow, wrist;
        Solve(S, T, out elbow, out wrist);
        Vector3 cu = (lower.position - S).normalized;
        upper.rotation = Quaternion.FromToRotation(cu, (elbow - S).normalized) * upper.rotation;
        Vector3 cl = (hand.position - lower.position).normalized;
        lower.rotation = Quaternion.FromToRotation(cl, (wrist - lower.position).normalized) * lower.rotation;
    }

    void UpdateProc(Transform pl, float dt, float reach, NPCController c)
    {
        bool show = reach > 0.02f;
        if (show != procVisible)
        {
            procVisible = show;
            if (holder != null) holder.SetActive(show);
            if (hiddenArm != null)
                for (int i = 0; i < hiddenArm.Length; i++) if (hiddenArm[i] != null) hiddenArm[i].enabled = !show;
        }
        if (!show) { tgtInit = false; return; }

        Vector3 S = anim.RightShoulderWorld();
        Vector3 rest = S - transform.up * ((l1 + l2) * 0.95f);
        Vector3 T = Targets(S, rest, pl, dt, reach);
        Vector3 E, W;
        Solve(S, T, out E, out W);

        Place(seg1, S, E, thick);
        Place(seg2, E, W, thick * 0.85f);
        ball0.position = S; ball1.position = E; ball2.position = W;

        Vector3 fd = (W - E).normalized;
        palm.position = W + fd * (0.035f * height);
        palm.rotation = Quaternion.FromToRotation(Vector3.up, fd);
        thumb.position = palm.position + palm.right * (0.03f * height) + fd * (0.01f * height);
        hold.position = palm.position + fd * (0.02f * height);
    }

    static void Place(Transform seg, Vector3 a, Vector3 b, float th)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.0001f) return;
        seg.position = (a + b) * 0.5f;
        seg.rotation = Quaternion.FromToRotation(Vector3.up, d / len);
        seg.localScale = new Vector3(th, len * 0.5f, th);
    }

    void OnDisable()
    {
        if (holder != null) holder.SetActive(false);
        procVisible = false;
        if (hiddenArm != null)
            for (int i = 0; i < hiddenArm.Length; i++) if (hiddenArm[i] != null) hiddenArm[i].enabled = true;
    }

    void OnDestroy()
    {
        if (holder != null) Destroy(holder);
    }
}
