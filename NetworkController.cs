using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using Unity.Netcode.Components;

public class NetworkController : NetworkBehaviour
{
    public int _PlayerID;

    public static NetworkController _Instance;

    public Coroutine _LoadGameCoroutine;

    private Dictionary<int, GameObject> _players;
    public Dictionary<int, GameObject> _Players { get { if (_players.Count == 0) SetPlayersFromConnection(); return _players; } private set { _players = value; } }//ulong is client id
    public Dictionary<int, GameObject> _DisconnectedPlayers { get; set; }
    public Dictionary<int, ulong> _IDToClientID { get; set; }

    private GameObject _ownPlayerObject { get; set; }

    private void Awake()
    {
        _Players = new Dictionary<int, GameObject>();
        _DisconnectedPlayers = new Dictionary<int, GameObject>();
        _IDToClientID = new Dictionary<int, ulong>();

        if (FindObjectsByType<NetworkController>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        _Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (FindObjectsByType<NetworkController>(FindObjectsSortMode.None).Length > 1)
        {
            return;
        }

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        if (IsServer)
            SaveSystemHandler.LoadGame(GameManager._Instance._SaveIndex);
    }
    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }
    private void Update()
    {
        foreach (var item in _Players)
        {
            if (item.Value == null)
                Debug.Log("Item value null : " + item.Key);
        }
        foreach (var item in _DisconnectedPlayers)
        {
            if (item.Value == null)
                Debug.Log("Item value null : " + item.Key);
        }


        if (!NetworkManager.IsApproved)
        {
            if (!GameObject.FindGameObjectWithTag("UI").transform.Find("Disconnected").gameObject.activeInHierarchy)
                GameObject.FindGameObjectWithTag("UI").transform.Find("Disconnected").gameObject.SetActive(true);
        }
        else
        {
            if (GameObject.FindGameObjectWithTag("UI").transform.Find("Disconnected").gameObject.activeInHierarchy)
                GameObject.FindGameObjectWithTag("UI").transform.Find("Disconnected").gameObject.SetActive(false);
        }
    }
    public void SetPlayersFromConnection()
    {
        _players.Clear();
        var playerObjects = GameObject.FindGameObjectsWithTag("Player");
        foreach (var id in NetworkController._Instance.NetworkManager.ConnectedClientsIds)
        {
            foreach (var player in playerObjects)
            {
                if (player.GetComponent<NetworkObject>().OwnerClientId == id)
                {
                    if (!_players.ContainsKey(player.GetComponent<PlayerNetworking>()._ID))
                        _players.Add(player.GetComponent<PlayerNetworking>()._ID, player);
                    continue;
                }
            }
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        return;
        if (clientId == NetworkManager.LocalClientId)
        {
            Debug.Log("Disconnected from server.");
            NetworkManager.Shutdown();
            StartCoroutine(TryReconnect());
        }
    }
    private IEnumerator TryReconnect(float delaySeconds = 5f)
    {
        yield return new WaitForSecondsRealtime(1);
        while (!NetworkManager.IsApproved)
        {
            Debug.Log("Trying to reconnect...");
            NetworkManager.Singleton.StartClient();
            yield return new WaitForSecondsRealtime(delaySeconds);
        }

    }

    public GameObject GetOwnPlayerObject()
    {
        if (_ownPlayerObject != null) return _ownPlayerObject;

        foreach (var player in FindObjectsByType<PlayerNetworking>(FindObjectsSortMode.None))
        {
            if (player.GetComponent<NetworkObject>().IsOwner)
            {
                _ownPlayerObject = player.gameObject;
                return _ownPlayerObject;
            }
        }
        Debug.LogError("Player Object Cannot be found!");
        return null;
    }

    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    public void SetPlayersFromConnectionRpc()
    {
        SetPlayersFromConnection();
    }

    #region LoadGameMethods
    public bool IsAllPlayersReady()
    {
        foreach (var player in _Players)
        {
            if (player.Value.GetComponent<PlayerNetworking>()._IsLoadingScene.Value)
                return false;
        }
        return true;
    }
    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    public void LoadGameStartedRpc()
    {
        GameManager._Instance._LoadingScreen.SetActive(true);

        foreach (var player in _Players)
        {
            player.Value.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            player.Value.GetComponent<Rigidbody>().isKinematic = true;
            player.Value.GetComponent<NetworkTransform>().enabled = false;
            player.Value.GetComponent<NetworkRigidbody>().enabled = false;
        }

        GameManager._Instance._IsGameLoading = true;
        GameManager._Instance.StopGame(false);
    }
    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    public void LoadGameEndedRpc()
    {
        foreach (var player in _Players)
        {
            player.Value.GetComponent<Rigidbody>().isKinematic = false;
            player.Value.GetComponent<NetworkTransform>().enabled = true;
            player.Value.GetComponent<NetworkRigidbody>().enabled = true;

            if (player.Value.GetComponent<NetworkObject>().IsOwner)
            {
                player.Value.GetComponent<NetworkTransform>().Teleport(player.Value.transform.position, player.Value.transform.rotation, Vector3.one);//for update positions from first frame
            }

            player.Value.GetComponent<PlayerNetworking>().AllClientsLoaded();
        }

        GameManager._Instance._LoadingScreen.SetActive(false);
        GameManager._Instance._IsGameLoading = false;
        GameManager._Instance.UnstopGame();
    }


    public IEnumerator LoadGameCoroutine(int index, bool isForLatejoin = false, int requesterID = 1000)
    {
        LoadGameStartedRpc();

        SavedDataBlock data = SaveSystemHandler.LoadGameData(index);

        if (!isForLatejoin)
        {
            foreach (var player in _Players)
            {
                player.Value.GetComponent<PlayerNetworking>()._IsLoadingScene.Value = true;
            }

            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);

            if (data == null) yield break;

            //Debug.Log("Load Game Started!");
            while (!IsAllPlayersReady())
            {
                //Debug.Log("Waiting one frame...");
                yield return null;
            }
            Debug.Log("Game Loaded!");

            foreach (var chest in data._GameData._ChestsWillBeSpawned._AllInventories)
            {
                int i = data._GameData._ChestsWillBeSpawned._AllInventories.IndexOf(chest);
                GameObject newObj = Instantiate(GameManager._Instance._AllNetworkPrefabs[1], data._GameData._PositionOfChestsWillBeSpawned[i], Quaternion.identity);
                newObj.transform.localEulerAngles = data._GameData._RotationOfChestsWillBeSpawned[i];
                newObj.GetComponent<Inventory>()._Items = chest._Items;
                newObj.GetComponent<Inventory>()._Items.SetNullFromSave();
                newObj.GetComponent<NetworkObject>().Spawn();
            }
            foreach (var pocket in data._GameData._PocketsWillBeSpawned._AllInventories)
            {
                int i = data._GameData._PocketsWillBeSpawned._AllInventories.IndexOf(pocket);
                GameObject newObj = Instantiate(GameManager._Instance._AllNetworkPrefabs[0], data._GameData._PositionOfPocketsWillBeSpawned[i], Quaternion.identity);
                newObj.transform.localEulerAngles = data._GameData._RotationOfPocketsWillBeSpawned[i];
                newObj.GetComponent<Inventory>()._Items = pocket._Items;
                newObj.GetComponent<Inventory>()._Items.SetNullFromSave();
                newObj.GetComponent<NetworkObject>().Spawn();
            }
        }

        //load game data (&latejoin)

        SetPlayersFromConnectionRpc();

        //load players data (&latejoin)

        foreach (var playerData in data._PlayerData)
        {
            if ((!_Players.ContainsKey(playerData._NetworkID) && !_DisconnectedPlayers.ContainsKey(playerData._NetworkID)) || (isForLatejoin && playerData._NetworkID != requesterID))
            {
                continue;
            }

            LoadOnePlayerData(playerData, isForLatejoin, requesterID);
        }

        GameManager._Instance.CallForAction(LoadGameEndedRpc, 0.2f, true);
    }

