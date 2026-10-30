using UnityEngine;

public interface IPlayerDamageable
{
    void TakeDamage(int damageAmount);
}

public class EnemyBulletBehaviour : MonoBehaviour
{
    private int damage = 1; // damage amount to Player
    private bool hasHit = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
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

            Destroy(gameObject);
        }
    }
}
