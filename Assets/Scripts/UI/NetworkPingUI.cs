using Unity.Netcode;
using UnityEngine;
using TMPro;

/// <summary>
/// Displays the local NGO client id and round-trip time.
/// Attach this component to a GameObject with a TMP_Text component.
/// </summary>
public sealed class NetworkPingUI : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField, Min(0.05f)] private float refreshInterval = 0.25f;
    [SerializeField] private bool showClientId = true;

    private float nextRefreshTime;

    private void Reset()
    {
        label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        nextRefreshTime = 0f;
        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshInterval);
        Refresh();
    }

    private void Refresh()
    {
        if (label == null)
            return;

        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsListening)
        {
            label.text = "Offline";
            return;
        }

        ulong clientId = manager.LocalClientId;
        ulong rttMs = 0;
        NetworkTransport transport = manager.NetworkConfig.NetworkTransport;
        if (transport != null)
            rttMs = transport.GetCurrentRtt(NetworkManager.ServerClientId);

        label.text = showClientId
            ? $"ID: {clientId}  Ping: {rttMs} ms"
            : $"Ping: {rttMs} ms";
    }
}