    private void LoadOnePlayerData(PlayerData data, bool isForLatejoin = false, int requesterID = 1000)
    {
        LoadOnePlayerTransformRpc(data._NetworkID, data._Position, data._EulerAngleY, isForLatejoin, requesterID);

        GameObject playerObj = _Players.ContainsKey(data._NetworkID) ? _Players[data._NetworkID] : _DisconnectedPlayers[data._NetworkID];
        playerObj.GetComponent<Inventory>()._Items = data._Inventory._Items;
        playerObj.GetComponent<Inventory>()._Items.SetNullFromSave();
        playerObj.GetComponent<Inventory>()._Equipments = data._Inventory._Equipments;
        playerObj.GetComponent<Inventory>()._Equipments.SetNullFromSave();
        ulong id = _IDToClientID.ContainsKey(requesterID) ? _IDToClientID[requesterID] : 0;
        if (_Players.ContainsKey(data._NetworkID))
        {
            playerObj.GetComponent<Inventory>().SyncInventory(id, isForLatejoin, true);
            playerObj.GetComponent<PlayerNetworking>().SyncPlayerData(isForLatejoin, id);
        }

    }
    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    public void LoadOnePlayerTransformRpc(int playerID, Vector3 pos, float yAngle, bool isSendingToClientID = false, int clientID = 1)
    {
        if (isSendingToClientID && NetworkController._Instance.GetOwnPlayerObject().GetComponent<PlayerNetworking>()._ID != clientID) return;

        GameObject playerObj = _Players.ContainsKey(playerID) ? _Players[playerID] : _DisconnectedPlayers[playerID];

        if (playerObj == null) return;

        playerObj.transform.position = pos;
        playerObj.transform.localEulerAngles = new Vector3(playerObj.transform.localEulerAngles.x, yAngle, playerObj.transform.localEulerAngles.z);
    }

