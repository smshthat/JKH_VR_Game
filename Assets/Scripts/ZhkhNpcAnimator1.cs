using System.Collections.Generic;
using UnityEngine;

// Адаптивная процедурная анимация NPC. Работает на любой модели:
//  1) Humanoid Animator: берёт кости из аватара;
//  2) любой скелет: ищет кости по названию (head/голова, arm/рука, leg/нога...);
//  3) модель из кубиков/капсул: определяет голову, руки и ноги по положению частей.
// Все повороты считаются в мировых осях NPC вокруг шарниров, поэтому оси костей модели не важны.
// Что умеет: дыхание, взгляд на игрока, кивки и жесты при разговоре, шаг при ходьбе,
// покачивание при ударе, протянутая рука с купюрой.
[DefaultExecutionOrder(150)]
public class ZhkhNpcAnimator : MonoBehaviour
{
    public bool talking;            // ставит диалог
    public bool offering;           // ставит ZhkhBillHold, когда NPC держит купюру
    public float lookDistance = 6f;
    public float intensity = 1f;
    public float activeDistance = 30f;
    public Transform handAnchor;    // кисть правой руки (к ней привязана купюра)
    public bool Ready { get { return built; } }
    public float BodyHeight { get; private set; }
    public Transform RightArmTransform { get { return armR != null ? armR.t : null; } }
    float bodyMinY;

    public Vector3 RightShoulderWorld()
    {
        if (armR != null && armR.t != null) return armR.t.TransformPoint(armR.pivotLocal);
        float h = Mathf.Max(BodyHeight, 1f);
        return new Vector3(transform.position.x, bodyMinY + h * 0.82f, transform.position.z) + transform.right * (0.14f * h);
    }

    class Bone { public Transform t; public Vector3 basePos, framePos; public Quaternion baseRot, frameRot; public Vector3 pivotLocal; public int side; }
    bool applied;

    void Update()
    {
        // Remove last frame's procedural offsets before the Animator evaluates.
        if (!applied || !additive) return;
        foreach (var b in all) if (b.t != null) { b.t.localPosition = b.framePos; b.t.localRotation = b.frameRot; }
        applied = false;
    }

    Bone head, spine, armL, armR, legL, legR;
    readonly List<Bone> all = new List<Bone>();
    Animator animator;
    bool additive, built, hierarchical;
    float seed, stagger, talkBlend, offerBlend, walkBlend, walkPhase, lookYaw, lookPitch, speed;
    Vector3 lastPos;

    int attempts;

    void Awake() { seed = Random.value * 20f; }

    void OnEnable()
    {
        lastPos = transform.position;
        if (!built) Invoke("Resolve", 0.6f);
    }

    public void Stagger() { stagger = 0.6f; }

    // ---------- поиск костей ----------

