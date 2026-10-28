using UnityEngine;
using Valve.VR.InteractionSystem;

// Доска заданий: показывает список задач смены и отмечает выполненные.
// Вешается на объект Доска_заданий (его создаёт меню ЖКХ -> Добавить задачи и декор).
public class TaskBoard : MonoBehaviour
{
    public TextMesh text;               // текст на доске
    public TaskZone bulbZone;           // лампочка
    public TaskZone trashZone;          // мусор
    public TaskZone mailZone;           // почтовый ящик
    public ValveTask valve;             // вентиль

    NPCController[] npcs;
    Transform head;
    string last = "";
    float nextStatusRefresh;

    void Start()
    {
        head = Player.instance.hmdTransform;
        npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);

        // Шрифт Arial из системы, чтобы работала кириллица
        Font font = Font.CreateDynamicFontFromOSFont("Arial", 64);
        text.font = font;
        text.GetComponent<MeshRenderer>().sharedMaterial = font.material;
    }

    void Update()
    {
        // Keep the board facing the player every frame; rebuild status text at 5 Hz.
        if (head != null)
        {
            Vector3 facing = transform.position - head.position;
            if (facing.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(facing);
        }
        if (Time.unscaledTime < nextStatusRefresh) return;
        nextStatusRefresh = Time.unscaledTime + 0.2f;
        // Сколько жильцов уже отдали деньги
        int total = npcs.Length;
        int taken = 0;
        foreach (NPCController n in npcs)
            if (n != null && n.money != null && n.money.wasTaken) taken++;

        bool moneyDone = total > 0 && taken >= total;
        bool bulbDone = bulbZone != null && bulbZone.done;
        bool trashDone = trashZone != null && trashZone.done;
        bool valveDone = valve != null && valve.done;
        bool mailDone = mailZone != null && mailZone.done;

        string s = "ЗАДАНИЯ НА СМЕНУ\n"
                 + Mark(moneyDone) + " Собрать оплату: " + taken + "/" + total + "\n"
                 + Mark(bulbDone) + " Заменить лампочку у подъезда\n"
                 + Mark(trashDone) + " Выбросить мусор в бак\n"
                 + Mark(valveDone) + " Перекрыть вентиль на трубе\n"
                 + Mark(mailDone) + " Отнести квитанцию в ящик";
        if (moneyDone && bulbDone && trashDone && valveDone && mailDone)
            s += "\n\nСмена закончена. Отлично!";

        if (s != last)
        {
            last = s;
            text.text = s;
        }

        // Доска всегда повёрнута к игроку
        Vector3 away = transform.position - head.position;
        if (away.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(away);
    }

    string Mark(bool done)
    {
        return done ? "[x]" : "[ ]";
    }
}