    #endregion

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void RequestGameStateRpc(int requesterID, ulong networkObjectID)
    {
        if (_DisconnectedPlayers.ContainsKey(requesterID))
        {
            GetObjectFromNetworkID(networkObjectID).GetComponent<PlayerNetworking>().SetPlayerFromDisconnectPlayers();
        }
        else
        {
            StartCoroutine(LoadGameCoroutine(GameManager._Instance._LastLoadedGameIndex, true, requesterID));
        }
    }


    #region ItemMethods

    public void EquipRequestSend(Item item, Humanoid userHuman, Inventory losingInventory, int fromIndex = -1, int equipIndex = -1, bool isSync = true)
    {
        if (fromIndex == -1)
            fromIndex = item._IsEquipped ? losingInventory._Equipments.IndexOf(item) : losingInventory._Items.IndexOf(item);

        EquipRequestRpc(userHuman.GetComponent<PlayerNetworking>().NetworkObjectId, losingInventory.NetworkObjectId, fromIndex, item._IsEquipped, item._Name, equipIndex, isSync);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void EquipRequestRpc(ulong userPlayerID, ulong inventoryNetworkID, int itemIndex, bool isEquipped, string itemName, int equipIndex, bool isSync)
    {
        Inventory inventory = GetObjectFromNetworkID(inventoryNetworkID).GetComponent<Inventory>();
        if (inventory == null) return;
        Item item = isEquipped ? inventory._Equipments[itemIndex] : inventory._Items[itemIndex];
        if (item == null) return;

        if (itemIndex == -1 || item._IsEquipped != isEquipped || item._Name != itemName) return;
        Equip(item, GetObjectFromNetworkID(userPlayerID).GetComponent<Humanoid>(), GetObjectFromNetworkID(inventoryNetworkID).GetComponent<Inventory>(), itemIndex, equipIndex, isSync);
    }
    private void Equip(Item item, Humanoid userHuman, Inventory losingInventory, int fromIndex, int equipIndex, bool isSync)
    {
        if (!NetworkController._Instance.IsServer)
        {
            Debug.LogError("Equip Called From Client!");
            return;
        }

        Inventory inventory = userHuman.GetComponent<Inventory>();
        if (equipIndex == -1)
            equipIndex = item.GetEquipIndex(inventory);
        if (fromIndex == -1)
            fromIndex = inventory._Items.IndexOf(item);

        if (!inventory.CanEquipThisItemType(item, equipIndex))
        {
            Debug.LogError("Item Type Is Wrong!");
            return;
        }
        if (inventory != losingInventory && losingInventory.GetComponent<Humanoid>() != null)
            Debug.LogError("Another Inventory is a Humanoid!");

        if (item._IsEquipped)
        {
            UnEquipRequestSend(item, losingInventory, losingInventory._Equipments.IndexOf(item), false, false);
        }
        else
        {
            losingInventory._Items.Remove(item, losingInventory, fromIndex);
        }

        if (inventory._Equipments[equipIndex] != null)
        {
            if (!inventory.CanTakeThisItem(inventory._Equipments[equipIndex]) && losingInventory != inventory)
                losingInventory.GainItem(inventory._Equipments[equipIndex], inventory._Equipments[equipIndex]._Count, fromIndex, false);
            UnEquipRequestSend(inventory._Equipments[equipIndex], inventory, equipIndex, false);
        }

        item._IsEquipped = true;
        inventory._Equipments[equipIndex] = item;
        inventory.CreateWorldInstanceForItem(item._WorldInstanceIndex, equipIndex, inventory.GetComponent<NetworkObject>().NetworkObjectId);

        GameManager._Instance.CheckInventoryUpdate(inventory);
        if (inventory != losingInventory)
            GameManager._Instance.CheckInventoryUpdate(losingInventory);
        if (isSync)
            inventory.SyncInventory();
        if (losingInventory != inventory && isSync)
            losingInventory.SyncInventory();
    }

    public void UnEquipRequestSend(Item item, Inventory inventory, int fromIndex, bool isSync = true, bool isTaking = true)
    {
        if (fromIndex == -1)
            fromIndex = item._IsEquipped ? inventory._Equipments.IndexOf(item) : inventory._Items.IndexOf(item);

        UnEquipRequestRpc(inventory.NetworkObjectId, fromIndex, item._IsEquipped, item._Name, isSync, isTaking);
    }
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void UnEquipRequestRpc(ulong inventoryNetworkID, int itemIndex, bool isEquipped, string itemName, bool isSync, bool isTaking)
    {
        Inventory inventory = GetObjectFromNetworkID(inventoryNetworkID).GetComponent<Inventory>();
        if (inventory == null) return;
        Item item = isEquipped ? inventory._Equipments[itemIndex] : inventory._Items[itemIndex];
        if (item == null) return;

        if (itemIndex == -1 || item._IsEquipped != isEquipped || item._Name != itemName) return;
        UnEquip(item, inventory, itemIndex, isSync, isTaking);
    }
    private void UnEquip(Item item, Inventory inventory, int fromIndex, bool isSync, bool isTaking)
    {
        if (!NetworkController._Instance.IsServer)
        {
            Debug.LogError("Unequip Called From Client!");
            return;
        }

        if (isTaking && !inventory.CanTakeThisItem(item))
            Debug.LogError("Wants to take but inventory is full");

        inventory._Equipments[fromIndex] = null;
        inventory.DestroyWorldInstanceForItem(fromIndex, inventory.GetComponent<NetworkObject>().NetworkObjectId);

        item._IsEquipped = false;
        if (isTaking)
            inventory.TakeItemFromNothing(item, item._Count, isSync);//also calls SyncInventory 
        if (isSync && !isTaking)
        {
            inventory.SyncInventory();
        }
        GameManager._Instance.CheckInventoryUpdate(inventory);
    }

    /*[Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void SpawnPocketWithItemRpc(int itemIndex, Vector3 pos, int dropCount)
    {
        Item item = GetNewItemByIndex(itemIndex, dropCount);
        SpawnPocketWithItem(item, pos, dropCount);
    }
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void SpawnPocketWithItemRpc(Item item, Vector3 pos, int dropCount)
    {
        SpawnPocketWithItem(item, pos, dropCount);
    }*/
    public void SpawnPocketWithItem(Item item, Vector3 pos, int dropCount)
    {
        if (CheckForNearPockets(pos, item) != null)
        {
            GameObject temp = CheckForNearPockets(pos, item);
            temp.GetComponent<Inventory>().GainItem(item, dropCount, -1);
            return;
        }

        GameObject obj = Instantiate(GameManager._Instance._AllNetworkPrefabs[0], pos + Vector3.up * 0.1f, Quaternion.identity);
        obj.GetComponent<Inventory>().GainItem(item, dropCount, -1, false);
        obj.GetComponent<NetworkObject>().Spawn(true);
    }
    private GameObject CheckForNearPockets(Vector3 spawnPos, Item item)
    {
        foreach (var staticInventory in GameManager._Instance._AllStaticInventories)
        {
            if (staticInventory != null && staticInventory.name.StartsWith("Pocket") && staticInventory.GetComponent<Inventory>().CanTakeThisItem(item) && (staticInventory.transform.position - spawnPos).magnitude < 0.5f)
            {
                return staticInventory;
            }
        }
        return null;
    }


    public void DespawnObject(GameObject obj)
    {
        if (obj != null && obj.GetComponent<NetworkObject>() != null)
            obj.GetComponent<NetworkObject>().Despawn(true);
        else
            Debug.LogError("Despawn gone wrong..");
    }

    public Item GetNewItemByIndex(int index, int count)
    {
        Item item = GetItemByIndex(index).Copy();
        item._Count = count;
        return item;
    }
    private Item GetItemByIndex(int index)
    {
        return GameManager._Instance._AllItems[index];
    }

    public int GetIndexByItem(Item item)
    {
        if (item._Name == "")
            Debug.LogError("Item Name is Empty!");
        if (item._Name == null)
            Debug.LogError("Item Name is Null!");
        for (int i = 0; i < GameManager._Instance._AllItems.Count; i++)
        {
            if (GameManager._Instance._AllItems[i]._Name == item._Name)
                return i;
        }
        Debug.LogError("Index not found!");
        return -1;
    }

    #endregion

    public GameObject GetObjectFromNetworkID(ulong id)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(id)) return null;
        return NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].gameObject;
    }

}
