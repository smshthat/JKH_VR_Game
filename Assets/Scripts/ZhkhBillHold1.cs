using UnityEngine;
using Valve.VR.InteractionSystem;

// Привязывает купюру NPC (NPCController.money) к кисти правой руки.
// Купюра следует за рукой каждый кадр, пока её не взяли. После взятия игроком или если она выпала, отпускается.
[DefaultExecutionOrder(200)]
public class ZhkhBillHold : MonoBehaviour
{
    NPCController ctrl;
    ZhkhNpcAnimator anim;
    ZhkhHandRig rig;
    Transform money;
    Interactable moneyInter;
    Rigidbody moneyRb;
    Quaternion relRot;
    bool captured;

    void Start()
    {
        ctrl = GetComponent<NPCController>();
        anim = GetComponent<ZhkhNpcAnimator>();
    }

    void LateUpdate()
    {
        if (ctrl == null || ctrl.money == null) { SetOffer(false); return; }
        if (anim == null) { anim = GetComponent<ZhkhNpcAnimator>(); if (anim == null) return; }
        if (rig == null) rig = GetComponent<ZhkhHandRig>();
        if (!anim.Ready || anim.handAnchor == null) return;
        if (rig != null && !rig.Ready) return;
        Transform hp = (rig != null && rig.HoldPoint != null) ? rig.HoldPoint : anim.handAnchor;

        GameObject mg = ctrl.money.gameObject;
        if (money != ctrl.money.transform)
        {
            money = ctrl.money.transform;
            moneyInter = mg.GetComponent<Interactable>();
            moneyRb = mg.GetComponent<Rigidbody>();
            captured = false;
        }

        bool hold = mg.activeInHierarchy && !ctrl.money.wasTaken
            && (moneyInter == null || moneyInter.attachedToHand == null);
        if (!hold) { SetOffer(false); captured = false; return; }

        if (!captured) { relRot = Quaternion.Inverse(transform.rotation) * money.rotation; captured = true; }
        if (rig != null && rig.HoldPoint != null) money.position = hp.position;
        else money.position = hp.position + transform.forward * 0.05f + transform.up * 0.02f;
        money.rotation = transform.rotation * relRot;
        SetOffer(true);
    }

    void SetOffer(bool v)
    {
        if (rig != null) rig.offer = v;
        else if (anim != null) anim.offering = v;
    }
}
