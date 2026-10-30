using UnityEngine;

public interface IEnemyDamageable
{
    void TakeDamage(int damageAmount);
}

public class BulletBehaviour : MonoBehaviour
{
    private int damage = 1; // how much damage the bullet gives to enemies
    private bool hasHit = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        if (collision.CompareTag("Enemy") || collision.CompareTag("InvisibleWall"))
        {
            hasHit = true;

            IEnemyDamageable damageable = collision.GetComponent<IEnemyDamageable>();
            
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            Destroy(gameObject); 
        }
    }
}