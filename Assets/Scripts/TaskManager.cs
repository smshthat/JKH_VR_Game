using UnityEngine;
using Valve.VR.InteractionSystem;

// Единственная задача игры и надпись в мире.
// Вешается на пустой объект TaskManager.
public class TaskManager : MonoBehaviour
{
    public GrabbableMoney money;    // купюра
    public TextMesh taskText;       // надпись в мире

    bool done;
    Transform head;

    void Start()
    {
        head = Player.instance.hmdTransform;

        // Шрифт Arial из системы, чтобы работала кириллица
        Font font = Font.CreateDynamicFontFromOSFont("Arial", 64);
        taskText.font = font;
        taskText.GetComponent<MeshRenderer>().sharedMaterial = font.material;

        taskText.text = "Возьмите оплату за ЖКХ";
    }

    void Update()
    {
        // Игрок взял деньги -> меняем надпись
        if (!done && money.wasTaken)
        {
            done = true;
            taskText.text = "Задача выполнена";
        }

        // Надпись всегда повёрнута к игроку
        Vector3 away = taskText.transform.position - head.position;
        taskText.transform.rotation = Quaternion.LookRotation(away);
    }
}
