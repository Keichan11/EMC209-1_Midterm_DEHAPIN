using UnityEngine;
using Unity.Netcode;

public class PlayerCollisionRelay : NetworkBehaviour
{
    private PlayerBehaviour playerBehaviour;

    void Awake()
    {
        playerBehaviour = GetComponentInParent<PlayerBehaviour>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.CompareTag("Enemy"))
        {
            if (playerBehaviour != null)
            {
                playerBehaviour.TakeDamage(1);
            }
        }
    }
}