using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerBehaviour : NetworkBehaviour, IPlayerDamageable
{
    [Header("Netcode Prep / Local Testing")]
    [Tooltip("Set to 0 for Player 1, 1 for Player 2. Later, Netcode will handle this automatically via OwnerClientId.")]

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
    // private int netPlayerHealth;
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
    private Image activeHealthBarImage;
    private Rigidbody2D activeRb;

    // Physics & Input caching
    private Vector2 inputVector;

    // Networked health variable so fillAmount stays perfectly synced across clients
    private NetworkVariable<int> netPlayerHealth = new NetworkVariable<int>(
        3, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );


    private bool healthBarBound = false;

    public override void OnNetworkSpawn()
    {
        int networkPlayerID = (int)OwnerClientId;
        ConfigurePlayer(networkPlayerID);
        SetPlayerExplosion();

        netPlayerHealth.OnValueChanged += OnHealthChanged;

        // Try immediately (works if UI already exists)
        TryBindHealthBar();
        UpdateHealthBarUI(netPlayerHealth.Value);
    }

    private void TryBindHealthBar()
    {
        if (healthBarBound || activeHealthBarImage != null) return;

        string tag = (OwnerClientId == 0) ? "Player1HealthBar" : "Player2HealthBar";
        GameObject bar = GameObject.FindWithTag(tag);

        if (bar != null)
        {
            activeHealthBarImage = bar.GetComponent<Image>();
            if (activeHealthBarImage != null)
            {
                healthBarBound = true;
                UpdateHealthBarUI(netPlayerHealth.Value); // apply current health once bound
                Debug.Log($"[PlayerBehaviour] Health bar bound: {tag}");
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        netPlayerHealth.OnValueChanged -= OnHealthChanged;
        base.OnNetworkDespawn();
    }


    void Update()
    {
        // Keep trying to bind until the UI scene loads
        if (!healthBarBound)
        {
            TryBindHealthBar();
        }

        HandlePlayerInput();
        HandlePlayerShooting();
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

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
        if (IsServer)
        {
            netPlayerHealth.Value = playerMaxHealth; 
        }

        // ensures explosion is set to false initially
        if (playerExplosion != null)
        {
            playerExplosion.SetActive(false);
        }
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        UpdateHealthBarUI(newHealth);
        if (activeHealthBarImage == null)
        {
            Debug.Log("null mo to");
        }
    }

    private void UpdateHealthBarUI(int currentHealth)
    {
        if (activeHealthBarImage != null)
        {
            // Maps current health to 0.0 - 1.0 fill amount ratio
            activeHealthBarImage.fillAmount = (float)currentHealth / playerMaxHealth;
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
        if (!IsOwner) return;
        ShootBulletServerRpc(activeBulletSpawner.position); // request this function from the server
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ShootBulletServerRpc(Vector3 spawnPosition)
    {
        if (activeBulletPrefab != null)
        {
            GameObject bullet = Instantiate(activeBulletPrefab, spawnPosition, activeBulletPrefab.transform.rotation);
            NetworkObject bulletNetObj = bullet.GetComponent<NetworkObject>();

            if (bulletNetObj != null)
            {
                bulletNetObj.Spawn(true);

                Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
                if (bulletRb != null)
                {
                    bulletRb.linearVelocity = Vector2.right * bulletSpeed;
                }
            }
        }
        else
        {
            Debug.LogWarning("Active Bullet Prefab or Spawner is not assigned in the PlayerBehaviour inspector!");
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (!IsServer) return;
        if (isDeadPlayer) return;

        if (Time.frameCount == lastDamageFrame) return;
        lastDamageFrame = Time.frameCount;

        netPlayerHealth.Value -= damageAmount;

        if (netPlayerHealth.Value <= 0)
        {
            PlayerDeath();
        }
    }

    void PlayerDeath()
    {
        PlayerDeathClientRpc(); 

        StartCoroutine(DestroySequence());

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterPlayerDeath();
    }

    [ClientRpc]
    private void PlayerDeathClientRpc()
    {   
        if (isDeadPlayer) return;
        isDeadPlayer = true;

        // stop shooting coroutines immediately
        StopAllCoroutines();

        if (playerVisuals != null) playerVisuals.SetActive(false);
        if (playerExplosion != null) playerExplosion.SetActive(true);

        if (activeRb != null)
        {
            activeRb.linearVelocity = Vector2.zero;
            activeRb.simulated = false;
        }

        if (playerGameObject != null)
        {
            foreach (var col in playerGameObject.GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        }
    }

    IEnumerator DestroySequence()
    {
        yield return new WaitForSeconds(explosionDuration); 
        if (IsServer)
        {
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

    public void TeleportToOwner(Vector3 worldPos)
    {
        if (!IsServer) return;

        if (activeRb != null)
            activeRb.linearVelocity = Vector2.zero;

        TeleportClientRpc(worldPos);
    }

    [Rpc(SendTo.Owner)]
    private void TeleportClientRpc(Vector3 worldPos)
    {
        transform.position = worldPos;

        if (activeRb != null)
            activeRb.linearVelocity = Vector2.zero;
    }
}