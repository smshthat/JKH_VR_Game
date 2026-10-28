using System.Collections.Generic;
using UnityEngine;

// Помощники для красивых панелей (непрозрачные цветные плашки + текст).
public static class ZhkhUI
{
    static readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();

    public static Material UIMat(Color c)
    {
        Material m;
        if (!mats.TryGetValue(c, out m))
        {
            Shader s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Unlit/Color");
            m = new Material(s);
            m.color = c;
            mats[c] = m;
        }
        return m;
    }

    public static GameObject Quad(Transform parent, Vector3 lp, float w, float h, Color c, int order, bool collider)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Quad);
        g.name = "UI";
        Collider old = g.GetComponent<Collider>();
        if (old != null) Object.Destroy(old);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = lp;
        g.transform.localScale = new Vector3(w, h, 1f);
        if (collider)
        {
            BoxCollider bc = g.AddComponent<BoxCollider>();
            bc.size = new Vector3(1f, 1f, 0.1f);
        }
        Renderer r = g.GetComponent<Renderer>();
        r.sharedMaterial = UIMat(c);
        r.sortingOrder = order;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return g;
    }

    public static void SetColor(GameObject g, Color c)
    {
        Renderer r = g.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = UIMat(c);
    }

    public static TextMesh Text(Transform parent, string s, Vector3 lp, float size, Color c, TextAnchor a, int order)
    {
        GameObject go = new GameObject("T");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = lp;
        TextMesh tm = go.AddComponent<TextMesh>();
        tm.font = ZhkhVR.GetFont();
        tm.fontSize = 64;
        tm.characterSize = size;
        tm.anchor = a;
        if (a == TextAnchor.MiddleCenter || a == TextAnchor.UpperCenter || a == TextAnchor.LowerCenter) tm.alignment = TextAlignment.Center;
        else if (a == TextAnchor.MiddleRight || a == TextAnchor.UpperRight || a == TextAnchor.LowerRight) tm.alignment = TextAlignment.Right;
        else tm.alignment = TextAlignment.Left;
        tm.color = c;
        tm.text = s;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = tm.font.material;
        mr.sortingOrder = order;
        return tm;
    }

    public static string Wrap(string s, int max)
    {
        string[] words = s.Split(' ');
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int len = 0;
        for (int i = 0; i < words.Length; i++)
        {
            string w = words[i];
            if (len > 0 && len + 1 + w.Length > max) { sb.Append('\n'); len = 0; }
            if (len > 0) { sb.Append(' '); len++; }
            sb.Append(w);
            len += w.Length;
        }
        return sb.ToString();
    }
}
