using UnityEngine;
using Valve.VR.InteractionSystem;

// Разговор с NPC: поднесите руку к NPC и нажмите грип/курок, над головой появится реплика.
// Ударов и драк больше нет. Вешается на NPC вместе с Interactable.
public class NPCInteraction : MonoBehaviour
{
    public string[] talkLines =
    {
        "Здравствуйте!",
        "Деньги за ЖКХ я приготовил.",
        "Квитанцию положите на стол.",
        "Спасибо, что зашли."
    };

    public float lineTime = 3f;        // сколько секунд видна реплика
    public int hitCount;               // оставлено для совместимости, больше не меняется

    Transform head;
    TextMesh bubble;
    float lineTimer;
    int talkIndex;

    void Start()
    {
        head = Player.instance.hmdTransform;

        GameObject b = new GameObject("Реплика");
        b.transform.SetParent(transform);
        b.transform.localPosition = new Vector3(0, 2.1f, 0);
        bubble = b.AddComponent<TextMesh>();
        Font font = SimpleSound.TextFont;
        bubble.font = font;
        b.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        bubble.fontSize = 100;
        bubble.characterSize = 0.015f;
        bubble.anchor = TextAnchor.MiddleCenter;
        bubble.alignment = TextAlignment.Center;
        bubble.color = Color.yellow;
        bubble.text = "";
    }

    // SteamVR вызывает этот метод, пока рука наведена на NPC
    void HandHoverUpdate(Hand hand)
    {
        if (hand.GetGrabStarting() != GrabTypes.None)
        {
            bubble.text = talkLines[talkIndex];
            lineTimer = lineTime;
            talkIndex = (talkIndex + 1) % talkLines.Length;
        }
    }

    void Update()
    {
        if (lineTimer > 0f)
        {
            lineTimer -= Time.deltaTime;
            if (lineTimer <= 0f) bubble.text = "";
        }
        Vector3 away = bubble.transform.position - head.position;
        if (away.sqrMagnitude > 0.001f)
            bubble.transform.rotation = Quaternion.LookRotation(away);
    }
}
