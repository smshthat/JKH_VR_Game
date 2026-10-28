using System.Collections;
using System.Linq;
using UnityEngine;

// Batch only explicitly static environment meshes. Interactive and animated objects stay separate.
public class ZhkhSceneOptimization : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null;
        var objects = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(r => r.gameObject.isStatic && !r.isPartOfStaticBatch && r.GetComponentInParent<Valve.VR.InteractionSystem.TeleportMarkerBase>() == null
                && r.GetComponentInParent<TaskBoard>() == null && r.GetComponent<MeshFilter>() != null
                && r.GetComponent<MeshFilter>().sharedMesh != null && r.GetComponent<MeshFilter>().sharedMesh.isReadable)
            .Select(r => r.gameObject).Distinct().ToArray();
        if (objects.Length > 1) StaticBatchingUtility.Combine(objects, gameObject);
    }
}
