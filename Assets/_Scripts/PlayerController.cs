 using System;
using System.Collections;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour{
    [SerializeField]private float speed = 3f;

    [CanBeNull]public static event System.Action GameOverEvent;

    private Camera _mainCamera;
    private Vector3 _mouseInput = Vector3.zero;
    private PlayerLength _playerLength;
    private bool _canCollide = true; 

    private readonly ulong[] _targetClientsArray = new ulong[1];

    private void Initialize(){
        _mainCamera = Camera.main;
        _playerLength = GetComponent<PlayerLength>();
    }

    public override void OnNetworkSpawn(){
        base.OnNetworkSpawn();
        Initialize();
    }

    private void Update(){
        if(!IsOwner || !Application.isFocused) return;
        MovePlayerServer();
        
    }

    private void MovePlayerServer(){
        _mouseInput.x = Input.mousePosition.x;
        _mouseInput.y = Input.mousePosition.y;
        _mouseInput.z = _mainCamera.nearClipPlane;
        Vector3 mouseWorldCoordinates = _mainCamera.ScreenToWorldPoint(_mouseInput);
        mouseWorldCoordinates.z = 0f;

        MovePlayerServerRpc(mouseWorldCoordinates);
    }

    [ServerRpc]
    private void MovePlayerServerRpc(Vector3 mouseWorldCoordinates){
        transform.position = Vector3.MoveTowards(transform.position, mouseWorldCoordinates, Time.deltaTime * speed);

        //Rotation
        if(mouseWorldCoordinates != transform.position){
            Vector3 targetDirection = mouseWorldCoordinates - transform.position;
            targetDirection.z = 0f;
            transform.up = targetDirection;
        }
    }

    //Client Authoritative Movement
    private void MoveClient(){
        // Debug.Log("Movement");
        //Movement
        _mouseInput.x = Input.mousePosition.x;
        _mouseInput.y = Input.mousePosition.y;
        _mouseInput.z = _mainCamera.nearClipPlane;
        Vector3 mouseWorldCoordinates = _mainCamera.ScreenToWorldPoint(_mouseInput);
        mouseWorldCoordinates.z = 0f;
        transform.position = Vector3.MoveTowards(transform.position, mouseWorldCoordinates, Time.deltaTime * speed);

        //Rotation
        if(mouseWorldCoordinates != transform.position){
            Vector3 targetDirection = mouseWorldCoordinates - transform.position;
            targetDirection.z = 0f;
            transform.up = targetDirection;
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void DetermineWinnerCollisionServerRpc(PlayerData Player1, PlayerData Player2){
        if(Player1.Length > Player2.Length){
            WinInforationServerRpc(Player1.Id, Player2.Id);
        }
        else{
            WinInforationServerRpc(Player2.Id, Player1.Id);
        }
    }

    [ServerRpc]
    private void WinInforationServerRpc(ulong winner, ulong loser){
        _targetClientsArray[0] = winner;
        ClientRpcParams clientRpcParams = new ClientRpcParams{
            Send = new ClientRpcSendParams{
                TargetClientIds = _targetClientsArray
            }
        };
        AtePlayerClientRpc(clientRpcParams);

        _targetClientsArray[0] = loser;
        clientRpcParams.Send.TargetClientIds = _targetClientsArray;

        GameOverClientRpc(clientRpcParams);

    }

    [ClientRpc]
    private void AtePlayerClientRpc(ClientRpcParams clientRpcParams  = default){
        if(!IsOwner) return;
        Debug.Log("You ate a player");
    }

    [ClientRpc]
    private void GameOverClientRpc(ClientRpcParams clientRpcParams  = default){
        if(!IsOwner) return;
        Debug.Log("You lose");
        GameOverEvent?.Invoke();
        NetworkManager.Singleton.Shutdown(); 
    }

    private IEnumerator CollisionCheckCoroutine(){
        _canCollide = false;
        yield return new WaitForSeconds(0.5f);
        _canCollide = true;
    }

    private void OnCollisionEnter2D(Collision2D col) {
        Debug.Log("Player Collision");
        if(!col.gameObject.CompareTag("Player")) return;
        if(!IsOwner) return;
        if(!_canCollide) return;
        StartCoroutine(CollisionCheckCoroutine());

        //Head-on collision
        if(col.gameObject.TryGetComponent(out PlayerLength playerLength)){
            Debug.Log("Head Collision");
            var Player1 = new PlayerData(){
                Id = OwnerClientId,
                Length = _playerLength.length.Value
            };

            var Player2 = new PlayerData(){
                Id = playerLength.OwnerClientId,
                Length = playerLength.length.Value
            };
            DetermineWinnerCollisionServerRpc(Player1, Player2);
        }
        else if(col.gameObject.TryGetComponent(out Tail tail)){
            Debug.Log("Tail Collision");
            WinInforationServerRpc(tail .networkedOwner.GetComponent<PlayerController>().OwnerClientId, OwnerClientId);
        }
    }

    struct PlayerData : INetworkSerializable
    {
        public ulong Id;
        public ushort Length;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Id);
            serializer.SerializeValue(ref Length);
            
        }
    }
}

