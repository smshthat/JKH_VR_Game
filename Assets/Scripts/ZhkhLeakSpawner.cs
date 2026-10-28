using UnityEngine;

// Время от времени на улице возле старта появляется протечка. Её чинят гаечным ключом.
public class ZhkhLeakSpawner : MonoBehaviour
{
    public static int FixedCount;
    public static int Active;

    public float firstDelay = 45f;
    public float minInterval = 80f;
    public float maxInterval = 140f;
    public int maxActive = 3;

    float next;
    Vector3 center;
    bool hasCenter;

    void Awake() { FixedCount = 0; Active = 0; }

    void Update()
    {
        if (!hasCenter)
        {
            Transform head = ZhkhVR.Head();
            if (head == null || Time.time < 3f) return;
            center = head.position;
            hasCenter = true;
            next = Time.time + firstDelay;
            return;
        }
        if (Time.time < next) return;
        next = Time.time + Random.Range(minInterval, maxInterval);
        if (Active < maxActive) TrySpawn();
    }

    void TrySpawn()
    {
        Transform head = ZhkhVR.Head();
        for (int i = 0; i < 20; i++)
        {
            Vector2 d = Random.insideUnitCircle.normalized * Random.Range(6f, 30f);
            Vector3 from = new Vector3(center.x + d.x, center.y + 20f, center.z + d.y);
            RaycastHit hit;
            if (!Physics.Raycast(from, Vector3.down, out hit, 60f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (hit.normal.y < 0.9f) continue;
            if (hit.point.y > center.y - 0.8f || hit.point.y < center.y - 3f) continue;
            if (Physics.CheckSphere(hit.point + Vector3.up * 0.7f, 0.5f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (head != null && (head.position - hit.point).sqrMagnitude < 25f) continue;
            ZhkhWaterLeak.Create(hit.point);
            return;
        }
    }
}
