using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Bomb_Behaviour : NetworkBehaviour, IEnemyDamageable
{
    [Header("References")]
    [SerializeField] private GameObject bombVisuals; // visuals of bomb
    [SerializeField] private GameObject bombExplosion; // GameObject with explosion animation

    [Header("Bomb Movement")]
    [SerializeField] private float moveSpeed = 0.5f; // move speed of Bomb movement

    [Header("Bomb Stats")]
    public int bombMaxHealth = 1; // max health
    private int bombCurrentHealth; // current health variable for bomb
    private bool isBombExplode; // bool variable for when bomb is dead or not
    private bool hasUnregistered = false;
    private int lastDamageFrame = -1;

    [Header("Bomb Explosion Settings")]
    [SerializeField] private float explosionDuration = 0.3f; // duration in seconds to wait for the explosion animation to finish before destroying the Bomb

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        
        SetBombExplosion(); 
    }

    void Update()
    {
        if (!IsServer) return;

        MoveBomb();
    }


    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        HandleCollisionBomb(collision);
    }

    void SetBombExplosion()
    {
        bombCurrentHealth = bombMaxHealth;

        // ensures enemy explosion is set to false
        if (bombExplosion != null)
        {
            bombExplosion.SetActive(false);
        }
    }

    void MoveBomb()
    {
        if (isBombExplode) return;

        transform.Translate(Vector2.left * (moveSpeed * Time.deltaTime));
    }

    // method called in player bullet script upon collision
    public void TakeDamage(int damageAmount)
    {
        if (!IsServer) return;

        if (isBombExplode) return;

        // Ignore duplicate hits occurring in the exact same frame tick
        if (Time.frameCount == lastDamageFrame) return;
        lastDamageFrame = Time.frameCount;

        bombCurrentHealth -= damageAmount; // deduct damage from the bomb's current health

        if (bombCurrentHealth <= 0)
        {
            BombExplode();
        }
    }

    private void HandleCollisionBomb(Collider2D collision)
    {
        switch (collision.tag)
        {
            case "Player":
                BombExplode();
                break;
            case "InvisibleWall2":
                NetworkObject netObj = GetComponent<NetworkObject>();
                if (netObj != null) netObj.Despawn(true);
                else Destroy(gameObject);
                break;
            default:
                break;
        }
    }

    // calls a method under GameManager to decrease recorded enemy length 
    public void NotifyManagerOnDeath()
    {
        if (!IsServer) return;

        if (hasUnregistered) return;
        hasUnregistered = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterEnemy();
        }
    }
    
    void BombExplode()
    {
        if (isBombExplode) return;
        isBombExplode = true;

        NotifyManagerOnDeath();

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(); // gets all colliders in the GameObject and its children
        foreach (Collider2D col in colliders)
        {
            col.enabled = false; // enable false so colliders won't stay active when explosion animation is on
        }

        ShowDeathVisualsCLientRpc();

        // waits for the explosion animation to finish, then destroy the entire Bomb parent object (which includes everything in Bomb)
        StartCoroutine(DestroySequence());
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ShowDeathVisualsCLientRpc()
    {
        // hides the bomb visuals (sprite with animation) so it won't be seen after the explosion animation started
        if (bombVisuals != null)
        {
            bombVisuals.SetActive(false);
        }

        // enables the explosion animation gameObject
        if (bombExplosion != null)
        {
            bombExplosion.SetActive(true);
        }
    }

    void OnDestroy()
    {
        NotifyManagerOnDeath(); // fallback for InvisibleWall2 destruction
    }

    IEnumerator DestroySequence()
    {
        yield return new WaitForSeconds(explosionDuration); // given duration for the Bomb Explosion set to Active before destrying the main parent Bomb GameObject

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    } 
}
