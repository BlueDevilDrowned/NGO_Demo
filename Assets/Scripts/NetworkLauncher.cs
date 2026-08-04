using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkLauncher : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private string serverAddress = "127.0.0.1";
    [SerializeField] private ushort port = 7777;

    public void StartHost()
    {
        if (!TryGetNetworkComponents(out NetworkManager networkManager, out UnityTransport transport))
        {
            return;
        }

        transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");

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
}
