using UnityEngine;
using Unity.Netcode;

public interface IPlayerDamageable
{
    void TakeDamage(int damageAmount);
}

public class EnemyBulletBehaviour : NetworkBehaviour
{
    private int damage = 1; // damage amount to Player
    private bool hasHit = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (hasHit) return;

        if (collision.CompareTag("Player") || collision.CompareTag("InvisibleWall"))
        {
            hasHit = true;
            
            IPlayerDamageable damageable = collision.GetComponentInParent<IPlayerDamageable>();
            
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            // Deal damage to player script here 
            // PlayerHealth player = collision.GetComponent<PlayerHealth>();
            // if (player != null) { player.TakeDamage(damage); }

            NetworkObject netObj = GetComponent<NetworkObject>();
            
            if (netObj != null)
            {
                netObj.Despawn(true);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
