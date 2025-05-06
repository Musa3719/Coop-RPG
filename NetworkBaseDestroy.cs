using Unity.Netcode;
using UnityEngine;

public class NetworkBaseDestroy : MonoBehaviour
{
    private void Awake()
    {
        if (FindObjectsByType<NetworkManager>(FindObjectsSortMode.None).Length > 1)
            Destroy(gameObject);
    }
}
