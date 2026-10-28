using UnityEngine;
using Valve.VR.InteractionSystem;

// Фонарик: взяли в руку (грипом или курком) и нажмите ДРУГУЮ кнопку, чтобы включить/выключить.
public class ZhkhFlashlight : MonoBehaviour
{
    Interactable inter;
    Light lamp;

    void Awake()
    {
        inter = GetComponent<Interactable>();
        lamp = GetComponentInChildren<Light>(true);
    }

    void Update()
    {
        if (inter == null || lamp == null || !ZhkhVR.InputReady) return;
        Hand h = inter.attachedToHand;
        if (h == null || h.grabPinchAction == null || h.grabGripAction == null) return;
        var src = h.handType;
        bool pinch = h.grabPinchAction.GetState(src);
        bool grip = h.grabGripAction.GetState(src);
        bool toggle = (h.grabPinchAction.GetStateDown(src) && grip) || (h.grabGripAction.GetStateDown(src) && pinch);
        if (toggle) lamp.enabled = !lamp.enabled;
    }
}
