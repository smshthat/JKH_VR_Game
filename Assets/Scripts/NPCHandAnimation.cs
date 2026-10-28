using UnityEngine;

// Дополнение: NPC опускает руки вдоль тела и протягивает купюру рукой.
// Работает с ЛЮБОЙ человекоподобной моделью (Rig -> Animation Type -> Humanoid),
// клипы анимации и Animator Controller не нужны: руки двигаются кодом.
// Вешается на объект NPC (рядом со старым NPCController). Старые скрипты не меняются.
public class NPCHandAnimation : MonoBehaviour
{
    public bool giveWithRightHand = true;   // какой рукой отдаёт купюру (false = левой)
    public bool controlOtherArm = true;     // держать вторую руку опущенной вдоль тела
    public float reachSpeed = 2.5f;         // как быстро рука протягивается и убирается

    Animator animator;                      // Animator модели (находится сам)
    NPCController npc;                      // старый скрипт NPC, берём из него купюру
    float reach;                            // 0 = рука опущена, 1 = рука протянута

    Transform rUpper, rLower, rHand, lUpper, lLower, lHand;

    void Start()
    {
        npc = GetComponent<NPCController>();
        animator = GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman)
        {
            Debug.LogWarning("NPCHandAnimation: у модели нет Humanoid-аватара. Выберите FBX -> Rig -> Animation Type: Humanoid -> Apply.");
            enabled = false;
            return;
        }

        // Находим кости рук
        rUpper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rLower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        rHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        lUpper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        lLower = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        lHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        if (rUpper == null || rLower == null || rHand == null || lUpper == null || lLower == null || lHand == null)
        {
            Debug.LogWarning("NPCHandAnimation: не найдены кости рук у модели.");
            enabled = false;
        }
    }

    // LateUpdate выполняется после Animator, поэтому наши руки главнее
    void LateUpdate()
    {
        // Надо ли протягивать руку: купюра видна и ещё не взята
        bool reaching = npc != null && npc.money != null
                        && npc.money.gameObject.activeSelf && !npc.money.wasTaken;
        reach = Mathf.MoveTowards(reach, reaching ? 1f : 0f, reachSpeed * Time.deltaTime);

        // Куда смотрит и где право у персонажа (определяем по плечам, значит работает с любой моделью)
        Vector3 right = (rUpper.position - lUpper.position).normalized;
        Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;

        Transform handTargetObj = npc != null && npc.money != null ? npc.money.transform : null;

        // Рука, которая отдаёт купюру
        if (giveWithRightHand)
        {
            MoveArm(rUpper, rLower, rHand, right, forward, reach, handTargetObj);
            if (controlOtherArm) MoveArm(lUpper, lLower, lHand, -right, forward, 0f, null);
        }
        else
        {
            MoveArm(lUpper, lLower, lHand, -right, forward, reach, handTargetObj);
            if (controlOtherArm) MoveArm(rUpper, rLower, rHand, right, forward, 0f, null);
        }
    }

    // Двигает руку между "опущена" (weight 0) и "тянется к цели" (weight 1)
    void MoveArm(Transform upper, Transform lower, Transform hand, Vector3 side, Vector3 forward, float weight, Transform reachTarget)
    {
        Vector3 shoulder = upper.position;
        float a = Vector3.Distance(upper.position, lower.position);   // длина плеча
        float b = Vector3.Distance(lower.position, hand.position);    // длина предплечья

        // Цель 1: рука опущена вдоль тела
        Vector3 rest = shoulder + Vector3.down * ((a + b) * 0.95f) + side * 0.04f;
        // Цель 2: рука у купюры
        Vector3 target = rest;
        if (reachTarget != null) target = Vector3.Lerp(rest, reachTarget.position, weight);

        // Куда смотрит локоть: назад при опущенной руке, вниз при протянутой
        Vector3 poleRest = -forward + side * 0.3f;
        Vector3 poleReach = Vector3.down + side * 0.3f;
        Vector3 pole = Vector3.Lerp(poleRest, poleReach, weight).normalized;

        // Считаем, где должен быть локоть, чтобы рука достала до цели
        Vector3 toTarget = target - shoulder;
        float dist = Mathf.Clamp(toTarget.magnitude, 0.1f, (a + b) * 0.999f);
        Vector3 dir = toTarget.normalized;
        float cosA = Mathf.Clamp((a * a + dist * dist - b * b) / (2f * a * dist), -1f, 1f);
        float sinA = Mathf.Sqrt(1f - cosA * cosA);
        Vector3 perp = Vector3.ProjectOnPlane(pole, dir).normalized;
        Vector3 elbowPos = shoulder + dir * (a * cosA) + perp * (a * sinA);
        Vector3 handPos = shoulder + dir * dist;

        // Поворачиваем плечо к локтю, потом предплечье к кисти
        upper.rotation = Quaternion.FromToRotation(lower.position - shoulder, elbowPos - shoulder) * upper.rotation;
        lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, handPos - lower.position) * lower.rotation;
    }
}
