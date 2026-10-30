using System.Collections;
using UnityEngine;

public class Enemy2_Behaviour : MonoBehaviour, IEnemyDamageable
{
    [Header("References")]
    [SerializeField] private GameObject enemySpaceshipVisuals; // visuals of Enemy
    [SerializeField] private GameObject enemyExplosion; // GameObject with explosion animation
    [SerializeField] private Transform[] enemy2BulletSpawners; // in list to get more than 1 spawner (since Enemy 2 has 2)
    [SerializeField] private GameObject enemy2BulletPrefab; // bullet prefab

    [Header("Enemy 2 Movement")]
    [SerializeField] private float moveSpeed = 0.3f; // move speed of Enemy

    [Header("Enemy 2 Shift Pattern")]
    [SerializeField] private float yDistance = 2f; // y movement time
    [SerializeField] private float verticalSpeed = 3f; // y movement speed time

    [Header("Enemy 2 Combat Stats")]
    public int enemy2MaxHealth = 5; // amount of player bullets it takes to kill
    private int enemy2CurrentHealth; // current health variable for Enemy
    private bool isDeadEnemy2; // bool variable for when bomb is dead or not
    private bool hasUnregistered = false;
    private int lastDamageFrame = -1;

    [Header("Enemy 2 Shooting Timer")] // delay between the bullet shots
    [SerializeField] private float shortInterval = 0.25f;
    [SerializeField] private float longInterval = 1f;
    [SerializeField] private float longestInterval = 3f;
    [SerializeField] private float shootPause = 1f; // delay before starting the next shoot cycle

    [Header("Enemy 1 Bullet Settings")]
    [SerializeField] private float bulletSpeed = 2f; // speed at which the enemy bullet moves to the left

    [Header("Enemy 1 Death Settings")]
    [SerializeField] private float explosionDuration = 0.3f; // duration in seconds to wait for the explosion animation to finish before destroying the Enemy

    private bool isShifting = false;
    private float shiftDirection;


    void Start()
    {   
        SpawnEnemy2Bullets();
    }

    void Update()
    {
        MoveEnemy2();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        HandleCollisionEnemy2(collision);
    }

    void SpawnEnemy2Bullets()
    {
        enemy2CurrentHealth = enemy2MaxHealth;

        // ensures enemy explosion is set to false
        if (enemyExplosion != null)
        {
            enemyExplosion.SetActive(false);
        }

        // Enemy shooting coroutine
        StartCoroutine(Enemy2SequenceRoutine());
    }

    void MoveEnemy2()
    {
        if (isDeadEnemy2 || isShifting) return;
        
        transform.Translate(Vector2.left * (moveSpeed * Time.deltaTime));
    }

    // Handles pattern: Move left > Pause > Shift UP > Move left > Pause > Shift DOWN > Repeat
    IEnumerator MovementPatternRoutine()
    {
        isShifting = true;

        // move vertically (UP on 1st loop, DOWN on 2nd loop)
        yield return StartCoroutine(MoveVertical(yDistance * shiftDirection)); // applying value to y target position whether up or down (yDistance*-1 or yDistance*+1)
        
        if (isDeadEnemy2) yield break; // break if Enemy is dead

        // alternate direction for the next cycle (+1 > -1 > +1)
        shiftDirection *= -1f;

        // resume horizontal movement after vertical move completes
        isShifting = false;
    }

    // vertical movement
    IEnumerator MoveVertical(float amountToMove)
    {
        Vector3 startPosition = transform.position; // records Enemy's current position
        Vector3 targetPosition = startPosition + new Vector3(0f, amountToMove, 0f); // records target y position

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f) // so long as the distance between player's current position and target position is > 0.01f (heasn't reach target position)
        {
            if (isDeadEnemy2) yield break;

            transform.position = Vector3.MoveTowards(
                transform.position, // current Enemy position
                targetPosition, // y destination 
                verticalSpeed * Time.deltaTime 
            );
            yield return null; // frame wait
        }

        transform.position = targetPosition; // snap to precise target position
    }


    // handles the shoot attack pattern: bullet 1 > short interval > bullet 2 > long pause > Repeat
    IEnumerator Enemy2SequenceRoutine()
    {
        // direction multiplier to alternate shifting UP (+1) and DOWN (-1)
        shiftDirection = 1f;

        while (!isDeadEnemy2)
        {   
            yield return new WaitForSeconds(longInterval);
            if (isDeadEnemy2) yield break; 
            Enemy2ShootBullet();

            yield return new WaitForSeconds(shortInterval);
            if (isDeadEnemy2) yield break; 
            Enemy2ShootBullet();

            yield return new WaitForSeconds(longInterval);
            if (isDeadEnemy2) yield break; 
            Enemy2ShootBullet();

            yield return new WaitForSeconds(shortInterval);
            if (isDeadEnemy2) yield break; 
            Enemy2ShootBullet();

            // Wait out the remaining shoot pause duration
            yield return new WaitForSeconds(longestInterval);
            if (isDeadEnemy2) yield break; 
            Enemy2ShootBullet();
            
            yield return new WaitForSeconds(shootPause);
            yield return StartCoroutine(MovementPatternRoutine()); // method called to start shifting before looping to shoot bullets again
            
        }
    }

    void Enemy2ShootBullet()
    {
        if (enemy2BulletPrefab != null && enemy2BulletSpawners != null)
        {
            // loop through each assigned spawner and instantiate a bullet
            foreach (Transform spawner in enemy2BulletSpawners)
            {
                if (spawner != null)
                {
                    GameObject bullet = Instantiate(enemy2BulletPrefab, spawner.position, enemy2BulletPrefab.transform.rotation); // to retain bullet prefab z rotation 'enemy2BulletPrefab.transform.rotation' instead of 'Quaternion.identity'

                    // velocity to move to the left (-Vector2.right or -transform.right)
                    Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();

                    if (rb != null)
                    {
                        // moves left (-X direction) at the specified speed
                        rb.linearVelocity = Vector2.left * bulletSpeed;
                    }
                }
            }
        }
    }

    // method called in player bullet script upon collision
    public void TakeDamage(int damageAmount)
    {
        if (isDeadEnemy2) return;

        // ignore duplicate hits occurring in the exact same frame tick
        if (Time.frameCount == lastDamageFrame) return; 
        lastDamageFrame = Time.frameCount;

        enemy2CurrentHealth -= damageAmount; // deduct damage from the bomb's current health

        if (enemy2CurrentHealth <= 0)
        {
            Enemy2Death();
        }
    }

    private void HandleCollisionEnemy2(Collider2D collision)
    {
        switch (collision.tag)
        {
            case "Player": 
                Enemy2Death();
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

    void Enemy2Death()
    {
        if (isDeadEnemy2) return;
        isDeadEnemy2 = true;

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

        // waits for the explosion animation to finish, then destroy the entire Enemy1 parent object (which includes everything in Enemy 2)
        StartCoroutine(DestroySequence());
    }

    void OnDestroy()
    {
        NotifyManagerOnDeath(); // fallback for InvisibleWall2 destruction
    }

    IEnumerator DestroySequence()
    {
        yield return new WaitForSeconds(explosionDuration); // given duration for the Enemy Explosion set to Active before destrying the main parent Enemy 2 GameObject
        Destroy(gameObject);
    } 
}
