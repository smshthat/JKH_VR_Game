using System.Collections;
using UnityEngine;

// Против мерцания полов (z-fighting) и для чёткой картинки в VR:
// ближняя плоскость камеры 0.1 м и дальняя 500 м вместо 0.01/1000 сильно улучшают точность глубины.
public class ZhkhRenderTuner : MonoBehaviour
{
    public float nearClip = 0.05f;
    public float farClip = 500f;

    IEnumerator Start()
    {
        QualitySettings.antiAliasing = 4;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        for (int k = 0; k < 3; k++)
        {
            yield return new WaitForSeconds(k == 0 ? 1f : 5f);
            Camera[] cams = Camera.allCameras;
            for (int i = 0; i < cams.Length; i++)
            {
                if (cams[i] == null) continue;
                cams[i].nearClipPlane = nearClip;
                cams[i].farClipPlane = farClip;
                cams[i].allowMSAA = true;
            }
        }
    }
}
