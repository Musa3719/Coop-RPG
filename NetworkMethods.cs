using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

public class NetworkMethods : NetworkBehaviour
{
    public static NetworkMethods _Instance;

    public Coroutine _LoadGameCoroutine;


    private void Awake()
    {
        if (FindObjectsByType<NetworkMethods>(FindObjectsSortMode.None).Length > 1)
            Destroy(gameObject);

        _Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsAllPlayersReady()
    {
        foreach (var player in GameManager._Instance._Players.Values)
        {
            if (player.GetComponent<InputsAndPlayerNetworking>()._IsLoadingScene.Value)
                return false;
        }
        return true;
    }
   
    public GameObject GetOwnPlayerObject()
    {
        foreach (var player in GameManager._Instance._Players)
        {
            if (player.Value.GetComponent<NetworkObject>().IsOwner)
                return player.Value;
        }
        Debug.LogError("Player Object Cannot be found!");
        return null;
    }

    public IEnumerator LoadGameCoroutine(int index)
    {
        foreach (var player in GameManager._Instance._Players)
        {
            player.Value.GetComponent<InputsAndPlayerNetworking>()._IsLoadingScene.Value = true;
        }

        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);

        SavedDataBlock data = SaveSystemHandler.LoadGameData(index);
        if (data == null) yield break;

        Debug.Log("Load Game Started!");
        while (!IsAllPlayersReady())
        {
            Debug.Log("inside");
            yield return null;
        }
        Debug.Log("Game Loaded!");

        //load game data here

        foreach (var networkObject in data._GameData._NetworkObjectsWillBeSpawned)
        {
            networkObject.GetComponent<NetworkObject>().Spawn();

            if (networkObject.GetComponent<Inventory>() != null)
            {
                InitItemHoldersFromSave(networkObject.GetComponent<Inventory>());
            }
        }


        //load players data

        foreach (var playerData in data._PlayerData)
        {
            if (!GameManager._Instance._Players.ContainsKey(playerData._NetworkID))
            {
                Debug.LogError("player not found when loading!!!!");
                continue;
            }
            GameObject playerObj = GameManager._Instance._Players[playerData._NetworkID];
            //load every playerdata here
            playerObj.GetComponent<Humanoid>()._Inventory._Items = playerData._Inventory;
        }
        Debug.Log("asdas");
    }


    #region ItemMethods

    public void InitItemHoldersFromSave(Inventory itemHolder)
    {
        itemHolder.GetComponent<NetworkObject>().Spawn(true);
        foreach (var item in itemHolder._Items)
        {
            foreach (var itemTemplate in GameManager._Instance._AllItems)
            {
                if (itemTemplate._Name == item._Name)
                {
                    int index = GameManager._Instance._AllItems.IndexOf(itemTemplate);
                    itemHolder.InitOneItemFromSaveClientRpc(index, item._Count);
                    continue;
                }
            }

        }
    }

    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void SpawnItemServerRpc(int itemIndex, Vector3 pos, int dropCount)
    {
        GameObject itemObj = Instantiate(GameManager._Instance._AllNetworkPrefabs[0], pos, Quaternion.identity);
        itemObj.GetComponent<NetworkObject>().Spawn(true);
        itemObj.GetComponent<Inventory>().InitOneItemFromSaveClientRpc(itemIndex, dropCount);
    }


    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void DespawnItemServerRpc(ulong networkObjectID)
    {
        GameObject item = GetObjectFromNetworkID(networkObjectID);
        if (item != null)
            item.GetComponent<NetworkObject>().Despawn(true);
    }

    public Item GetItemByIndex(int index)
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
        return -1;
    }
    #endregion

    public GameObject GetObjectFromNetworkID(ulong id)
    {
        return NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].gameObject;
    }
    public ulong GetClientIDFromObject(GameObject obj)
    {
        if (obj.GetComponent<NetworkObject>() == null) return 0;
        return obj.GetComponent<NetworkObject>().OwnerClientId;
    }
    public ulong GetNetworkIDFromObject(GameObject obj)
    {
        if (obj.GetComponent<NetworkObject>() == null) return 0;
        return obj.GetComponent<NetworkObject>().NetworkObjectId;
    }
}
