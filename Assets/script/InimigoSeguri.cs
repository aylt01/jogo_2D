using UnityEngine;

public class InimigoSeguri : MonoBehaviour
{
    Transform player;
    public float velocidade = 2f;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            player.position,
            velocidade * Time.deltaTime
        );
    }
}