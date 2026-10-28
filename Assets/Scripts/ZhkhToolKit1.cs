using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;

// Ставит рядом со стартом игрока тумбу с предметами: фонарик, гаечный ключ, мячик.
// Предметы берутся рукой как остальные (Throwable), их можно положить в мешочек.
public class ZhkhToolKit : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSeconds(2.5f);
        float waited = 0f;
        while (ZhkhVR.Head() == null && waited < 15f) { waited += 1f; yield return new WaitForSeconds(1f); }
        Transform head = ZhkhVR.Head();
        if (head == null) yield break;
        if (GameObject.Find("ZhkhToolTable") != null) yield break;

        Vector3 f = head.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
        f.Normalize();
        Vector3 r = Vector3.Cross(Vector3.up, f);
        Vector3[] dirs = new Vector3[] { f, r, -r, -f };

        Vector3 chosenDir = f;
        Vector3 chosenPos = head.position + f * 1.5f;
        float chosenY = head.position.y - 1.6f;
        bool found = false;
        for (int i = 0; i < dirs.Length && !found; i++)
        {
            Vector3 c = head.position + dirs[i] * 1.5f;
            float gy;
            if (!ZhkhVR.Ground(new Vector3(c.x, head.position.y, c.z), 4f, out gy)) continue;
            if (gy < head.position.y - 2.4f || gy > head.position.y - 1.0f) continue;
            Vector3 center = new Vector3(c.x, gy + 0.6f, c.z);
            if (Physics.CheckBox(center, new Vector3(0.55f, 0.5f, 0.35f), Quaternion.LookRotation(-dirs[i]), ~0, QueryTriggerInteraction.Ignore)) continue;
            chosenDir = dirs[i]; chosenPos = c; chosenY = gy; found = true;
        }

        Quaternion rot = Quaternion.LookRotation(-chosenDir);
        GameObject root = new GameObject("ZhkhToolTable");
        root.transform.SetPositionAndRotation(new Vector3(chosenPos.x, chosenY, chosenPos.z), rot);
        ZhkhVR.Prim(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.8f, 0.45f), new Color(0.45f, 0.3f, 0.18f), true);
        TextMesh label = ZhkhVR.MakeText("ИНСТРУМЕНТЫ", root.transform, new Vector3(0f, 1.25f, 0f), 0.006f, Color.white, TextAnchor.MiddleCenter);
        label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        label.gameObject.AddComponent<ZhkhKeepVisible>();

        MakeFlashlight(root.transform.TransformPoint(new Vector3(-0.3f, 0.88f, 0f)), rot);
        MakeWrench(root.transform.TransformPoint(new Vector3(0f, 0.86f, 0f)), rot);
        MakeBall(root.transform.TransformPoint(new Vector3(0.3f, 0.9f, 0f)));
    }

    static GameObject NewItem(string name, Vector3 pos, Quaternion rot, float mass)
    {
        GameObject go = new GameObject(name);
        go.transform.SetPositionAndRotation(pos, rot);
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = mass;
        return go;
    }

    static void Finish(GameObject go)
    {
        Throwable th = go.AddComponent<Throwable>();
        Interactable it = go.GetComponent<Interactable>();
        if (it != null) it.highlightOnHover = false;
        // У компонентов, добавленных из кода, UnityEvent-поля бывают пустыми (null) -> NullReferenceException при взятии в руку.
        FixEvents(th);
        FixEvents(it);
    }

    static void FixEvents(Component c)
    {
        if (c == null) return;
        FieldInfo[] fs = c.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < fs.Length; i++)
        {
            FieldInfo f = fs[i];
            if (!typeof(UnityEventBase).IsAssignableFrom(f.FieldType)) continue;
            if (f.GetValue(c) != null) continue;
            try { f.SetValue(c, System.Activator.CreateInstance(f.FieldType)); } catch (System.Exception) { }
        }
    }

    static void MakeFlashlight(Vector3 pos, Quaternion rot)
    {
        GameObject go = NewItem("Фонарик", pos, rot, 0.4f);
        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.size = new Vector3(0.05f, 0.05f, 0.18f);
        ZhkhVR.Prim(PrimitiveType.Cylinder, go.transform, Vector3.zero, new Vector3(0.035f, 0.07f, 0.035f), new Color(0.15f, 0.15f, 0.18f), false, new Vector3(90f, 0f, 0f));
        ZhkhVR.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0f, 0.08f), new Vector3(0.05f, 0.015f, 0.05f), new Color(0.9f, 0.85f, 0.4f), false, new Vector3(90f, 0f, 0f));
        GameObject lg = new GameObject("Свет");
        lg.transform.SetParent(go.transform, false);
        lg.transform.localPosition = new Vector3(0f, 0f, 0.1f);
        Light l = lg.AddComponent<Light>();
        l.type = LightType.Spot;
        l.range = 20f;
        l.spotAngle = 55f;
        l.intensity = 3f;
        l.color = new Color(1f, 0.96f, 0.85f);
        l.enabled = false;
        Finish(go);
        go.AddComponent<ZhkhFlashlight>();
    }

    static void MakeWrench(Vector3 pos, Quaternion rot)
    {
        GameObject go = NewItem("Гаечный ключ", pos, rot, 0.6f);
        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.size = new Vector3(0.08f, 0.04f, 0.27f);
        bc.center = new Vector3(0f, 0f, 0.03f);
        Color steel = new Color(0.6f, 0.62f, 0.66f);
        ZhkhVR.Prim(PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(0.025f, 0.02f, 0.22f), steel, false);
        ZhkhVR.Prim(PrimitiveType.Cube, go.transform, new Vector3(0f, 0f, 0.12f), new Vector3(0.07f, 0.02f, 0.05f), steel, false);
        Finish(go);
        go.AddComponent<ZhkhWrench>();
    }

    static void MakeBall(Vector3 pos)
    {
        GameObject go = NewItem("Мячик", pos, Quaternion.identity, 0.2f);
        SphereCollider sc = go.AddComponent<SphereCollider>();
        sc.radius = 0.065f;
        ZhkhVR.Prim(PrimitiveType.Sphere, go.transform, Vector3.zero, new Vector3(0.13f, 0.13f, 0.13f), new Color(0.95f, 0.45f, 0.1f), false);
        Finish(go);
    }
}
