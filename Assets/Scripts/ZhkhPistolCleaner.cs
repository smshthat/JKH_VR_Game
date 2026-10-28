using System.Collections;
using UnityEngine;

// Страховка: один раз после старта убирает пистолет, если он остался в сцене.
public class ZhkhPistolCleaner : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);
        MonoBehaviour[] all;
#if UNITY_2023_1_OR_NEWER
        all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        all = FindObjectsOfType<MonoBehaviour>(true);
#endif
        for (int i = 0; i < all.Length; i++)
        {
            MonoBehaviour m = all[i];
            if (m != null && m.GetType().Name == "PistolController")
                Destroy(m.gameObject);
        }
    }
}
