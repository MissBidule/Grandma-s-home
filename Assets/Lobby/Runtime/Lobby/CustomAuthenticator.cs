using System;
using System.Threading.Tasks;
using UnityEngine;
using PurrNet.Authentication;
using PurrNet;
using PurrNet.Logging;
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
        try
        {
            // ? FIX BUG #4: Validate RoleKeeper exists
            var roleKeeper = FindAnyObjectByType<RoleKeeper>();
            if (roleKeeper == null)
            {
                PurrLogger.LogError("RoleKeeper not found during authentication!", this);
                return Task.FromResult(new AuthenticationRequest<string>(_password + " UNKNOWN"));
            }
            
            var localMemberId = roleKeeper.GetLocalMemberID();
            if (string.IsNullOrEmpty(localMemberId))
            {
                PurrLogger.LogError("Local member ID is null or empty!", this);
                return Task.FromResult(new AuthenticationRequest<string>(_password + " UNKNOWN"));
            }
            
            return Task.FromResult(new AuthenticationRequest<string>(_password + " " + localMemberId));
        }
        catch (Exception ex)
        {
            PurrLogger.LogError($"Error in GetClientPayload: {ex.Message}", this);
            return Task.FromResult(new AuthenticationRequest<string>(_password + " ERROR"));
        }
    }

    protected override void UnAuthenticateClient(Connection conn)
    {
        Debug.Log("User disconnected.");
    }

    protected override Task<AuthenticationResponse> ValidateClientPayload(Connection conn, string payload)
    {
        try
        {
            // ? FIX BUG #4: Validate payload format
            if (string.IsNullOrEmpty(payload))
            {
                PurrLogger.LogError("Payload is null or empty", this);
                return Task.FromResult(new AuthenticationResponse() { success = false });
            }
            
            var parts = payload.Split(' ');
            if (parts.Length != 2)
            {
                PurrLogger.LogError($"Invalid payload format. Expected 'password memberId', got: {payload}", this);
                return Task.FromResult(new AuthenticationResponse() { success = false });
            }
            
            string receivedPassword = parts[0];
            string memberId = parts[1];
            
            // ? FIX BUG #4: Validate password
            bool isValid = _password == receivedPassword;
            if (!isValid)
            {
                PurrLogger.LogWarning($"Invalid password from connection {conn.connectionId}", this);
                return Task.FromResult(new AuthenticationResponse() { success = false });
            }
            
            // ? FIX BUG #4: Validate RoleKeeper exists before access
            var roleKeeper = FindAnyObjectByType<RoleKeeper>();
            if (roleKeeper == null)
            {
                PurrLogger.LogError("RoleKeeper not found during validation!", this);
                return Task.FromResult(new AuthenticationResponse() { success = false });
            }
            
            // ? FIX BUG #4: Safe setter with validation
            if (!string.IsNullOrEmpty(memberId) && memberId != "UNKNOWN" && memberId != "ERROR")
            {
                roleKeeper.SetConnectionID(memberId, conn.connectionId);
            }
            
            return Task.FromResult(new AuthenticationResponse() { success = true });
        }
        catch (Exception ex)
        {
            PurrLogger.LogError($"Error in ValidateClientPayload: {ex.Message}", this);
            return Task.FromResult(new AuthenticationResponse() { success = false });
        }
    }
}