using UnityEngine;
using Valve.VR.InteractionSystem;

// Жилец: стоит на месте, поворачивается к игроку и протягивает купюру.
// Вешается на корневой объект NPC.
public class NPCController : MonoBehaviour
{
    public GrabbableMoney money;    // купюра
    public Transform moneyPoint;    // точка, где NPC держит купюру
    public float triggerDistance = 3f;  // с какого расстояния NPC замечает игрока
    public float turnSpeed = 3f;        // скорость поворота

    Transform head;                 // голова игрока (камера SteamVR)

    void Start()
    {
        head = Player.instance.hmdTransform;
        money.gameObject.SetActive(false);  // пока игрок далеко, купюры не видно
    }

    void Update()
    {
        Vector3 toPlayer = head.position - transform.position;
        toPlayer.y = 0;

        if (toPlayer.magnitude < triggerDistance && toPlayer.sqrMagnitude > 0.001f)
        {
            // Поворачиваемся к игроку
            Quaternion target = Quaternion.LookRotation(toPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);

            // Показываем купюру
            if (!money.wasTaken) money.gameObject.SetActive(true);
        }

        // Пока купюру не взяли, она остаётся в руке NPC
        if (money.gameObject.activeSelf && !money.wasTaken)
        {
            money.transform.position = moneyPoint.position;
            money.transform.rotation = moneyPoint.rotation;
        }
    }
}
