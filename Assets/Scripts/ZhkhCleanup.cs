using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

// Чистка после сторонней «достройки»:
//  1) убирает перчатки на руках (объекты «Перчатка…» и скрипты с «glove» в названии) и возвращает стандартные модели рук SteamVR;
//  2) отключает второй, чужой инвентарь, оставляя один (ZhkhInventoryScreen);
//  3) чинит экран «СМЕНА ЗАВЕРШЕНА»: текст рисуется поверх плашек.
// Что именно найдено и отключено, пишется в Console строками «ЖКХ-чистка: ...».
public class ZhkhCleanup : MonoBehaviour
{
    readonly HashSet<string> loggedTypes = new HashSet<string>();
    readonly HashSet<int> doneScreens = new HashSet<int>();
    float nextScreen;

    IEnumerator Start()
    {
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(2f);
            DisableScripts();
            DestroyNamed(i < 25);
            if (i == 1 || i == 2) ShowHands();
        }
    }

    void Update()
    {
        if (Time.unscaledTime < nextScreen) return;
        nextScreen = Time.unscaledTime + 1.5f;
        FindShiftScreens();
    }

    static MonoBehaviour[] AllBehaviours()
    {
#if UNITY_2023_1_OR_NEWER
        return FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return FindObjectsOfType<MonoBehaviour>(true);
#endif
    }

    static bool IsSteamVRStuff(Transform t)
    {
        MonoBehaviour[] ms = t.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < ms.Length; i++)
        {
            if (ms[i] == null) continue;
            string n = ms[i].GetType().Name;
            if (n.StartsWith("SteamVR_") || n == "Hand") return true;
        }
        return false;
    }

    // Нельзя удалять модели рук SteamVR: всё, где есть SkinnedMeshRenderer, и всё внутри объектов SteamVR/Hand/Player
    static bool IsProtected(Transform t)
    {
        if (t.GetComponentInChildren<SkinnedMeshRenderer>(true) != null) return true;
        for (Transform p = t; p != null; p = p.parent)
        {
            string n = p.name.ToLowerInvariant();
            if (n.StartsWith("vr_") || n.Contains("rendermodel") || n.Contains("render model")) return true;
            MonoBehaviour[] ms = p.GetComponents<MonoBehaviour>();
            for (int i = 0; i < ms.Length; i++)
            {
                if (ms[i] == null) continue;
                string tn = ms[i].GetType().Name;
                if (tn.StartsWith("SteamVR_") || tn == "Hand" || tn == "Player") return true;
            }
        }
        return false;
    }

    static bool HasPouch(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent)
        {
            MonoBehaviour[] ms = p.GetComponents<MonoBehaviour>();
            for (int i = 0; i < ms.Length; i++) if (ms[i] != null && ms[i].GetType().Name == "PouchInventory") return true;
        }
        MonoBehaviour[] cs = t.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < cs.Length; i++) if (cs[i] != null && cs[i].GetType().Name == "PouchInventory") return true;
        return false;
    }

    void DisableScripts()
    {
        MonoBehaviour[] all = AllBehaviours();
        for (int i = 0; i < all.Length; i++)
        {
            MonoBehaviour m = all[i];
            if (m == null || !m.enabled) continue;
            string tn = m.GetType().Name;
            string l = tn.ToLowerInvariant();
            if (l.StartsWith("zhkh") || l.StartsWith("steamvr")) continue;
            bool glove = l.Contains("glove") || l.Contains("перчат") || l.Contains("handvisual");
            bool inventory = l.Contains("inventory") && l != "pouchinventory";
            if (!glove && !inventory) continue;
            m.enabled = false;
            if (loggedTypes.Add(tn)) Debug.Log("ЖКХ-чистка: отключён скрипт " + tn + " на объекте " + m.gameObject.name);
        }
    }

    void DestroyNamed(bool includeInventories)
    {
        Transform[] ts;
#if UNITY_2023_1_OR_NEWER
        ts = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        ts = FindObjectsOfType<Transform>(true);
#endif
        for (int i = 0; i < ts.Length; i++)
        {
            Transform t = ts[i];
            if (t == null) continue;
            string n = t.name.ToLowerInvariant();
            if (n.StartsWith("zhkh")) continue;
            // «Перчатка» из сторонней достройки (без SkinnedMeshRenderer) удаляем; английское glove только вне объектов SteamVR
            bool glove = (n.Contains("перчат") && t.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
                || (n.Contains("glove") && !IsProtected(t));
            bool inv = includeInventories && (n.Contains("инвентар") || n.Contains("inventory")) && !HasPouch(t);
            if (!glove && !inv) continue;
            Debug.Log("ЖКХ-чистка: удалён объект «" + t.name + "»");
            Destroy(t.gameObject);
        }
    }

    void ShowHands()
    {
        Player p = ZhkhVR.P;
        if (p == null || p.hands == null) return;
        for (int i = 0; i < p.hands.Length; i++)
        {
            Hand h = p.hands[i];
            if (h == null) continue;
            try { h.Show(); } catch (System.Exception) { }
        }
    }

    // ---------- экран «СМЕНА ЗАВЕРШЕНА» ----------

    void FindShiftScreens()
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
            if (tm == null || string.IsNullOrEmpty(tm.text)) continue;
            if (tm.text.IndexOf("ЗАВЕРШЕН", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            Transform root = ScreenRoot(tm.transform);
            if (root == null || !doneScreens.Add(root.GetInstanceID())) continue;
            Debug.Log("ЖКХ-чистка: починен экран конца смены «" + root.name + "»");
            StartCoroutine(FixScreen(root));
        }
    }

    static Transform ScreenRoot(Transform t)
    {
        Transform best = t;
        int levels = 0;
        for (Transform p = t.parent; p != null && levels < 3; p = p.parent, levels++)
        {
            if (p.GetComponent<Camera>() != null || p.GetComponent<Player>() != null) break;
            if (p.GetComponentsInChildren<Renderer>(true).Length > 60) break;
            best = p;
        }
        return best;
    }

    IEnumerator FixScreen(Transform root)
    {
        HashSet<Renderer> converted = new HashSet<Renderer>();
        for (int k = 0; k < 60 && root != null; k++)
        {
            Apply(root, converted);
            yield return new WaitForSeconds(0.5f);
        }
    }

    static void Apply(Transform root, HashSet<Renderer> converted)
    {
        Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
        int order = 0;
        for (int i = 0; i < rs.Length; i++)
        {
            Renderer r = rs[i];
            if (r == null) continue;
            if (r.GetComponent<TextMesh>() != null) { r.sortingOrder = 300; continue; }
            MeshRenderer mr = r as MeshRenderer;
            if (mr == null) continue;
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            string mn = mf.sharedMesh.name;
            Vector3 s = r.bounds.size;
            float thin = Mathf.Min(s.x, Mathf.Min(s.y, s.z));
            bool flat = mn == "Quad" || mn == "Plane" || (mn == "Cube" && thin < 0.08f);
            if (!flat) continue;
            if (converted.Add(r))
            {
                Color c = Color.white;
                Material m = r.sharedMaterial;
                if (m != null && m.HasProperty("_Color")) c = m.GetColor("_Color");
                else if (m != null && m.HasProperty("_BaseColor")) c = m.GetColor("_BaseColor");
                if (c.a < 0.05f) c.a = 1f;
                r.sharedMaterial = ZhkhUI.UIMat(c);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            r.sortingOrder = Mathf.Min(order++, 250);
        }
    }
}
