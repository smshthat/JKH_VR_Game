using UnityEngine;
using Valve.VR.InteractionSystem;

// Задача: перекрыть вентиль на трубе, которая течёт.
// Подносите руку к вентилю и нажимайте грип или курок: каждое нажатие поворачивает вентиль на четверть оборота.
// Вешается на объект Вентиль вместе с Interactable.
public class ValveTask : MonoBehaviour
{
    public Transform wheel;             // колесо вентиля (оно вращается)
    public GameObject leak;             // вода, которая пропадёт, когда вентиль закрыт
    public int pressesNeeded = 4;       // сколько нажатий нужно
    public float turnPerPress = 90f;    // на сколько градусов поворачивается за нажатие

    public bool done;                   // задача выполнена

    int presses;
    float targetAngle;
    float currentAngle;

    // SteamVR вызывает этот метод, пока рука наведена на вентиль
    void HandHoverUpdate(Hand hand)
    {
        if (done) return;
        if (hand.GetGrabStarting() != GrabTypes.None)
        {
            presses++;
            targetAngle += turnPerPress;
            if (presses >= pressesNeeded)
            {
                done = true;
                if (leak != null) leak.SetActive(false);
            }
        }
    }

    void Update()
    {
        // Плавно крутим колесо к нужному углу
        float next = Mathf.MoveTowards(currentAngle, targetAngle, 180f * Time.deltaTime);
        wheel.Rotate(0f, next - currentAngle, 0f, Space.Self);
        currentAngle = next;
    }
}
