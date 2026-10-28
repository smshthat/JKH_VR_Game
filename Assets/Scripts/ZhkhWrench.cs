using UnityEngine;
using Valve.VR.InteractionSystem;

// Гаечный ключ для починки протечек.
public class ZhkhWrench : MonoBehaviour
{
    Interactable inter;
    void Awake() { inter = GetComponent<Interactable>(); }
    public bool IsHeld { get { return inter != null && inter.attachedToHand != null; } }
}
