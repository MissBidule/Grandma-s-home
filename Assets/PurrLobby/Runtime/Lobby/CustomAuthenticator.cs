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
        return Task.FromResult(new AuthenticationRequest<string>(_password + " " +FindAnyObjectByType<RoleKeeper>().getLocalMemberID()));
    }

    protected override void UnAuthenticateClient(Connection conn)
    {
        throw new System.NotImplementedException();
    }

    protected override Task<AuthenticationResponse> ValidateClientPayload(Connection conn, string payload)
    {
        FindAnyObjectByType<RoleKeeper>().setConnectionID(payload.Split(' ')[1], conn.connectionId);
        bool isValid = _password == payload.Split(' ')[0];
        return Task.FromResult(new AuthenticationResponse() {success = isValid});
    }
}