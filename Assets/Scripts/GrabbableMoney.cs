using UnityEngine;
using Valve.VR.InteractionSystem;

// Купюра, которую можно взять рукой.
// Вешается на купюру вместе с Interactable, Throwable и Rigidbody (из ассета SteamVR).
public class GrabbableMoney : MonoBehaviour
{
    public bool wasTaken;           // true, когда игрок взял купюру

    Interactable interactable;
    Rigidbody rb;

    void Awake()
    {
        interactable = GetComponent<Interactable>();
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;      // пока купюра в руке NPC, она не падает
    }

    void Update()
    {
        // attachedToHand не пустой -> купюра у игрока в руке
        if (!wasTaken && interactable.attachedToHand != null)
        {
            wasTaken = true;
            rb.useGravity = true;   // когда игрок отпустит, купюра упадёт
        }
    }
}
