using UnityEngine;
using Valve.VR.InteractionSystem;

// Универсальная анимация NPC для ЛЮБОЙ гуманоидной модели (Rig -> Animation Type -> Humanoid).
// Ничего не нужно настраивать: размеры рук и направление «вперёд» определяются по самой модели.
//  - дыхание и лёгкое покачивание корпуса;
//  - голова плавно поворачивается к игроку;
//  - руки опущены вдоль тела; когда игрок подходит, NPC наклоняется и протягивает руку с купюрой;
//  - купюра лежит именно в руке (идёт за костью кисти) и слегка покачивается.
// Скрипт добавляется ВСЕМ NPC автоматически при запуске игры (и меню сборки тоже добавляет его).
// Заменяет старый NPCHandAnimation. Вешается на объект NPC.
[DefaultExecutionOrder(200)]
public class NPCAnimator : MonoBehaviour
{
    public bool giveWithRightHand = true;   // какой рукой отдаёт купюру
    public float reachSpeed = 1.8f;         // скорость протягивания руки
    public float breathing = 1.2f;          // сила дыхания (градусы)
    public float leanForward = 5f;          // наклон корпуса вперёд при подаче (градусы)
    public float lookStrength = 0.9f;       // насколько голова следит за игроком (0..1)
    public float maxLookAngle = 55f;        // максимальный поворот головы
    public float lookDistance = 6f;         // с какого расстояния NPC смотрит на игрока

    NPCController npc;
    Animator animator;
    Transform playerHead;
    Transform spine, chest, neck, headBone;
    Transform rUpper, rLower, rHand, lUpper, lLower, lHand;
    float reach;
    Quaternion lookDelta = Quaternion.identity;

    // Каждому NPC в сцене добавляем этот скрипт, а старый NPCHandAnimation убираем
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AddToAllNpcs()
    {
        NPCController[] all = FindObjectsByType<NPCController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (NPCController c in all)
        {
            foreach (MonoBehaviour mb in c.GetComponents<MonoBehaviour>())
                if (mb != null && mb.GetType().Name == "NPCHandAnimation") Destroy(mb);
            // One animation owner per skeleton; the adaptive rig also supports generic models.
            var old = c.GetComponent<NPCAnimator>();
            if (old != null) old.enabled = false;
            if (c.GetComponent<ZhkhNpcAnimator>() == null) c.gameObject.AddComponent<ZhkhNpcAnimator>();
            if (c.GetComponent<ZhkhHandRig>() == null) c.gameObject.AddComponent<ZhkhHandRig>();
            if (c.GetComponent<ZhkhBillHold>() == null) c.gameObject.AddComponent<ZhkhBillHold>();
        }
    }

    void Start()
    {
        npc = GetComponent<NPCController>();
        animator = GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman)
        {
            // Нет Humanoid-модели: NPC остаётся как был (купюра в точке перед ним)
            enabled = false;
            return;
        }

        playerHead = Player.instance.hmdTransform;
        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        neck = animator.GetBoneTransform(HumanBodyBones.Neck);
        headBone = animator.GetBoneTransform(HumanBodyBones.Head);
        rUpper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rLower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        rHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        lUpper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        lLower = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        lHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        if (rUpper == null || rLower == null || rHand == null || lUpper == null || lLower == null || lHand == null)
        {
            Debug.LogWarning("NPCAnimator: у модели не найдены кости рук (" + name + ").");
            enabled = false;
            return;
        }

