using UnityEngine;

// Игрок. Ходьба в SteamVR сделана телепортом (готовая система из ассета SteamVR),
// поэтому этот скрипт только ставит игрока на старт и возвращает его, если он куда-то провалился.
// Вешается на объект Player (префаб из ассета SteamVR).
public class PlayerController : MonoBehaviour
{
    Vector3 startPosition;   // где игрок начинает игру

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // Провалился под землю -> вернуть на старт
        if (transform.position.y < -5f)
            transform.position = startPosition;
    }
}
