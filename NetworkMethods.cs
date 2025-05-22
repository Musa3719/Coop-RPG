using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using Unity.Netcode.Components;

public class NetworkMethods : NetworkBehaviour
{
    public static NetworkMethods _Instance;

    public Coroutine _LoadGameCoroutine;

    private GameObject _ownPlayerObject { get; set; }



    private void Awake()
    {
        if (FindObjectsByType<NetworkMethods>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        _Instance = this;
        DontDestroyOnLoad(gameObject);
    }



    public GameObject GetOwnPlayerObject()
    {
        if (_ownPlayerObject != null) return _ownPlayerObject;

        foreach (var player in GameManager._Instance._Players)
        {
            if (player.Value.GetComponent<NetworkObject>().IsOwner)
            {
                _ownPlayerObject = player.Value;
                return _ownPlayerObject;
            }
        }
        Debug.LogError("Player Object Cannot be found!");
        return null;
    }

    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    public void SetPlayersFromConnectionRpc()
    {
        GameManager._Instance.SetPlayersFromConnection();
    }

    #region LoadGameMethods
    public bool IsAllPlayersReady()
    {
        foreach (var player in GameManager._Instance._Players)
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

        foreach (var player in GameManager._Instance._Players)
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
        foreach (var player in GameManager._Instance._Players)
        {
            player.Value.GetComponent<Rigidbody>().isKinematic = false;
            player.Value.GetComponent<NetworkTransform>().enabled = true;
            player.Value.GetComponent<NetworkRigidbody>().enabled = true;

            if (player.Value.GetComponent<NetworkObject>().IsOwner)
            {
                player.Value.GetComponent<NetworkTransform>().Teleport(player.Value.transform.position, player.Value.transform.rotation, Vector3.one);//for update positions from first frame
            }
        }

        GameManager._Instance._LoadingScreen.SetActive(false);
        GameManager._Instance._IsGameLoading = false;
        GameManager._Instance.UnstopGame();
    }


    public IEnumerator LoadGameCoroutine(int index, bool isForLatejoin = false, ulong requesterID = 1000)
    {
        LoadGameStartedRpc();

        SavedDataBlock data = SaveSystemHandler.LoadGameData(index);

        if (!isForLatejoin)
        {
            foreach (var player in GameManager._Instance._Players)
            {
                player.Value.GetComponent<PlayerNetworking>()._IsLoadingScene.Value = true;
            }

            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);

            if (data == null) yield break;

            Debug.Log("Load Game Started!");
            while (!IsAllPlayersReady())
            {
                Debug.Log("Waiting one frame...");
                yield return null;
            }
            Debug.Log("Game Loaded!");

            foreach (var chest in data._GameData._ChestsWillBeSpawned._AllInventories)
            {
                int i = data._GameData._ChestsWillBeSpawned._AllInventories.IndexOf(chest);
                GameObject newObj = Instantiate(GameManager._Instance._AllNetworkPrefabs[1], data._GameData._PositionOfChestsWillBeSpawned[i], Quaternion.identity);
                newObj.transform.localEulerAngles = data._GameData._RotationOfChestsWillBeSpawned[i];
                newObj.GetComponent<Inventory>()._Items = chest._Items;
                newObj.GetComponent<NetworkObject>().Spawn();
            }
            foreach (var pocket in data._GameData._PocketsWillBeSpawned._AllInventories)
            {
                int i = data._GameData._PocketsWillBeSpawned._AllInventories.IndexOf(pocket);
                GameObject newObj = Instantiate(GameManager._Instance._AllNetworkPrefabs[0], data._GameData._PositionOfPocketsWillBeSpawned[i], Quaternion.identity);
                newObj.transform.localEulerAngles = data._GameData._RotationOfPocketsWillBeSpawned[i];
                newObj.GetComponent<Inventory>()._Items = pocket._Items;
                newObj.GetComponent<NetworkObject>().Spawn();
            }
        }

        //load game data (&latejoin)

        SetPlayersFromConnectionRpc();

        //load players data (&latejoin)

        foreach (var playerData in data._PlayerData)
        {
            if (!GameManager._Instance._Players.ContainsKey(playerData._NetworkID) || (isForLatejoin && playerData._NetworkID != requesterID))
            {
                continue;
            }

            LoadOnePlayerData(playerData, isForLatejoin, requesterID);
        }

        GameManager._Instance.CallForAction(LoadGameEndedRpc, 0.2f, true);
    }

    private void LoadOnePlayerData(PlayerData data, bool isForLatejoin = false, ulong requesterID = 1000)
    {
        LoadOnePlayerTransformRpc(data._NetworkID, data._Position, data._EulerAngleY, isForLatejoin, requesterID);

        GameObject playerObj = GameManager._Instance._Players[data._NetworkID];
        playerObj.GetComponent<Inventory>()._Items = data._Inventory._Items;
        playerObj.GetComponent<Inventory>()._Items.SetNullFromSave();
        playerObj.GetComponent<Inventory>()._Equipments = data._Inventory._Equipments;
        playerObj.GetComponent<Inventory>()._Equipments.SetNullFromSave();
        playerObj.GetComponent<Inventory>().SyncInventory(requesterID, isForLatejoin, true);
    }
    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    public void LoadOnePlayerTransformRpc(ulong playerID, Vector3 pos, float yAngle, bool isSendingToClientID = false, ulong clientID = 1)
    {
        if (isSendingToClientID && NetworkManager.LocalClientId != clientID) return;

        GameObject playerObj = GameManager._Instance._Players[playerID];

        if (playerObj == null) return;

        playerObj.transform.position = pos;
        playerObj.transform.localEulerAngles = new Vector3(playerObj.transform.localEulerAngles.x, yAngle, playerObj.transform.localEulerAngles.z);
    }

    #endregion

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void RequestGameStateRpc(ulong requesterID)
    {
        StartCoroutine(LoadGameCoroutine(GameManager._Instance._LastLoadedGameIndex, true, requesterID));
    }


    #region ItemMethods

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void SpawnPocketWithItemRpc(int itemIndex, Vector3 pos, int dropCount)
    {
        Item item = GetNewItemByIndex(itemIndex, dropCount);
        SpawnPocketWithItem(item, pos, dropCount);
    }
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void SpawnPocketWithItemRpc(Item item, Vector3 pos, int dropCount)
    {
        SpawnPocketWithItem(item, pos, dropCount);
    }
    private void SpawnPocketWithItem(Item item, Vector3 pos, int dropCount)
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
