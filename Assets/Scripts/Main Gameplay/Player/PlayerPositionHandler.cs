using UnityEngine;
using Unity.Netcode;

public class PlayerPositionHandle : MonoBehaviour
{
    [Tooltip("Spawn points in order. Player with OwnerClientId 0 goes to index 0, etc.")]
    [SerializeField] private Transform[] spawnPoints;

    private void Start()
    {
        StartCoroutine(SnapWhenReady());
    }

    private System.Collections.IEnumerator SnapWhenReady()
    {
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            yield return null;

        yield return null;
        yield return null;

        SnapPlayers();
    }

    private void SnapPlayers()
    {
        foreach (var nb in FindObjectsByType<PlayerBehaviour>(FindObjectsSortMode.None))
        {
            if (!nb.IsSpawned) continue;
            if (!nb.IsServer) continue; // server decides who goes where

            int id = (int)nb.OwnerClientId;
            if (id < 0 || id >= spawnPoints.Length || spawnPoints[id] == null)
            {
                Debug.LogWarning($"No spawn point for player {id}");
                continue;
            }

            // Tell the OWNER to move its own transform (client-authoritative)
            nb.TeleportToOwner(spawnPoints[id].position);
        }
    }
}