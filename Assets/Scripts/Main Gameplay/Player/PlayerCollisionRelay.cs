using UnityEngine;

public class PlayerCollisionRelay : MonoBehaviour
{
    private PlayerBehaviour playerBehaviour;

    void Awake()
    {
        playerBehaviour = GetComponentInParent<PlayerBehaviour>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            if (playerBehaviour != null)
            {
                playerBehaviour.TakeDamage(1);
            }
        }
    }
}