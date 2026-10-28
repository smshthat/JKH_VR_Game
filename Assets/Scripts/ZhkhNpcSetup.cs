using System.Collections;
using UnityEngine;

// Страховка: каждые 10 секунд добавляет аниматор, руку и привязку купюры всем NPC (в том числе в других домах).
public class ZhkhNpcSetup : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);
        while (true)
        {
            NPCController[] all;
#if UNITY_2023_1_OR_NEWER
            all = FindObjectsByType<NPCController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            all = FindObjectsOfType<NPCController>(true);
#endif
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                GameObject g = all[i].gameObject;
                if (g.GetComponent<ZhkhNpcAnimator>() == null) g.AddComponent<ZhkhNpcAnimator>();
                if (g.GetComponent<ZhkhHandRig>() == null) g.AddComponent<ZhkhHandRig>();
                if (g.GetComponent<ZhkhBillHold>() == null) g.AddComponent<ZhkhBillHold>();
            }
            yield return new WaitForSeconds(10f);
        }
    }
}
