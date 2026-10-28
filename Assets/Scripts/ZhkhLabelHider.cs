using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

// Когда игрок под крышей/потолком, прячет высокие надписи над домами (улица).
// Надписи NPC, мешочка, доски задач и всё помеченное ZhkhKeepVisible остаются.
public class ZhkhLabelHider : MonoBehaviour
{
    public float minHeightAbsolute = 3f;     // прячем только то, что выше этого (м)
    public float minAboveHead = 1.5f;        // и выше головы минимум на столько (м)
    public float minRoofSize = 2.5f;         // потолком считается плоскость не меньше такой (м)

    class Entry { public TextMesh label; public Renderer rend; public bool candidate; public bool weHid; }

    readonly List<Entry> entries = new List<Entry>();
    readonly HashSet<TextMesh> known = new HashSet<TextMesh>();
    static readonly List<MonoBehaviour> tmpMb = new List<MonoBehaviour>();
    static readonly RaycastHit[] hits = new RaycastHit[16];
    float nextScan, nextCheck;

    void Update()
    {
        float now = Time.unscaledTime;
        if (now < nextCheck) return;
        nextCheck = now + 0.25f;
        Transform head = ZhkhVR.Head();
        if (head == null) return;
        if (now >= nextScan) { nextScan = now + 3f; Scan(); }

        bool inside = IsInside(head);
        float hy = head.position.y;
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry e = entries[i];
            if (e.label == null) { known.Remove(e.label); entries.RemoveAt(i); continue; }
            if (!e.candidate || e.rend == null) continue;
            float y = e.label.transform.position.y;
            bool hide = inside && y > minHeightAbsolute && y > hy + minAboveHead;
            if (hide && !e.weHid) { e.rend.enabled = false; e.weHid = true; }
            else if (!hide && e.weHid) { e.rend.enabled = true; e.weHid = false; }
        }
    }

    void Scan()
    {
        TextMesh[] all;
#if UNITY_2023_1_OR_NEWER
        all = FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
#else
        all = FindObjectsOfType<TextMesh>();
#endif
        for (int i = 0; i < all.Length; i++)
        {
            TextMesh tm = all[i];
            if (tm == null || !known.Add(tm)) continue;
            Entry e = new Entry();
            e.label = tm;
            e.rend = tm.GetComponent<MeshRenderer>();
            e.candidate = IsCandidate(tm.transform);
            entries.Add(e);
        }
    }

    bool IsCandidate(Transform t)
    {
        Player p = ZhkhVR.P;
        if (p != null && t.IsChildOf(p.transform)) return false;
        for (Transform c = t; c != null; c = c.parent)
        {
            if (c.GetComponent<ZhkhKeepVisible>() != null) return false;
            c.GetComponents(tmpMb);
            for (int k = 0; k < tmpMb.Count; k++)
            {
                MonoBehaviour m = tmpMb[k];
                if (m == null) continue;
                string n = m.GetType().Name;
                if (n.StartsWith("NPC") || n == "PouchInventory" || n.StartsWith("Zhkh")) return false;
            }
        }
        return true;
    }

    bool IsInside(Transform head)
    {
        Player p = ZhkhVR.P;
        int n = Physics.RaycastNonAlloc(head.position, Vector3.up, hits, 12f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            Collider c = hits[i].collider;
            if (c == null) continue;
            if (p != null && c.transform.IsChildOf(p.transform)) continue;
            if (c is SphereCollider || c is CapsuleCollider) continue; // кроны деревьев и т.п.
            Vector3 s = c.bounds.size;
            if (s.x >= minRoofSize && s.z >= minRoofSize) return true;
        }
        return false;
    }
}
