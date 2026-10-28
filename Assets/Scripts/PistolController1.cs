using UnityEngine;
using Valve.VR.InteractionSystem;

// Пистолет для контроллеров HTC Vive (работает и с другими контроллерами SteamVR).
//  - Взять: поднесите руку и нажмите боковую кнопку (грип) ИЛИ курок.
//  - Выстрел: курок.
//  - Бросить: боковая кнопка (грип), пистолет полетит с физикой.
// Пистолет держится в руке сам (без Throwable), поэтому курком можно и брать, и стрелять.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Interactable))]
public class PistolController : MonoBehaviour
{
    public Transform muzzle;            // дуло (оттуда вылетает пуля)
    public float damage = 34f;          // урон NPC (3 попадания = нокаут)
    public float range = 60f;           // дальность выстрела
    public float shotForce = 6f;        // с какой силой пуля толкает предметы
    public float fireRate = 0.25f;      // пауза между выстрелами
    public int maxAmmo = 12;            // патронов в обойме
    public float reloadTime = 2f;       // через сколько секунд после пустой обоймы заряжается сам

    [Header("Как пистолет сидит в руке (если целится не туда, меняйте поворот)")]
    public Vector3 holdPosition = new Vector3(0f, -0.03f, 0.06f);
    public Vector3 holdRotation = new Vector3(0f, 0f, 0f);

    int ammo;
    float nextShot;
    float reloadTimer;
    float pickupTime;
    float dropTime = -10f;
    float tracerTimer;

    Hand holder;                        // рука, в которой пистолет (пусто = лежит)
    Rigidbody rb;
    Vector3 lastPos;
    Vector3 velocity;                   // скорость руки, чтобы бросок был по-настоящему
    LineRenderer tracer;                // след выстрела
    LineRenderer laser;                 // тонкий лазер для прицеливания
    AudioSource audioSrc;
    AudioClip shotClip;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ammo = maxAmmo;

        tracer = MakeLine("След", new Color(1f, 0.9f, 0.4f), 0.012f);
        laser = MakeLine("Лазер", new Color(1f, 0.1f, 0.1f), 0.003f);

        // Звук выстрела делаем прямо в коде, файлы не нужны
        int rate = 44100;
        int n = rate / 6;
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            data[i] = (Random.value * 2f - 1f) * Mathf.Pow(1f - t, 3f) * 0.8f;
        }
        shotClip = AudioClip.Create("shot", n, 1, rate, false);
        shotClip.SetData(data, 0);
        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 1f;
    }

    LineRenderer MakeLine(string name, Color color, float width)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = color;
        lr.enabled = false;
        return lr;
    }

    // SteamVR сам вызывает этот метод, пока рука наведена на пистолет (нужен Interactable)
    void HandHoverUpdate(Hand hand)
    {
        if (holder != null) return;
        if (Time.time - dropTime < 0.3f) return;
        if (hand.currentAttachedObject != null) return;     // в этой руке уже что-то есть
        if (hand.GetGrabStarting() != GrabTypes.None)
        {
            holder = hand;
            pickupTime = Time.time;
            rb.isKinematic = true;
            lastPos = transform.position;
            velocity = Vector3.zero;
        }
    }

    void Update()
    {
        bool held = holder != null;

        // Автозарядка, когда обойма пустая
        if (ammo <= 0)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f) ammo = maxAmmo;
        }

        // Лазер виден, пока пистолет в руке
        laser.enabled = held;
        if (held)
        {
            Vector3 end = muzzle.position + muzzle.forward * range;
            RaycastHit hit;
            if (Physics.Raycast(muzzle.position, muzzle.forward, out hit, range, ~0, QueryTriggerInteraction.Ignore))
                end = hit.point;
            laser.SetPosition(0, muzzle.position);
            laser.SetPosition(1, end);

            // Курок -> выстрел, боковая кнопка -> бросить
            if (Time.time - pickupTime > 0.3f)
            {
                if (holder.GetGrabStarting(GrabTypes.Pinch) != GrabTypes.None) Shoot();
                else if (holder.GetGrabStarting(GrabTypes.Grip) != GrabTypes.None) Drop();
            }
        }

        // След выстрела пропадает через долю секунды
        if (tracer.enabled)
        {
            tracerTimer -= Time.deltaTime;
            if (tracerTimer <= 0f) tracer.enabled = false;
        }
    }

    // Пистолет следует за рукой (после того, как SteamVR обновил положение контроллера)
    void LateUpdate()
    {
        if (holder == null) return;

        transform.rotation = holder.transform.rotation * Quaternion.Euler(holdRotation);
        transform.position = holder.transform.TransformPoint(holdPosition);

        Vector3 v = (transform.position - lastPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        velocity = Vector3.Lerp(velocity, v, 0.3f);
        lastPos = transform.position;
    }

    void Drop()
    {
        holder = null;
        dropTime = Time.time;
        rb.isKinematic = false;
        rb.linearVelocity = velocity;             // бросок с скоростью руки
        rb.angularVelocity = Vector3.zero;
    }

    void Shoot()
    {
        if (Time.time < nextShot || ammo <= 0) return;
        nextShot = Time.time + fireRate;
        ammo--;
        if (ammo == 0) reloadTimer = reloadTime;

        Vector3 origin = muzzle.position;
        Vector3 dir = muzzle.forward;
        Vector3 end = origin + dir * range;

        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, range, ~0, QueryTriggerInteraction.Ignore))
        {
            end = hit.point;

            // Попали в NPC
            NPCFightBack npc = hit.collider.GetComponentInParent<NPCFightBack>();
            if (npc != null) npc.TakeDamage(damage);

            // Попали в предмет с физикой -> толкаем
            if (hit.rigidbody != null)
                hit.rigidbody.AddForceAtPosition(dir * shotForce, hit.point, ForceMode.Impulse);
        }

        // След и звук
        tracer.SetPosition(0, origin);
        tracer.SetPosition(1, end);
        tracer.enabled = true;
        tracerTimer = 0.05f;
        audioSrc.PlayOneShot(shotClip);
    }
}
