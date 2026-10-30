using System.Collections;
using UnityEngine;

public class Enemy3_Behaviour : MonoBehaviour, IEnemyDamageable
{
    [Header("References")]
    [SerializeField] private GameObject enemySpaceshipVisuals; // visuals of Enemy
    [SerializeField] private GameObject enemyExplosion; // GameObject with explosion animation
    [SerializeField] private Transform[] enemy3BulletSpawners; // still in list in case I change my mind about bullet spawn number
    [SerializeField] private GameObject enemy3BulletPrefab; // bullet prefab

    [Header("Enemy 3 Movement")]
    [SerializeField] private float moveSpeed = 0.4f; // move speed of Enemy
    [SerializeField] private float moveDuration = 6f; // duration moving forward before stopping to execute the spinning shoot attack

    [Header("Enemy 3 Spin Attack Settings")]
    [SerializeField] private float spinDuration = 3f; // duration to complete full rotation
    [SerializeField] private float totalRotationDegrees = 360f; // degree rotation for the Enemy when it attacks (shoots bullets)
    [SerializeField] private float fireRateDuringSpin = 0.2f; // delay between the bullet shots

    [Header("Enemy 3 Combat Stats")]
    public int enemy3MaxHealth = 8; // amount of player bullets it takes to kill
    private int enemy3CurrentHealth; // current health variable for Enemy
    private bool isDeadEnemy3; // bool variable for when bomb is dead or not
    private bool hasUnregistered = false;
    private int lastDamageFrame = -1;

    [Header("Enemy 3 Bullet Settings")]
    [SerializeField] private float bulletSpeed = 3f; // speed at which the enemy bullet moves to the left from the Enemy's facing direction

    [Header("Enemy 3 Death Settings")]
    [SerializeField] private float explosionDuration = 0.3f; // duration in seconds to wait for the explosion animation to finish before destroying the Enemy

    private bool isAttacking = false;

    void Start()
    {
        SpawnEnemy3Bullets();
    }

    void Update()
    {
        MoveEnemy3();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        HandleCollisionEnemy3(collision);
    }

    void SpawnEnemy3Bullets()
    {
        enemy3CurrentHealth = enemy3MaxHealth;

        // ensures enemy explosion is set to false
        if (enemyExplosion != null)
        {
            enemyExplosion.SetActive(false);
        }

        // Enemy shooting coroutine
        StartCoroutine(Enemy3SequenceRoutine());
    }

    void MoveEnemy3()
    {
        // only move to left when Enemy not performing the spinning attack
        if (isDeadEnemy3 || isAttacking) return;

        transform.Translate(Vector2.left * (moveSpeed * Time.deltaTime));
    }

    // handles the enemy shoot attack and movement pattern:: Move > Stop & Spin-Shoot > Reset Rotation > Repeat
    IEnumerator Enemy3SequenceRoutine()
    {
        while (!isDeadEnemy3)
        {
            // Move left linearly for moveDuration
            isAttacking = false;
            yield return new WaitForSeconds(moveDuration);
            if (isDeadEnemy3) yield break;

            // Stop moving and start spinning attack
            isAttacking = true;
            yield return StartCoroutine(SpinAndShootRoutine());
            if (isDeadEnemy3) yield break;

            // Reset ship rotation back to default facing direction (0 degrees)
            transform.rotation = Quaternion.identity;
        }
    }

    // handles rotating the ship over spinDuration while firing continuously
    IEnumerator SpinAndShootRoutine()
    {
        float elapsed = 0f; // for timer handling
        float fireTimer = 0f; // for timer handling
        Quaternion startRotation = transform.rotation; // records Enemy's current rotation

        while (elapsed < spinDuration)
        {
            if (isDeadEnemy3) yield break;

            elapsed += Time.deltaTime;
            fireTimer += Time.deltaTime;

            // rotate Z axis towards target degrees
            float currentZAngle = Mathf.Lerp(0f, totalRotationDegrees, elapsed / spinDuration);
            transform.rotation = startRotation * Quaternion.Euler(0f, 0f, currentZAngle);

            // shoot at defined intervals during rotation
            if (fireTimer >= fireRateDuringSpin)
            {
                fireTimer = 0f;
                Enemy3ShootBullet();
            }

            yield return null; // Frame wait
        }
    }

    void Enemy3ShootBullet()
    {
        if (enemy3BulletPrefab == null || enemy3BulletSpawners == null) return;

        foreach (Transform spawner in enemy3BulletSpawners)
        {
            if (spawner != null)
            {
                // Trajectory vector facing outward from spawner based on ship's rotation
                Vector2 shootDirection = transform.rotation * Vector2.left; // current enemy rotation, linear shoot direction 

                // Calculate base direction angle in degrees
                float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;

                // Apply your custom 90-degree offset to match the prefab's sprite orientation
                Quaternion bulletRotation = Quaternion.Euler(0f, 0f, angle + 90f);

                GameObject bullet = Instantiate(enemy3BulletPrefab, spawner.position, bulletRotation);
                Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();

                if (rb != null)
                {
                    // moves straight along the outward vector trajectory (current angle where Enemy is facing to shoot)
                    rb.linearVelocity = shootDirection * bulletSpeed;
                }
            }
        }
    }

    // method called in player bullet script upon collision
    public void TakeDamage(int damageAmount)
    {
        if (isDeadEnemy3) return;

        // ignore duplicate hits occurring in the exact same frame tick
        if (Time.frameCount == lastDamageFrame) return; 
        lastDamageFrame = Time.frameCount;

        enemy3CurrentHealth -= damageAmount; // deduct damage from the bomb's current health

        if (enemy3CurrentHealth <= 0)
        {
            Enemy3Death();
        }
    }

    private void HandleCollisionEnemy3(Collider2D collision)
    {
        switch (collision.tag)
        {
            case "Player":
                Enemy3Death();
                break;
            case "InvisibleWall2":
                Destroy(gameObject); // instantly destroys Enemy GameObject via collider outside camera view
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

    void Enemy3Death()
    {
        if (isDeadEnemy3) return;
        isDeadEnemy3 = true;

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

        if (enemyExplosion != null)
        {
            enemyExplosion.SetActive(true);
        }

        // waits for the explosion animation to finish, then destroy the entire Enemy1 parent object (which includes everything in Enemy 3)
        StartCoroutine(DestroySequence());
    }

    void OnDestroy()
    {
        NotifyManagerOnDeath(); // fallback for InvisibleWall2 destruction
    }

    IEnumerator DestroySequence()
    {
        yield return new WaitForSeconds(explosionDuration); // given duration for the Enemy Explosion set to Active before destrying the main parent Enemy 3 GameObject
        Destroy(gameObject);
    }
}