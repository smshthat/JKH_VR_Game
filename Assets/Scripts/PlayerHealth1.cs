using UnityEngine;

// Заглушка. Здоровья игрока и урона от NPC больше нет.
// Файл оставлен для совместимости со старыми скриптами.
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;
    public int resetCount;

    public void TakeDamage(float amount)
    {
    }
}
