using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerLength : NetworkBehaviour
{
    [SerializeField]private GameObject tailPrefab;
    public NetworkVariable<ushort> length = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public static event System.Action<ushort> ChangedLengthEvent;

    private Collider2D _collider2D;

    private List<GameObject> _tails;
    private Transform _lastTail;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _tails = new List<GameObject>();
        _lastTail = transform;
        _collider2D = GetComponent<Collider2D>();
        if(!IsServer) length.OnValueChanged += LengthChangedEvent;
    }

    public override void OnNetworkDespawn(){
        base.OnNetworkDespawn();
        DestroyTails();
    }

    private void DestroyTails(){
        while(_tails.Count != 0){
            GameObject tail = _tails[0];
            _tails.RemoveAt(0);
            Destroy(tail);
        }
    }

    // This will be called by the Server
    public void AddLength(){
        length.Value += 1;
        InstantiateTail();
        LenghtChanged();
    }

    private void LenghtChanged(){
        if(!IsOwner){
            return;
        }
        ChangedLengthEvent?.Invoke(length.Value );
        ClientMusicPlayer.Instance.PlayNomAudio();
    }

    private void LengthChangedEvent(ushort previousValue, ushort newValue)
    {
        Debug.Log("LengthChanged Callback");
        InstantiateTail();
        LenghtChanged();
    }

    private void InstantiateTail(){
        GameObject tailGameObject = Instantiate(tailPrefab, transform.position, Quaternion.identity); 
        tailGameObject.GetComponent<SpriteRenderer>().sortingOrder = -length.Value;
        if(tailGameObject.TryGetComponent(out Tail tail)){
            tail.networkedOwner = transform;
            tail.followTransform = _lastTail;
            _lastTail = tailGameObject.transform;
            Physics2D.IgnoreCollision(tailGameObject.GetComponent<Collider2D>(), _collider2D);
        }
        _tails.Add(tailGameObject);
    }
}
