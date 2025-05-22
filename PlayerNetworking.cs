using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerNetworking : NetworkBehaviour
{
    public NetworkVariable<bool> _IsLoadingScene;
    public NetworkObject _NetworkObject { get; set; }


    public GameObject _ClosestInventory { get; private set; }
    private float _closestInventoryDistance;

    private void OnEnable()
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoad;
    }
    private void OnDisable()
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoad;
    }

    public override void OnNetworkSpawn()
    {
        GameManager._Instance.SetPlayersFromConnection();

        SpawnRpc(NetworkObject.OwnerClientId);

        _NetworkObject = GetComponent<NetworkObject>();
        if (!IsOwner)
        {
            GetComponent<LocomotionSystem>().enabled = false;
            GetComponent<PlayerInputHandler>().enabled = false;
        }
        else
        {
            GetComponent<PlayerInputHandler>().InitilizeController();
            if (!IsServer)
                NetworkMethods._Instance.RequestGameStateRpc(NetworkManager.LocalClientId);
        }
    }
    public override void OnNetworkDespawn()
    {
        PlayerDespawned(NetworkObject.OwnerClientId);
    }

    protected virtual void Update()
    {
        if (!_NetworkObject.IsOwner) return;

        GetComponent<PlayerInputHandler>().ArrangeCameraFollow();
        CheckForClosestInventory();
    }
    private void CheckForClosestInventory()
    {
        _closestInventoryDistance = 2f;
        _ClosestInventory = null;
        foreach (var staticInventory in GameManager._Instance._AllStaticInventories)
        {
            if ((staticInventory.transform.position - transform.position).magnitude < 1f)
            {
                Physics.Raycast(transform.position, (staticInventory.transform.position - transform.position).normalized, out RaycastHit hit, 10f, GameManager._Instance._UseInventoryLayerMask);
                if (hit.collider != null && hit.collider.transform.parent != null && hit.collider.transform.parent.gameObject == staticInventory)
                {
                    if ((staticInventory.transform.position - transform.position).magnitude < _closestInventoryDistance)
                    {
                        _closestInventoryDistance = (staticInventory.transform.position - transform.position).magnitude;
                        _ClosestInventory = staticInventory;
                    }
                }
            }
        }
    }

    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    public void SpawnRpc(ulong ID)
    {
        if (!GameManager._Instance._Players.ContainsKey(ID))
            GameManager._Instance._Players.Add(ID, GetNetworkObject(ID).gameObject);
    }

    public void PlayerDespawned(ulong ID)
    {
        if (IsServer && !IsOwner)
            SaveSystemHandler.SaveOnePlayerDataWhenDisconnect(ID);

        if (GameManager._Instance._Players.ContainsKey(ID))
            GameManager._Instance._Players.Remove(ID);
    }

    public void OnLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (!GetComponent<NetworkObject>().IsSpawned) Debug.LogError("Loaded but not spawned!!");

        if (IsOwner)
            OnLoadRpc();
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void OnLoadRpc()
    {
        _IsLoadingScene.Value = false;
    }




}