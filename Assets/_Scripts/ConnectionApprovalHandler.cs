using UnityEngine;
using Unity.Netcode;
public class ConnectionApprovalHandler : MonoBehaviour
{
    private const int MaxPlayers = 10;

    private void Start() {
    NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response){
        Debug.Log("Connection Approved");
        response.Approved = true;
        response.CreatePlayerObject = true; 
        response.PlayerPrefabHash = null;
        if(NetworkManager.Singleton.ConnectedClients.Count >= MaxPlayers){
            response.Approved = true;
            response.Reason = "Server is Full";
        }
        response.Pending = false;
    }   
}
