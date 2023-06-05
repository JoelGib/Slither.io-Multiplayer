using System;
using Unity.Netcode;
using UnityEngine;

public class Food : NetworkBehaviour
{
    public GameObject prefab;

    private void OnTriggerEnter2D(Collider2D col) {
        if(!col.CompareTag("Player")) return;

        if(!NetworkManager.Singleton.IsServer) return;

        if(col.TryGetComponent(out PlayerLength playerLength)){
            playerLength.AddLength();
        }
        else if(col.TryGetComponent(out Tail tail)){
            tail.networkedOwner.GetComponent<PlayerLength>().AddLength();
        }

        NetworkObjectPool.Singleton.ReturnNetworkObject(NetworkObject, prefab);
        NetworkObject.Despawn();
    }
}
