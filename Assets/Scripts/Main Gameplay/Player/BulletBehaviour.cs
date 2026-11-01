using UnityEngine;
using Unity.Netcode;

public interface IEnemyDamageable
{
    void TakeDamage(int damageAmount);
}



public class BulletBehaviour : NetworkBehaviour
{
    private int damage = 1; // how much damage the bullet gives to enemies
    private bool hasHit = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;
        if (!IsServer) return;

        if (collision.CompareTag("Enemy") || collision.CompareTag("InvisibleWall"))
        {
            hasHit = true;

            IEnemyDamageable damageable = collision.GetComponent<IEnemyDamageable>();
            
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

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