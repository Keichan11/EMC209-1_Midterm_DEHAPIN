using System.Collections;
using UnityEngine;

public class Enemy1_Behaviour : MonoBehaviour, IEnemyDamageable
{
    [Header("References")]
    [SerializeField] private GameObject enemySpaceshipVisuals; // visuals of Enemy
    [SerializeField] private GameObject enemyExplosion; // GameObject with explosion animation
    [SerializeField] private Transform enemy1BulletSpawner; // bullet spawner
    [SerializeField] private GameObject enemy1BulletPrefab; // bullet prefab

    [Header("Enemy 1 Movement")]
    [SerializeField] private float moveSpeed = 0.5f; // move speed of Enemy

    [Header("Enemy 1 Combat Stats")]
    [SerializeField] private int enemy1MaxHealth = 3; // amount of player bullets it takes to kill
    private int enemy1CurrentHealth; // current health variable for Enemy
    private bool isDeadEnemy1; // bool variable for when bomb is dead or not
    private bool hasUnregistered = false;
    private int lastDamageFrame = -1;

    [Header("Enemy 1 Shooting Timer")]
    [SerializeField] private float shortInterval = 0.25f; // delay between the first and second shot bullet
    [SerializeField] private float shootPause = 2f; // delay before starting the next shoot cycle.

    [Header("Enemy 1 Bullet Settings")]
    [SerializeField] private float bulletSpeed = 2f; // speed at which the enemy bullet moves to the left

    [Header("Enemy 1 Death Settings")]
    [SerializeField] private float explosionDuration = 0.3f; // duration in seconds to wait for the explosion animation to finish before destroying the Enemy


    void Start()
    {   
        SpawnEnemy1Bullets();
    }

    void Update()
    {
        MoveEnemy1();
    }
    

    void OnTriggerEnter2D(Collider2D collision)
    {
        HandleCollisionEnemy1(collision);
    }

    void SpawnEnemy1Bullets()
    {
        enemy1CurrentHealth = enemy1MaxHealth;

        // ensures enemy explosion is set to false
        if (enemyExplosion != null)
        {
            enemyExplosion.SetActive(false);
        }

        // Enemy 1 shooting coroutine
        StartCoroutine(Enemy1ShootRoutine());
    }

    void MoveEnemy1()
    {
        if (isDeadEnemy1) return;
        
        transform.Translate(Vector2.left * (moveSpeed * Time.deltaTime));
    }

    // handles the shoot attack pattern: bullet 1 > short interval > bullet 2 > long pause > Repeat
    IEnumerator Enemy1ShootRoutine()
    {
        while (!isDeadEnemy1)
        {
            // wait before firing the bullet
            yield return new WaitForSeconds(shootPause);
            if (isDeadEnemy1) yield break; // if enemy is already dead, stops shooting 

            // bullet 1
            Enemy1ShootBullet();

            // short interval between bullets
            yield return new WaitForSeconds(shortInterval);

            if (isDeadEnemy1) yield break; // if enemy is already dead after the first bullet, stop shooting the second bullet

            // bullet 2
            Enemy1ShootBullet();
        }
    }

    void Enemy1ShootBullet()
    {
        if (enemy1BulletPrefab != null && enemy1BulletSpawner != null)
        {
            // instantiates bullet
            GameObject bullet = Instantiate(enemy1BulletPrefab, enemy1BulletSpawner.position, Quaternion.identity);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                // moves left (-X direction) at the specified speed
                rb.linearVelocity = Vector2.left * bulletSpeed;
            }
        }
    }

    // method called in player bullet script upon collision
    public void TakeDamage(int damageAmount)
    {
        if (isDeadEnemy1) return;

        // ignore duplicate hits occurring in the exact same frame tick
        if (Time.frameCount == lastDamageFrame) return;
        lastDamageFrame = Time.frameCount;

        enemy1CurrentHealth -= damageAmount; // deduct damage from the bomb's current health

        if (enemy1CurrentHealth <= 0)
        {
            Enemy1Death();
        }
    }

    private void HandleCollisionEnemy1(Collider2D collision)
    {
        switch (collision.tag)
        {
            case "Player":
                Enemy1Death();
                break;
            case "InvisibleWall2": // instantly destroys Enemy GameObject via collider outside camera view
                Destroy(gameObject);
                break;
            default:
                break;
        }
    }

    // calls a method under GameManager to decrease recorded enemy length
    public void NotifyManagerOnDeath()
    {
        if (hasUnregistered) return;
        hasUnregistered = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterEnemy();
        }
    }

    void Enemy1Death()
    {
        if (isDeadEnemy1) return;
        isDeadEnemy1 = true;

        NotifyManagerOnDeath();

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(); // gets all colliders in the GameObject and its children
        foreach (Collider2D col in colliders)
        {
            col.enabled = false; // enable false so colliders won't stay active when explosion animation is on
        }

        // stop shooting coroutines immediately
        StopAllCoroutines();

        // hides the spaceship visuals (sprite, exhausts, spawner) so it won't be seen after the explosion animation started
        if (enemySpaceshipVisuals != null)
        {
            enemySpaceshipVisuals.SetActive(false);
        }

        // enables the explosion animation gameObject
        if (enemyExplosion != null)
        {
            enemyExplosion.SetActive(true);
        }

        // waits for the explosion animation to finish, then destroy the entire Enemy1 parent object (which includes everything in Enemy 1)
        StartCoroutine(DestroySequence());
    }

    void OnDestroy()
    {
        NotifyManagerOnDeath(); // fallback for InvisibleWall2 destruction
    }

    IEnumerator DestroySequence()
    {
        yield return new WaitForSeconds(explosionDuration); // given duration for the Enemy Explosion set to Active before destrying the main parent Enemy 1 GameObject
        Destroy(gameObject);
    } 
}