using System.Threading.Tasks;
using UnityEngine;
using PurrNet.Authentication;
using PurrNet;
using PurrNet.Transports;
using PurrLobby;

// this attributes ensures the correct Type variant is registered
[RegisterNetworkType(typeof(AuthenticationRequest<string>))]
public class CustomAuthenticator : AuthenticationBehaviour<string>
{
    [Tooltip("The password required to authenticate the client.")]
    [SerializeField]
    private string _password = "YourSecretPassword";

    protected override Task<AuthenticationRequest<string>> GetClientPayload()
    {
        // the client will send his password and ID to the server
        RoleKeeper roleKeeper = FindAnyObjectByType<RoleKeeper>();
        string localMemberId = roleKeeper.GetLocalMemberID();
        string payload = _password + " " + localMemberId;
        
        Debug.Log($"[Client] Sending authentication payload with roleId: {localMemberId}");
        
        return Task.FromResult(new AuthenticationRequest<string>(payload));
    }

    protected override void UnAuthenticateClient(Connection conn)
    {
        Debug.Log("User disconnected.");
    }

    protected override Task<AuthenticationResponse> ValidateClientPayload(Connection conn, string payload)
    {
        string[] parts = payload.Split(' ');
        if (parts.Length < 2)
        {
            Debug.LogError($"[Server] Invalid payload format: {payload}");
            return Task.FromResult(new AuthenticationResponse() { success = false });
        }
        
        string roleId = parts[1];
        int connectionId = conn.connectionId;
        
        Debug.Log($"[Server] Validating client - roleId: {roleId}, connectionId: {connectionId}");
        
        // Set connection ID on server side
        FindAnyObjectByType<RoleKeeper>().SetConnectionID(roleId, connectionId);
        
        bool isValid = _password == parts[0];
        Debug.Log($"[Server] Authentication valid: {isValid}");
        
        return Task.FromResult(new AuthenticationResponse() { success = isValid });
    }
}

