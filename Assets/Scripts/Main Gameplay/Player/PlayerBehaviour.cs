using System.Collections;
using UnityEngine;
// Make sure this namespace is included at the top:
using UnityEngine.InputSystem;

public class PlayerBehaviour : MonoBehaviour, IPlayerDamageable
{
    [Header("Netcode Prep / Local Testing")]
    [Tooltip("Set to 0 for Player 1, 1 for Player 2. Later, Netcode will handle this automatically via OwnerClientId.")]
    [SerializeField] private int localPlayerId = 0;

    [Header("Player Configuration")]
    [SerializeField] private GameObject player1GameObject;
    [SerializeField] private GameObject player2GameObject;

    [Header("Player Visuals and Explosion")]
    [SerializeField] private GameObject player1Visuals;
    [SerializeField] private GameObject player2Visuals;
    [SerializeField] private GameObject player1Explosion;
    [SerializeField] private GameObject player2Explosion;

    [Header("Shooting & Bullets Configuration")]
    [SerializeField] private GameObject player1BulletPrefab;
    [SerializeField] private GameObject player2BulletPrefab;
    [SerializeField] private Transform player1BulletSpawner;
    [SerializeField] private Transform player2BulletSpawner;

    [Header("Movement & Bullet Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float bulletSpeed = 10f;

    [Header("Player Combat Stats")]
    [SerializeField] private int playerMaxHealth = 3;
    private int playerCurrentHealth;
    private int lastDamageFrame = -1;
    private bool isDeadPlayer;

    [Header("Player Death Settings")]
    [SerializeField] private float explosionDuration = 0.6f; // duration in seconds to wait for the explosion animation to finish before destroying the Player

    // Active variables assigned automatically based on player ID
    private GameObject playerGameObject;
    private GameObject playerVisuals;
    private GameObject playerExplosion;
    private GameObject activeBulletPrefab;
    private Transform activeBulletSpawner;
    private Rigidbody2D activeRb;

    // Physics & Input caching
    private Vector2 inputVector;


    void Start()
    {
        // Configure visuals and firing points on startup
        AlternatingPlayerSpawner.playerJoined += () => ConfigurePlayer(localPlayerId);
        SetPlayerExplosion();
    }

    void Update()
    {
        // =================================================================
        // NETCODE PREP REMINDER:
        // Later, when you inherit from NetworkBehaviour, uncomment this line:
        // if (!IsOwner) return;
        // =================================================================

        // Poll inputs in Update so quick button presses are never missed
        HandlePlayerInput();
        HandlePlayerShooting();
    }

    void FixedUpdate()
    {
        // Apply physics movement synced with Unity's physics tick rate
        MovePlayer();
    }

    public void ConfigurePlayer(int playerId)
    {
        if (playerId == 0)
        {
            // Enable Player 1, Disable Player 2
            if (player1GameObject != null) player1GameObject.SetActive(true);
            if (player2GameObject != null) player2GameObject.SetActive(false);

            playerGameObject = player1GameObject;
            playerVisuals = player1Visuals;
            playerExplosion = player1Explosion;
            activeBulletPrefab = player1BulletPrefab;
            activeBulletSpawner = player1BulletSpawner;
        }
        else
        {
            // Enable Player 2, Disable Player 1
            if (player1GameObject != null) player1GameObject.SetActive(false);
            if (player2GameObject != null) player2GameObject.SetActive(true);

            playerGameObject = player2GameObject;
            playerVisuals = player2Visuals;
            playerExplosion = player2Explosion;
            activeBulletPrefab = player2BulletPrefab;
            activeBulletSpawner = player2BulletSpawner;
        }

        if (playerGameObject != null)
        {
            activeRb = playerGameObject.GetComponentInChildren<Rigidbody2D>();
            if (activeRb == null)
            {
                Debug.LogError("Rigidbody2D component is missing from the active player child object or its children!", playerGameObject);
            }
        }
    }

    void SetPlayerExplosion()
    {
        playerCurrentHealth = playerMaxHealth;

        // ensures explosion is set to false initially
        if (playerExplosion != null)
        {
            playerExplosion.SetActive(false);
        }
    }

    void HandlePlayerInput()
    {
        if (Keyboard.current == null || isDeadPlayer)
        {
            inputVector = Vector2.zero;
            return;
        }

        inputVector = Vector2.zero;

        // Handle Horizontal (A/D or Left/Right arrows)
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputVector.x += 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputVector.x -= 1f;

        // Handle Vertical (W/S or Up/Down arrows)
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputVector.y += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputVector.y -= 1f;
    }

    void MovePlayer()
    {
        if (activeRb == null || isDeadPlayer)
        {
            if (activeRb != null) activeRb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 moveDir = inputVector.normalized;
        
        // Drive movement through Rigidbody2D velocity so collisions resolve smoothly without jitter
        activeRb.linearVelocity = moveDir * moveSpeed;
    }

    void HandlePlayerShooting()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            PlayerShootBullet();
        }
    }
    
    void PlayerShootBullet()
    {
        if (activeBulletPrefab != null && activeBulletSpawner != null)
        {
            // =================================================================
            // NETCODE PREP REMINDER:
            // Later, this local Instantiate will be replaced by a ServerRpc call 
            // so the server spawns the bullet for everyone across the network.
            // =================================================================
            GameObject bullet = Instantiate(activeBulletPrefab, activeBulletSpawner.position, activeBulletPrefab.transform.rotation);
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();

            if (bulletRb != null)
            {
                bulletRb.linearVelocity = Vector2.right * bulletSpeed; 
            }
        }
        else
        {
            Debug.LogWarning("Active Bullet Prefab or Spawner is not assigned in the PlayerBehaviour inspector!");
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDeadPlayer) return;

        if (Time.frameCount == lastDamageFrame) return;
        lastDamageFrame = Time.frameCount;

        playerCurrentHealth -= damageAmount;

        if (playerCurrentHealth <= 0)
        {
            PlayerDeath();
        }
    }

    void PlayerDeath()
    {
        if (isDeadPlayer) return;
        isDeadPlayer = true;

        // Stop movement immediately
        if (activeRb != null)
        {
            activeRb.linearVelocity = Vector2.zero;
            activeRb.simulated = false; // Turn off physics simulation so it doesn't interact anymore
        }

        if (playerGameObject != null)
        {
            Collider2D[] colliders = playerGameObject.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D col in colliders)
            {
                col.enabled = false;   // Disables the collider component entirely
            }
        }

        // stop shooting coroutines immediately
        StopAllCoroutines();

        // hides the spaceship visuals
        if (playerVisuals != null)
        {
            playerVisuals.SetActive(false);
        }

        // enables the explosion animation gameObject
        if (playerExplosion != null)
        {
            playerExplosion.SetActive(true);
        }

        // waits for the explosion animation to finish, then destroys the player object
        StartCoroutine(DestroySequence());
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayerDeath();
        }
    }

    IEnumerator DestroySequence()
    {
        yield return new WaitForSeconds(explosionDuration); 
        Destroy(gameObject);
    } 
}