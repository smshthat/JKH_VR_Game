using UnityEngine;

// БЕЗОПАСНАЯ ВЕРСИЯ: NPC больше не атакует, не гонится за игроком и не умирает.
// Все публичные поля и методы сохранены, чтобы остальные скрипты проекта продолжали компилироваться.
// Удар рукой по NPC теперь даёт только реакцию-покачивание (анимация ZhkhNpcAnimator), без урона.
public class NPCFightBack : MonoBehaviour
{
    public float startHealth = 50f;
    public int hitsToAnger = 1;
    public int hitsToKnockout = 5;
    public float wakeHealth = 30f;
    public float knockoutTime = 20f;
    public float corpseTime = 30f;
    public float chaseSpeed = 1.3f;
    public float attackRange = 1.1f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.2f;
    public float giveUpDistance = 20f;

    public float health;
    public bool angry;        // всегда false
    public bool knockedOut;   // всегда false
    public bool dead;         // всегда false

    NPCInteraction interaction;
    ZhkhNpcAnimator anim;
    int hitBaseline;

    void Awake()
    {
        // Каждый NPC (в том числе в других домах и появившийся позже) сам получает анимации и руку с купюрой
        if (GetComponent<ZhkhNpcAnimator>() == null) gameObject.AddComponent<ZhkhNpcAnimator>();
        if (GetComponent<ZhkhHandRig>() == null) gameObject.AddComponent<ZhkhHandRig>();
        if (GetComponent<ZhkhBillHold>() == null) gameObject.AddComponent<ZhkhBillHold>();
    }

    void Start()
    {
        health = startHealth;
        interaction = GetComponent<NPCInteraction>();
        anim = GetComponent<ZhkhNpcAnimator>();
        if (interaction != null) hitBaseline = interaction.hitCount;
    }

    void Update()
    {
        angry = false; knockedOut = false; dead = false;
        if (interaction == null) return;
        int h = interaction.hitCount;
        if (h == hitBaseline) return;
        hitBaseline = h;
        if (anim == null) anim = GetComponent<ZhkhNpcAnimator>();
        if (anim != null) anim.Stagger();
    }

    // Оставлено для совместимости (раньше вызывалось пистолетом). Урона нет.
    public void TakeDamage(float amount) { }
    public void Calm() { angry = false; }
}