    void Resolve()
    {
        try
        {
            animator = GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman) FindHumanoid();
            else FindByName();
            if (head == null && armR == null && armL == null && legL == null && legR == null) FindByGeometry();
            if (head == null && armR == null && armL == null && legL == null && legR == null && attempts < 6)
            {
                // части модели ещё не созданы другими скриптами: пробуем позже
                attempts++;
                all.Clear();
                Invoke("Resolve", 1f);
                return;
            }
            hierarchical = spine != null && head != null && head.t.IsChildOf(spine.t);
            additive = animator != null && animator.runtimeAnimatorController != null && animator.isActiveAndEnabled;
            if (!additive)
            {
                LowerRestArm(armL); LowerRestArm(armR);
                foreach (var b in all) if (b.t != null) { b.basePos = b.t.localPosition; b.baseRot = b.t.localRotation; }
            }
            ComputeBody();
            MakeHandAnchor();
            built = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("ZhkhNpcAnimator: не удалось настроить " + name + ": " + e.Message);
            enabled = false;
        }
    }

    Transform H(HumanBodyBones b) { return animator.GetBoneTransform(b); }

    void LowerRestArm(Bone arm, bool horizontalOnly = false)
    {
        if (arm == null) return;
        foreach (var t in arm.t.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (t == arm.t || !(n.Contains("forearm") || n.Contains("lowerarm") || n.Contains("lowearm"))) continue;
            Vector3 direction = t.position - arm.t.position;
            if (horizontalOnly && Mathf.Abs(Vector3.Dot(direction.normalized, transform.up)) > 0.45f) break;
            if (direction.sqrMagnitude > 0.0001f)
                arm.t.rotation = Quaternion.FromToRotation(direction, -transform.up + transform.right * arm.side * 0.12f) * arm.t.rotation;
            break;
        }
    }

    void FindHumanoid()
    {
        head = Mk(H(HumanBodyBones.Head), 0);
        Transform sp = H(HumanBodyBones.UpperChest);
        if (sp == null) sp = H(HumanBodyBones.Chest);
        if (sp == null) sp = H(HumanBodyBones.Spine);
        spine = Mk(sp, 0);
        armL = Mk(H(HumanBodyBones.LeftUpperArm), 0);
        armR = Mk(H(HumanBodyBones.RightUpperArm), 0);
        legL = Mk(H(HumanBodyBones.LeftUpperLeg), 0);
        legR = Mk(H(HumanBodyBones.RightUpperLeg), 0);
        handAnchor = H(HumanBodyBones.RightHand);
    }

    bool IsExcluded(Transform t)
    {
        NPCController c = GetComponent<NPCController>();
        if (c != null && c.money != null && t.IsChildOf(c.money.transform)) return true;
        for (Transform p = t; p != null && p != transform; p = p.parent)
            if (p.GetComponent<TextMesh>() != null || p.name.Contains("Реплика")) return true;
        return false;
    }

    static bool IsArmName(string n)
    {
        if (n.Contains("hand") || n.Contains("кист") || n.Contains("finger") || n.Contains("палец")) return false;
        if (n.Contains("fore") || n.Contains("lower") || n.Contains("предплеч") || n.Contains("armature") || n.Contains("armor")) return false;
        return n.Contains("arm") || n.Contains("рука") || n.Contains("руки") || n.Contains("плеч");
    }

    static bool IsLegName(string n)
    {
        if (n.Contains("foot") || n.Contains("toe") || n.Contains("стоп") || n.Contains("lower") || n.Contains("calf") || n.Contains("shin")) return false;
        return n.Contains("leg") || n.Contains("thigh") || n.Contains("нога") || n.Contains("ноги") || n.Contains("бедр");
    }

    static void KeepTop(List<Transform> l)
    {
        for (int i = l.Count - 1; i >= 0; i--)
            for (int j = 0; j < l.Count; j++)
                if (j != i && l[i].IsChildOf(l[j])) { l.RemoveAt(i); break; }
    }

    void PickSides(List<Transform> l, out Transform left, out Transform right)
    {
        left = null; right = null;
        if (l.Count == 0) return;
        float maxD = -9999f, minD = 9999f;
        for (int i = 0; i < l.Count; i++)
        {
            float d = Vector3.Dot(l[i].position - transform.position, transform.right);
            if (d > maxD) { maxD = d; right = l[i]; }
            if (d < minD) { minD = d; left = l[i]; }
        }
        if (left == right)
        {
            if (maxD >= 0f) left = null; else right = null;
        }
    }

    void FindByName()
    {
        var boneSet = new HashSet<Transform>();
        foreach (var skin in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            foreach (var bone in skin.bones) if (bone != null) boneSet.Add(bone);
        Transform[] ts = boneSet.Count > 0 ? new List<Transform>(boneSet).ToArray() : GetComponentsInChildren<Transform>(true);
        Transform hd = null, sp = null;
        List<Transform> arms = new List<Transform>();
        List<Transform> legs = new List<Transform>();
        for (int i = 0; i < ts.Length; i++)
        {
            Transform t = ts[i];
            if (t == transform || IsExcluded(t)) continue;
            string n = t.name.ToLowerInvariant();
            if (hd == null && (n.Contains("head") || n.Contains("голов")) && !n.Contains("headphone")) { hd = t; continue; }
            if (sp == null && (n.Contains("spine") || n.Contains("chest") || n.Contains("torso") || n.Contains("туловищ") || n.Contains("корпус") || n.Contains("грудь") || n == "body" || n.Contains("тело"))) { sp = t; continue; }
            if (IsArmName(n)) arms.Add(t);
            else if (IsLegName(n)) legs.Add(t);
        }
        KeepTop(arms); KeepTop(legs);
        Transform aL, aR, lL, lR;
        PickSides(arms, out aL, out aR);
        PickSides(legs, out lL, out lR);
        head = Mk(hd, 2);
        spine = Mk(sp, 2);
        armL = Mk(aL, 1); armR = Mk(aR, 1);
        legL = Mk(lL, 1); legR = Mk(lR, 1);
        if (armR != null) handAnchor = FindHand(armR.t);
    }

    Transform FindHand(Transform arm)
    {
        Transform deepest = null; int depth = -1;
        Transform[] cs = arm.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < cs.Length; i++)
        {
            Transform c = cs[i];
            if (c == arm || IsExcluded(c)) continue;
            string n = c.name.ToLowerInvariant();
            if (n.Contains("hand") || n.Contains("кист") || n.Contains("ладон") || n.Contains("palm")) return c;
            int d = 0;
            for (Transform p = c; p != null && p != arm; p = p.parent) d++;
            if (d > depth) { depth = d; deepest = c; }
        }
        return deepest;
    }

    // Модель из отдельных кусков (кубы/капсулы): классифицируем по положению
    void FindByGeometry()
    {
        Renderer[] rs = GetComponentsInChildren<Renderer>(false);
        List<Renderer> parts = new List<Renderer>();
        for (int i = 0; i < rs.Length; i++)
        {
            Renderer r = rs[i];
            if (r is SkinnedMeshRenderer || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
            if (IsExcluded(r.transform)) continue;
            parts.Add(r);
        }
        if (parts.Count < 3) return;

        float rootY = transform.position.y;
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < parts.Count; i++)
        {
            Bounds b = parts[i].bounds;
            minY = Mathf.Min(minY, b.min.y);
            maxY = Mathf.Max(maxY, b.max.y);
        }
        float Hh = maxY - minY;
        if (Hh < 0.3f) return;

        Renderer torso = null; float bestVol = 0f;
        for (int i = 0; i < parts.Count; i++)
        {
            Bounds b = parts[i].bounds;
            float ny = (b.center.y - minY) / Hh;
            if (ny < 0.3f || ny > 0.8f) continue;
            float vol = b.size.x * b.size.y * b.size.z;
            if (vol > bestVol) { bestVol = vol; torso = parts[i]; }
        }
        float torsoHalf = torso != null ? Mathf.Max(torso.bounds.extents.x, torso.bounds.extents.z) : 0.2f;

        Renderer hd = null; float topY = -1f;
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] == torso) continue;
            Bounds b = parts[i].bounds;
            if ((b.center.y - minY) / Hh > 0.78f && b.center.y > topY) { topY = b.center.y; hd = parts[i]; }
        }

        Renderer aL = null, aR = null, lL = null, lR = null;
        float sAL = 0f, sAR = 0f, sLL = 0f, sLR = 0f;
        for (int i = 0; i < parts.Count; i++)
        {
            Renderer r = parts[i];
            if (r == torso || r == hd) continue;
            Bounds b = r.bounds;
            float ny = (b.center.y - minY) / Hh;
            float x = Vector3.Dot(b.center - transform.position, transform.right);
            float size = b.size.y;
            if (ny > 0.3f && ny < 0.92f && Mathf.Abs(x) > torsoHalf * 0.6f)
            {
                if (x > 0f && size > sAR) { sAR = size; aR = r; }
                else if (x < 0f && size > sAL) { sAL = size; aL = r; }
            }
            else if (ny < 0.45f && Mathf.Abs(x) > 0.02f)
            {
                if (x > 0f && size > sLR) { sLR = size; lR = r; }
                else if (x < 0f && size > sLL) { sLL = size; lL = r; }
            }
        }
        head = Mk(hd != null ? hd.transform : null, 2);
        armL = Mk(aL != null ? aL.transform : null, 1);
        armR = Mk(aR != null ? aR.transform : null, 1);
        legL = Mk(lL != null ? lL.transform : null, 1);
        legR = Mk(lR != null ? lR.transform : null, 1);
        if (armR != null) handAnchor = FindHand(armR.t);
    }

    // pivotMode: 0 = шарнир кости, 1 = верх меша (плечо/бедро), 2 = низ меша (шея/поясница)
    Bone Mk(Transform t, int pivotMode)
    {
        if (t == null) return null;
        for (int i = 0; i < all.Count; i++) if (all[i].t == t) return all[i];
        Bone b = new Bone();
        b.t = t; b.basePos = t.localPosition; b.baseRot = t.localRotation;
        Vector3 wp = t.position;
        Renderer r = t.GetComponent<Renderer>();
        if (pivotMode != 0 && r != null && !(r is SkinnedMeshRenderer))
        {
            Bounds bb = r.bounds;
            wp = pivotMode == 1 ? new Vector3(bb.center.x, bb.max.y, bb.center.z) : new Vector3(bb.center.x, bb.min.y, bb.center.z);
        }
        b.pivotLocal = t.InverseTransformPoint(wp);
        b.side = Vector3.Dot(wp - transform.position, transform.right) >= 0f ? 1 : -1;
        all.Add(b);
        return b;
    }

    void ComputeBody()
    {
        Bounds enc = new Bounds(transform.position, Vector3.zero);
        Renderer[] rs = GetComponentsInChildren<Renderer>(false);
        for (int i = 0; i < rs.Length; i++)
        {
            Renderer r = rs[i];
            if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || IsExcluded(r.transform)) continue;
            enc.Encapsulate(r.bounds);
        }
        BodyHeight = Mathf.Clamp(enc.size.y, 0.5f, 3f);
        bodyMinY = enc.min.y;
    }

    void MakeHandAnchor()
    {
        if (handAnchor != null) return;
        GameObject g = new GameObject("ZhkhHand");
        if (armR != null)
        {
            Renderer r = armR.t.GetComponent<Renderer>();
            Vector3 wp = (r != null && !(r is SkinnedMeshRenderer))
                ? new Vector3(r.bounds.center.x, r.bounds.min.y, r.bounds.center.z)
                : armR.t.position - transform.up * 0.45f;
            g.transform.SetParent(armR.t, true);
            g.transform.position = wp;
        }
        else
        {
            Bounds enc = new Bounds(transform.position, Vector3.zero);
            Renderer[] rs = GetComponentsInChildren<Renderer>(false);
            for (int i = 0; i < rs.Length; i++) if (!(rs[i] is ParticleSystemRenderer) && !IsExcluded(rs[i].transform)) enc.Encapsulate(rs[i].bounds);
            float h = Mathf.Max(enc.size.y, 1f);
            g.transform.SetParent(transform, true);
            g.transform.position = new Vector3(transform.position.x, enc.min.y + h * 0.55f, transform.position.z)
                + transform.right * Mathf.Max(0.25f, enc.extents.x * 0.8f) + transform.forward * 0.15f;
        }
        handAnchor = g.transform;
    }

    // ---------- анимация ----------

    void Rot(Bone b, Vector3 axis, float deg)
    {
        if (b == null || b.t == null || Mathf.Abs(deg) < 0.001f) return;
        b.t.RotateAround(b.t.TransformPoint(b.pivotLocal), axis, deg);
    }

    void LateUpdate()
    {
        if (!built) return;
        Transform pl = ZhkhVR.Head();
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        if (dt <= 0f) return;
        if (pl != null && (pl.position - transform.position).sqrMagnitude > activeDistance * activeDistance) { lastPos = transform.position; return; }

        Vector3 pos = transform.position;
        float sp = new Vector3(pos.x - lastPos.x, 0f, pos.z - lastPos.z).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        speed = Mathf.Lerp(speed, Mathf.Min(sp, 3f), 1f - Mathf.Exp(-8f * dt));
        lastPos = pos;

        // при разговоре поворачиваемся к игроку
        talkBlend = Mathf.MoveTowards(talkBlend, talking ? 1f : 0f, dt * 3f);
        offerBlend = Mathf.MoveTowards(offerBlend, offering ? 1f : 0f, dt * 2.5f);
        if (talking && pl != null)
        {
            Vector3 d = pl.position - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d.normalized, Vector3.up), 1f - Mathf.Exp(-3f * dt));
        }

        if (!additive)
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].t == null) continue;
                all[i].t.localPosition = all[i].basePos;
                all[i].t.localRotation = all[i].baseRot;
            }

        foreach (var b in all) if (b.t != null) { b.framePos = b.t.localPosition; b.frameRot = b.t.localRotation; }
        applied = true;
        LowerRestArm(armL, true); LowerRestArm(armR, true);
        float t = Time.time + seed;
        float I = intensity;
        Vector3 up = transform.up, right = transform.right, fwd = transform.forward;

        float walkTarget = Mathf.Clamp01(speed / 0.4f);
        walkBlend = Mathf.Lerp(walkBlend, walkTarget, 1f - Mathf.Exp(-6f * dt));
        walkPhase += speed * dt * 2.2f * Mathf.PI * 2f * 0.5f;
        float swing = Mathf.Sin(walkPhase) * 26f * walkBlend;

        float breathe = Mathf.Sin(t * 1.7f);
        float sway = Mathf.Sin(t * 0.6f);
        float stg = stagger > 0f ? Mathf.Sin(Mathf.PI * (1f - stagger / 0.6f)) : 0f;
        stagger = Mathf.Max(0f, stagger - dt);

        // корпус
        if (hierarchical)
        {
            Rot(spine, right, (breathe * 1.1f - 12f * stg + talkBlend * Mathf.Sin(t * 1.9f) * 1.5f) * I);
            Rot(spine, fwd, sway * 0.8f * I);
        }

        // голова: смотрит на игрока
        if (head != null)
        {
            float ty = 0f, tp = 0f;
            if (pl != null)
            {
                Vector3 hp = head.t.TransformPoint(head.pivotLocal);
                Vector3 d = pl.position - hp;
                float dist = d.magnitude;
                if (dist < lookDistance && dist > 0.2f)
                {
                    Vector3 fl = Vector3.ProjectOnPlane(fwd, Vector3.up);
                    Vector3 dl = Vector3.ProjectOnPlane(d, Vector3.up);
                    if (fl.sqrMagnitude > 0.0001f && dl.sqrMagnitude > 0.0001f)
                        ty = Mathf.Clamp(Vector3.SignedAngle(fl, dl, Vector3.up), -65f, 65f);
                    tp = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(d.y / dist, -1f, 1f)) * Mathf.Rad2Deg, -25f, 25f);
                }
            }
            float k = 1f - Mathf.Exp(-5f * dt);
            lookYaw = Mathf.Lerp(lookYaw, ty, k);
            lookPitch = Mathf.Lerp(lookPitch, tp, k);
            float nod = Mathf.Sin(t * 5.5f) * 3.5f * talkBlend;
            Rot(head, up, (lookYaw * 0.85f + Mathf.Sin(t * 0.5f) * 2f) * I);
            Rot(head, right, (-lookPitch * 0.8f + nod - 8f * stg) * I);
        }

        // руки
        float gestureR = talkBlend * (1f - offerBlend) * (18f + Mathf.Sin(t * 2.6f) * 12f);
        float gestureL = talkBlend * (8f + Mathf.Sin(t * 2.1f + 1f) * 8f);
        if (armL != null)
        {
            Rot(armL, right, -(swing + gestureL) * I);
            Rot(armL, fwd, armL.side * (3f + sway * 1.2f + breathe * 0.8f + 22f * stg) * I);
        }
        if (armR != null)
        {
            Rot(armR, right, -(-swing + gestureR + offerBlend * 40f) * I);
            Rot(armR, fwd, armR.side * (3f - sway * 1.2f + breathe * 0.8f + 22f * stg) * I);
        }

        // ноги
        if (legL != null) Rot(legL, right, -(-swing * 0.9f) * I);
        if (legR != null) Rot(legR, right, -(swing * 0.9f) * I);
    }
}