        // Купюра не должна толкаться с телом NPC
        if (npc != null && npc.money != null)
        {
            Collider[] npcCols = GetComponentsInChildren<Collider>();
            Collider[] moneyCols = npc.money.GetComponentsInChildren<Collider>(true);
            foreach (Collider a in npcCols)
                foreach (Collider b in moneyCols)
                    if (a != null && b != null) Physics.IgnoreCollision(a, b);
        }
    }

    // LateUpdate идёт после Animator и после NPCController, поэтому наши движения главнее
    void LateUpdate()
    {
        bool reaching = npc != null && npc.money != null
                        && npc.money.gameObject.activeSelf && !npc.money.wasTaken;
        reach = Mathf.MoveTowards(reach, reaching ? 1f : 0f, reachSpeed * Time.deltaTime);
        float e = Mathf.SmoothStep(0f, 1f, reach);        // плавное начало и конец движения

        Vector3 right = (rUpper.position - lUpper.position).normalized;
        Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;

        Breathe(right, e);
        LookAtPlayer(forward);

        right = (rUpper.position - lUpper.position).normalized;
        forward = Vector3.Cross(right, Vector3.up).normalized;

        Transform gu = giveWithRightHand ? rUpper : lUpper;
        Transform gl = giveWithRightHand ? rLower : lLower;
        Transform gh = giveWithRightHand ? rHand : lHand;
        Vector3 giveSide = giveWithRightHand ? right : -right;

        Transform ou = giveWithRightHand ? lUpper : rUpper;
        Transform ol = giveWithRightHand ? lLower : rLower;
        Transform oh = giveWithRightHand ? lHand : rHand;

        float armLen = Vector3.Distance(gu.position, gl.position) + Vector3.Distance(gl.position, gh.position);
        Vector3 shoulderMid = (rUpper.position + lUpper.position) * 0.5f;

        // Куда тянется рука: вперёд от груди на длину руки, чуть ниже плеч, с лёгким покачиванием
        Vector3 reachPos = shoulderMid + forward * (armLen * 0.75f) + Vector3.down * (armLen * 0.35f)
                           + giveSide * 0.1f + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.012f * e);

        MoveArm(gu, gl, gh, giveSide, forward, e, reachPos);
        MoveArm(ou, ol, oh, -giveSide, forward, 0f, Vector3.zero);

        // Купюра в руке: идёт за костью кисти и чуть покачивается
        if (reaching)
        {
            Transform m = npc.money.transform;
            m.position = gh.position + forward * 0.05f + Vector3.up * 0.02f;
            m.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 2.2f) * 4f * e);
        }
    }

    // Дыхание и наклон корпуса вперёд, когда NPC протягивает руку
    void Breathe(Vector3 right, float e)
    {
        float a = Mathf.Sin(Time.time * 1.6f) * breathing + leanForward * e;
        if (spine != null) spine.rotation = Quaternion.AngleAxis(a, right) * spine.rotation;
        if (chest != null) chest.rotation = Quaternion.AngleAxis(a * 0.5f, right) * chest.rotation;
    }

    // Голова поворачивается к игроку (считается от направления тела, поэтому работает на любой модели)
    void LookAtPlayer(Vector3 forward)
    {
        Transform h = headBone != null ? headBone : neck;
        if (h == null || playerHead == null) return;

        Vector3 toPlayer = playerHead.position - h.position;
        if (toPlayer.magnitude < lookDistance)
        {
            Quaternion target = Quaternion.FromToRotation(forward, toPlayer.normalized);
            target = Quaternion.RotateTowards(Quaternion.identity, target, maxLookAngle);
            lookDelta = Quaternion.Slerp(lookDelta, target, 5f * Time.deltaTime);
        }
        else
        {
            lookDelta = Quaternion.Slerp(lookDelta, Quaternion.identity, 3f * Time.deltaTime);
        }

        Quaternion applied = Quaternion.Slerp(Quaternion.identity, lookDelta, lookStrength);
        if (neck != null && headBone != null)
        {
            neck.rotation = Quaternion.Slerp(Quaternion.identity, applied, 0.4f) * neck.rotation;
            headBone.rotation = Quaternion.Slerp(Quaternion.identity, applied, 0.6f) * headBone.rotation;
        }
        else
        {
            h.rotation = applied * h.rotation;
        }
    }

    // Рука между «опущена» (weight 0) и «тянется к цели» (weight 1)
    void MoveArm(Transform upper, Transform lower, Transform hand, Vector3 side, Vector3 forward, float weight, Vector3 reachTarget)
    {
        Vector3 shoulder = upper.position;
        float a = Vector3.Distance(upper.position, lower.position);
        float b = Vector3.Distance(lower.position, hand.position);

        // Опущенная рука с лёгким покачиванием
        Vector3 rest = shoulder + Vector3.down * ((a + b) * 0.95f) + side * 0.04f
                       + forward * (Mathf.Sin(Time.time * 1.1f) * 0.012f);
        Vector3 target = weight > 0f ? Vector3.Lerp(rest, reachTarget, weight) : rest;

        Vector3 poleRest = -forward + side * 0.3f;
        Vector3 poleReach = Vector3.down + side * 0.3f;
        Vector3 pole = Vector3.Lerp(poleRest, poleReach, weight).normalized;

        Vector3 toTarget = target - shoulder;
        float dist = Mathf.Clamp(toTarget.magnitude, 0.1f, (a + b) * 0.999f);
        Vector3 dir = toTarget.normalized;
        float cosA = Mathf.Clamp((a * a + dist * dist - b * b) / (2f * a * dist), -1f, 1f);
        float sinA = Mathf.Sqrt(1f - cosA * cosA);
        Vector3 perp = Vector3.ProjectOnPlane(pole, dir).normalized;
        Vector3 elbowPos = shoulder + dir * (a * cosA) + perp * (a * sinA);
        Vector3 handPos = shoulder + dir * dist;

        upper.rotation = Quaternion.FromToRotation(lower.position - shoulder, elbowPos - shoulder) * upper.rotation;
        lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, handPos - lower.position) * lower.rotation;
    }
}
