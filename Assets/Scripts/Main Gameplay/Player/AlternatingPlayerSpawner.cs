using System.Collections;
using Unity.Netcode;
using UnityEngine;
using System;

public class AlternatingPlayerSpawner : NetworkBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Must have a NetworkObject component and be in the NetworkManager's Prefab List")]
    [SerializeField] private GameObject parentPrefab;

    [SerializeField] private GameObject prefabA;
    [SerializeField] private GameObject prefabB;

    public static event Action playerJoined;

    private NetworkObject spawnedParent;
    private int _playerCount = 0;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // 1. Spawn the parent first
        GameObject parentInstance = Instantiate(parentPrefab);
        spawnedParent = parentInstance.GetComponent<NetworkObject>();

        // Spawn as a dynamic object (false), not a scene object (true)
        spawnedParent.Spawn(false); // false = not destroyed when the current scene is unloaded

        // 2. Listen for client connections
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = false; // We handle spawning manually
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject == null)
        {
            SpawnPlayerForClient(clientId);
        }
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        GameObject chosenPrefab = (_playerCount % 2 == 0) ? prefabA : prefabB;

        // 1. Instantiate the player locally
        GameObject playerInstance = Instantiate(chosenPrefab);
        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();

        // 2. Spawn it on the network
        netObj.SpawnAsPlayerObject(clientId);

        netObj.DestroyWithScene = false;

        // 3. Parent it on the NEXT frame so Netcode doesn't panic
        StartCoroutine(ParentNextFrame(netObj));

        _playerCount++;
    }

    private IEnumerator ParentNextFrame(NetworkObject netObj)
    {
        // Wait exactly one frame for Netcode to finish registering the spawn
        yield return null;

        // Now it is safe to parent without throwing "can only be re-parented after spawned" errors
        if (netObj != null && netObj.IsSpawned && spawnedParent != null && spawnedParent.IsSpawned)
        {
            netObj.TrySetParent(spawnedParent, false);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }
    }
}