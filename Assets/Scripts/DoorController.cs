using UnityEngine;
using Valve.VR.InteractionSystem;

// Дверь открывается, когда игрок наводит руку и нажимает кнопку захвата (грип/триггер).
// Вешается на петлю двери вместе с компонентом Interactable (из ассета SteamVR).
public class DoorController : MonoBehaviour
{
    public float openAngle = -90f;  // на сколько градусов поворачивается дверь
    public float speed = 3f;        // скорость открывания

    bool isOpen;
    Quaternion closedRot;
    Quaternion openRot;

    void Start()
    {
        closedRot = transform.localRotation;
        openRot = closedRot * Quaternion.Euler(0, openAngle, 0);
    }

    // SteamVR сам вызывает этот метод каждый кадр, пока рука наведена на объект с Interactable
    void HandHoverUpdate(Hand hand)
    {
        // Кнопку захвата только что нажали -> открыть/закрыть
        if (hand.GetGrabStarting() != GrabTypes.None)
            isOpen = !isOpen;
    }

    void Update()
    {
        // Плавно поворачиваем дверь
        Quaternion target = isOpen ? openRot : closedRot;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, speed * Time.deltaTime);
    }
}
