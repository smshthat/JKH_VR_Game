using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

// Общие помощники для новых скриптов ЖКХ (SteamVR Interaction System).
public static class ZhkhVR
{
    public static bool InputReady => Valve.VR.SteamVR.active && Valve.VR.SteamVR_Input.initialized;
    static Player player;
    static float nextFind;
    static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();
    static Material baseMat;
    static readonly RaycastHit[] hits = new RaycastHit[16];
    static Font font;

    public static Player P
    {
        get
        {
            if (player == null && Time.unscaledTime >= nextFind)
            {
                nextFind = Time.unscaledTime + 1f;
#if UNITY_2023_1_OR_NEWER
                player = Object.FindFirstObjectByType<Player>();
#else
                player = Object.FindObjectOfType<Player>();
#endif
            }
            return player;
        }
    }

    public static Transform Head()
    {
        Player p = P;
        if (p == null) return null;
        Transform h = p.hmdTransform;
        if (h != null) return h;
        return Camera.main != null ? Camera.main.transform : null;
    }

    // Высота земли под точкой (игнорирует коллайдеры игрока).
    public static bool Ground(Vector3 from, float dist, out float y)
    {
        y = 0f;
        Player p = P;
        int n = Physics.RaycastNonAlloc(from, Vector3.down, hits, dist, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        bool ok = false;
        for (int i = 0; i < n; i++)
        {
            Collider c = hits[i].collider;
            if (c == null) continue;
            if (p != null && c.transform.IsChildOf(p.transform)) continue;
            if (hits[i].distance < best)
            {
                best = hits[i].distance;
                y = hits[i].point.y;
                ok = true;
            }
        }
        return ok;
    }

    public static Material Mat(Color c)
    {
        Material m;
        if (!cache.TryGetValue(c, out m))
        {
            if (baseMat == null)
            {
                GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseMat = tmp.GetComponent<Renderer>().sharedMaterial;
                Object.Destroy(tmp);
            }
            m = new Material(baseMat);
            m.color = c;
            cache[c] = m;
        }
        return m;
    }

    public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 lp, Vector3 ls, Color c, bool collider, Vector3 euler = default(Vector3))
    {
        GameObject g = GameObject.CreatePrimitive(t);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = lp;
        g.transform.localScale = ls;
        g.transform.localRotation = Quaternion.Euler(euler);
        g.GetComponent<Renderer>().sharedMaterial = Mat(c);
        if (!collider)
        {
            Collider col = g.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }
        return g;
    }

    public static Font GetFont()
    {
        if (font == null)
            font = Font.CreateDynamicFontFromOSFont(new string[] { "Arial", "Segoe UI", "Liberation Sans" }, 64);
        return font;
    }

    public static TextMesh MakeText(string text, Transform parent, Vector3 localPos, float charSize, Color color, TextAnchor anchor)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        TextMesh tm = go.AddComponent<TextMesh>();
        tm.font = GetFont();
        tm.fontSize = 64;
        tm.characterSize = charSize;
        tm.anchor = anchor;
        tm.alignment = (anchor == TextAnchor.MiddleCenter) ? TextAlignment.Center : TextAlignment.Left;
        tm.color = color;
        tm.text = text;
        go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
        return tm;
    }
}
