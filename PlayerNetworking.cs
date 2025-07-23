using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerNetworking : NetworkBehaviour
{
    public int _ID;
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

    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    private void SetIDRpc(int id, ulong requesterID, bool isSendingRpc)
    {
        if (!isSendingRpc && requesterID != NetworkManager.LocalClientId) return;

        if (!NetworkController._Instance._IDToClientID.ContainsKey(id))
            NetworkController._Instance._IDToClientID.Add(id, GetComponent<PlayerNetworking>().OwnerClientId);
        _ID = id;
        NetworkController._Instance.SetPlayersFromConnection();

        if (isSendingRpc && requesterID != NetworkManager.LocalClientId)
            NetworkController._Instance.GetOwnPlayerObject().GetComponent<PlayerNetworking>().SetIDRpc(NetworkController._Instance._PlayerID, requesterID, false);
    }


    public override void OnNetworkSpawn()
    {
        _NetworkObject = GetComponent<NetworkObject>();

        if (IsOwner)
            SetIDRpc(NetworkController._Instance._PlayerID, NetworkManager.LocalClientId, true);


        if (!IsOwner)
        {
            GetComponent<LocomotionSystem>().enabled = false;
            GetComponent<PlayerInputHandler>().enabled = false;
        }
        else
            GetComponent<PlayerInputHandler>().InitilizeController();

        Invoke("NetworkSpawnWithWait", 0.5f);

    }
    public void NetworkSpawnWithWait()
    {
        if (_ID == 0)
        {
            Debug.LogError("ID is not initialized!!");
            return;
        }

        if (!NetworkController._Instance._Players.ContainsKey(_ID))
            NetworkController._Instance._Players.Add(_ID, gameObject);

        if (IsOwner && !IsServer)
        {
            NetworkController._Instance.RequestGameStateRpc(_ID, NetworkObjectId);
        }
    }
    public override void OnNetworkDespawn()
    {
        if (NetworkController._Instance._IDToClientID.ContainsKey(_ID))
            NetworkController._Instance._IDToClientID.Remove(_ID);

        if (IsServer && !IsOwner && !NetworkController._Instance._DisconnectedPlayers.ContainsKey(_ID))
        {
            GameObject copyObject = Instantiate(gameObject);
            copyObject.transform.position = transform.position;
            copyObject.transform.rotation = transform.rotation;
            copyObject.name = "copy";
            copyObject.GetComponent<Inventory>()._Items = GetComponent<Inventory>()._Items;
            copyObject.GetComponent<Inventory>()._Equipments = GetComponent<Inventory>()._Equipments;
            copyObject.GetComponent<Humanoid>().CopyHumanData(GetComponent<Humanoid>());
            Destroy(copyObject.GetComponent<NetworkObject>());
            copyObject.transform.parent = NetworkController._Instance.transform;
            copyObject.SetActive(false);
            NetworkController._Instance._DisconnectedPlayers.Add(_ID, copyObject);
        }

        if (NetworkController._Instance._Players.ContainsKey(_ID))
            NetworkController._Instance._Players.Remove(_ID);
    }

    protected virtual void Update()
    {
        if (!_NetworkObject.IsOwner) return;

        GetComponent<PlayerInputHandler>().ArrangeCameraFollow();
        CheckForClosestInventory();
    }
    public void SetPlayerFromDisconnectPlayers()
    {
        if (NetworkController._Instance._DisconnectedPlayers[_ID] == null)
        {
            Debug.LogError("dict value is null");
            return;
        }

        GetComponent<Inventory>()._Items = NetworkController._Instance._DisconnectedPlayers[_ID].GetComponent<Inventory>()._Items;
        GetComponent<Inventory>()._Equipments = NetworkController._Instance._DisconnectedPlayers[_ID].GetComponent<Inventory>()._Equipments;
        transform.position = NetworkController._Instance._DisconnectedPlayers[_ID].transform.position;
        transform.rotation = NetworkController._Instance._DisconnectedPlayers[_ID].transform.rotation;
        GetComponent<Humanoid>().CopyHumanData(NetworkController._Instance._DisconnectedPlayers[_ID].GetComponent<Humanoid>());

        GetComponent<Inventory>().SyncInventory();

        Destroy(NetworkController._Instance._DisconnectedPlayers[_ID]);
        NetworkController._Instance._DisconnectedPlayers.Remove(_ID);
    }
    public void SyncPlayerData(bool isForLatejoin = false, ulong requesterID = 1000)
    {
        if (!IsServer)
            Debug.LogError("Sync Called From Client!");
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

    public void AllClientsLoaded()
    {

    }

    #region InputMethods

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void SplitRequestRpc(int itemIndex, ulong inventoryNetworkID, string itemName, bool isEquipped, int count)
    {
        Inventory inventory = NetworkController._Instance.GetObjectFromNetworkID(inventoryNetworkID).GetComponent<Inventory>();
        if (inventory == null) return;
        Item item = isEquipped ? inventory._Equipments[itemIndex] : inventory._Items[itemIndex];
        if (item == null) return;

        if (itemIndex == -1 || item._IsEquipped != isEquipped || item._Name != itemName) return;
        if (count == 0 || count >= item._Count) return;
        if (item._Count <= 1 || inventory.IsFull()) return;

        Split(count, item, inventory);
    }
    private void Split(int count, Item item, Inventory inventory)
    {
        if (!IsServer) { Debug.LogError("Split Called in Client!"); return; }

        int index = inventory._Items.GetFirstEmptyIndex();
        int splitAmount = count == -1 ? item._Count / 2 : count;
        item._Count -= splitAmount;
        Item newItem = item.Copy();
        newItem._IsEquipped = false;
        inventory.GainItem(newItem, splitAmount, index);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void CombineAllSameItemsRequestRpc(ulong inventoryNetworkID, int itemIndex, bool isEquipped, string itemName)
    {
        Inventory inventory = NetworkController._Instance.GetObjectFromNetworkID(inventoryNetworkID).GetComponent<Inventory>();
        if (inventory == null) return;
        Item item = isEquipped ? inventory._Equipments[itemIndex] : inventory._Items[itemIndex];
        if (item == null) return;

        if (itemIndex == -1 || item._IsEquipped != isEquipped || item._Name != itemName) return;
        CombineAllSameItems(item, inventory);
    }
    private void CombineAllSameItems(Item item, Inventory inventory)
    {
        if (!IsServer) { Debug.LogError("CombineAllSameItems Called in Client!"); return; }

        if (item == null || item.IsUniqueItemType())
        {
            Debug.Log("Item is unique tpye!!");
            return;
        }

        for (int i = 0; i < inventory._Items.Length; i++)
        {
            if (inventory._Items[i] != null && inventory._Items[i] != item && !inventory._Items[i].IsUniqueItemType() && inventory._Items[i]._Name == item._Name)
            {
                item._Count += inventory._Items[i]._Count;
                inventory._Items.Remove(inventory._Items[i], inventory, i);
            }
        }
        for (int i = 0; i < inventory._Equipments.Length; i++)
        {
            if (inventory._Equipments[i] != null && inventory._Equipments[i] != item && !inventory._Equipments[i].IsUniqueItemType() && inventory._Equipments[i]._Name == item._Name)
            {
                item._Count += inventory._Equipments[i]._Count;
                NetworkController._Instance.UnEquipRequestSend(inventory._Equipments[i], inventory, i, false, false);
            }
        }

        inventory.SyncInventory();
        GameManager._Instance.CheckInventoryUpdate(inventory);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void ItemExchangeRequestRpc(ulong inventory1NetworkID, int item1Index, bool isEquipped1, string item1Name, ulong inventory2NetworkID, int item2Index, bool isEquipped2, string item2Name)
    {
        Inventory inventory1 = NetworkController._Instance.GetObjectFromNetworkID(inventory1NetworkID).GetComponent<Inventory>();
        if (inventory1 == null) return;
        Item item1 = isEquipped1 ? inventory1._Equipments[item1Index] : inventory1._Items[item1Index];
        if (item1 == null) return;

        Inventory inventory2 = NetworkController._Instance.GetObjectFromNetworkID(inventory2NetworkID).GetComponent<Inventory>();
        if (inventory2 == null) return;
        Item item2 = isEquipped2 ? inventory2._Equipments[item2Index] : inventory2._Items[item2Index];
        if (item2 == null) return;

        if (item1Index == -1 || item1._IsEquipped != isEquipped1 || item1._Name != item1Name) return;
        if (item2Index == -1 || item2._IsEquipped != isEquipped2 || item2._Name != item2Name) return;
        ItemExchance(inventory1, item1Index, item1, inventory2, item2Index, item2);
    }
    private void ItemExchance(Inventory firstInventory, int firstIndex, Item firstItem, Inventory secondInventory, int secondIndex, Item secondItem)
    {
        if (!IsServer) { Debug.LogError("ItemExchance Called in Client!"); return; }

        if (firstItem == null || secondItem == null || firstItem == secondItem) return;

        if ((firstItem._IsEquipped && !firstInventory.CanEquipThisItemType(secondItem, firstIndex)) || (secondItem._IsEquipped && !secondInventory.CanEquipThisItemType(firstItem, secondIndex)))
            return;

        bool isFirstItemFromEquipments = firstItem._IsEquipped;
        bool isSecondItemFromEquipments = secondItem._IsEquipped;
        int firstItemCount = firstItem._Count;
        int secondItemCount = secondItem._Count;

        if (!firstItem.IsUniqueItemType() && !secondItem.IsUniqueItemType() && firstItem._Name == secondItem._Name)
        {
            CombineTwoItem(firstInventory, firstIndex, firstItem, secondInventory, secondIndex, secondItem);
            return;
        }

        //losing
        if (firstItem._IsEquipped)
        {
            NetworkController._Instance.UnEquipRequestSend(firstItem, firstInventory, firstIndex, false, false);
        }
        else
        {
            firstInventory.LoseItem(firstItem, firstItem._Count, firstIndex, false);
        }

        if (secondItem._IsEquipped)
        {
            NetworkController._Instance.UnEquipRequestSend(secondItem, secondInventory, secondIndex, false, false);
        }
        else
        {
            secondInventory.LoseItem(secondItem, secondItem._Count, secondIndex, false);
        }

        //gaining
        if (isFirstItemFromEquipments)
        {
            secondItem._IsEquipped = true;
            firstInventory._Equipments[firstIndex] = secondItem;
            firstInventory.CreateWorldInstanceForItem(secondItem._WorldInstanceIndex, firstIndex, firstInventory.GetComponent<NetworkObject>().NetworkObjectId);
        }
        else
        {
            firstInventory.GainItem(secondItem, secondItemCount, firstIndex, false);
        }

        if (isSecondItemFromEquipments)
        {
            firstItem._IsEquipped = true;
            secondInventory._Equipments[secondIndex] = firstItem;
            secondInventory.CreateWorldInstanceForItem(firstItem._WorldInstanceIndex, secondIndex, secondInventory.GetComponent<NetworkObject>().NetworkObjectId);
        }
        else
        {
            secondInventory.GainItem(firstItem, firstItemCount, secondIndex, false);
        }

        GameManager._Instance.CheckInventoryUpdate(firstInventory);
        GameManager._Instance.CheckInventoryUpdate(secondInventory);
        firstInventory.SyncInventory();
        secondInventory.SyncInventory();
    }

    private void CombineTwoItem(Inventory firstInventory, int firstIndex, Item firstItem, Inventory secondInventory, int secondIndex, Item secondItem, bool isUpdating = true)
    {
        if (!IsServer) { Debug.LogError("CombineTwoItem Called in Client!"); return; }

        int firstItemCount = firstItem._Count;

        if (firstItem._IsEquipped)
            NetworkController._Instance.UnEquipRequestSend(firstItem, firstInventory, firstIndex, false, false);
        else
            firstInventory.LoseItem(firstItem, firstItemCount, firstIndex, false);

        if (secondItem._IsEquipped)
            secondItem._Count += firstItemCount;
        else
            secondInventory.GainItem(firstItem, firstItemCount, secondIndex, false);

        if (isUpdating)
        {
            firstInventory.SyncInventory();
            GameManager._Instance.CheckInventoryUpdate(firstInventory);
            if (secondInventory != firstInventory)
            {
                secondInventory.SyncInventory();
                GameManager._Instance.CheckInventoryUpdate(secondInventory);
            }
        }

    }


   

    #endregion

}