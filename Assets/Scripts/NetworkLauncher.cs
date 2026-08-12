using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkLauncher : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private string serverAddress = "127.0.0.1";
    [SerializeField] private ushort port = 7777;

    [Header("Player Spawn")]
    [SerializeField] private Transform playerSpawnPoint;

    private bool connectionApprovalRegistered;

    public void StartHost()
    {
        if (!TryGetNetworkComponents(out NetworkManager networkManager, out UnityTransport transport))
        {
            return;
        }

        transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
        ConfigureConnectionApproval(networkManager, true);

        if (!networkManager.StartHost())
        {
            Debug.LogError("Failed to start host.", this);
        }
    }

    public void StartClient()
    {
        if (!TryGetNetworkComponents(out NetworkManager networkManager, out UnityTransport transport))
        {
            return;
        }

        transport.SetConnectionData(serverAddress, port);
        ConfigureConnectionApproval(networkManager, false);

        if (!networkManager.StartClient())
        {
            Debug.LogError($"Failed to start client for {serverAddress}:{port}.", this);
        }
    }

    public void StartDedicatedServer()
    {
        if (!TryGetNetworkComponents(out NetworkManager networkManager, out UnityTransport transport))
        {
            return;
        }

        transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
        ConfigureConnectionApproval(networkManager, true);

        if (!networkManager.StartServer())
        {
            Debug.LogError("Failed to start dedicated server.", this);
        }
    }

    public void Disconnect()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null && networkManager.IsListening)
        {
            networkManager.Shutdown();
        }
    }

    public void SetServerAddress(string address)
    {
        if (!string.IsNullOrWhiteSpace(address))
        {
            serverAddress = address.Trim();
        }
    }

    private bool TryGetNetworkComponents(
        out NetworkManager networkManager,
        out UnityTransport transport)
    {
        networkManager = NetworkManager.Singleton;
        transport = null;

        if (networkManager == null)
        {
            Debug.LogError("NetworkManager.Singleton was not found.", this);
            return false;
        }

        if (networkManager.IsListening)
        {
            Debug.LogWarning("NetworkManager is already running.", this);
            return false;
        }

        transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("UnityTransport was not found on the NetworkManager object.", this);
            return false;
        }

        return true;
    }

    private void ConfigureConnectionApproval(NetworkManager networkManager, bool registerCallback)
    {
        networkManager.NetworkConfig.ConnectionApproval = true;

        if (!registerCallback || connectionApprovalRegistered)
            return;

        networkManager.ConnectionApprovalCallback += ApproveConnection;
        connectionApprovalRegistered = true;
    }

    private void ApproveConnection(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = true;

        if (playerSpawnPoint == null)
        {
            response.Position = Vector3.zero;
            response.Rotation = Quaternion.identity;
            Debug.LogWarning(
                "Player spawn point is not assigned. The player will spawn at the world origin.",
                this);
            return;
        }

        response.Position = playerSpawnPoint.position;
        response.Rotation = playerSpawnPoint.rotation;
    }
}
