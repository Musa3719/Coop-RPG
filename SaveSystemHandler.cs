using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Netcode;
using System.Collections;
using UnityEngine.SceneManagement;

public static class SaveSystemHandler
{
    private static string SavePath(int index) => Path.Combine(Application.persistentDataPath, "Save" + index.ToString() + ".json");

    public static void SaveOnePlayerDataWhenDisconnect(ulong playerID)
    {
        SaveOnePlayer(LoadGameData(GameManager._Instance._LastLoadedGameIndex), GameManager._Instance._LastLoadedGameIndex, playerID, GameManager._Instance._Players[playerID], true);
    }
    public static void SaveGame(int index)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        SavedDataBlock data = new SavedDataBlock
        {
            _GameData = new GameData(),
            _PlayerData = new List<PlayerData>()
        };

        SavedDataBlock oldData = LoadGameData(index);
        if (oldData != null)
            data._PlayerData = oldData._PlayerData;

        //add game data here
        data._GameData._ChestsWillBeSpawned = new ListOfInventoryWrapper();
        data._GameData._PocketsWillBeSpawned = new ListOfInventoryWrapper();
        data._GameData._PositionOfChestsWillBeSpawned = new List<Vector3>();
        data._GameData._PositionOfPocketsWillBeSpawned = new List<Vector3>();
        data._GameData._RotationOfChestsWillBeSpawned = new List<Vector3>();
        data._GameData._RotationOfPocketsWillBeSpawned = new List<Vector3>();
        var holders = MonoBehaviour.FindObjectsByType<Inventory>(FindObjectsSortMode.None);
        foreach (var itemHolder in holders)
        {
            if (itemHolder.GetComponent<Humanoid>() == null)
            {
                //add all static inventories
                if (itemHolder.name.StartsWith("Chest"))
                {
                    data._GameData._ChestsWillBeSpawned._AllInventories.Add(new InventoryWrapper(itemHolder._Items, null));
                    data._GameData._PositionOfChestsWillBeSpawned.Add(itemHolder.transform.position);
                    data._GameData._RotationOfChestsWillBeSpawned.Add(itemHolder.transform.localEulerAngles);
                }
                if (itemHolder.name.StartsWith("Pocket"))
                {
                    data._GameData._PocketsWillBeSpawned._AllInventories.Add(new InventoryWrapper(itemHolder._Items, null));
                    data._GameData._PositionOfPocketsWillBeSpawned.Add(itemHolder.transform.position);
                    data._GameData._RotationOfPocketsWillBeSpawned.Add(itemHolder.transform.localEulerAngles);
                }
            }
        }

        //add every player data here
        foreach (var player in GameManager._Instance._Players)
        {
            SaveOnePlayer(data, GameManager._Instance._LastLoadedGameIndex, player.Key, player.Value);
        }

        SaveGameData(index, data);
    }
    public static void SaveOnePlayer(SavedDataBlock data, int saveIndex, ulong id, GameObject player, bool isWritingToSaveFile = false)
    {
        PlayerData playerData = data._PlayerData.GetPlayerData(id);

        if (playerData == null)
        {
            playerData = new PlayerData(id);
            data._PlayerData.Add(playerData);
        }

        //add player data here

        playerData._Position = player.transform.position;
        playerData._EulerAngleY = player.transform.localEulerAngles.y;
        playerData._Inventory = new InventoryWrapper(player.GetComponent<Humanoid>()._Inventory._Items, player.GetComponent<Humanoid>()._Inventory._Equipments);

        if (isWritingToSaveFile)
            SaveGameData(saveIndex, data);
    }

    public static void LoadGame(int index)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        GameManager._Instance._LastLoadedGameIndex = index;
        GameManager._Instance.CoroutineCall(ref NetworkMethods._Instance._LoadGameCoroutine, NetworkMethods._Instance.LoadGameCoroutine(index), NetworkMethods._Instance);
    }


    private static void SaveGameData(int index, SavedDataBlock savedDataBlock)
    {
        string json = JsonUtility.ToJson(savedDataBlock, true);
        File.WriteAllText(SavePath(index), json);
        Debug.Log("Game saved to " + SavePath(index));
    }
    public static SavedDataBlock LoadGameData(int index)
    {
        if (!File.Exists(SavePath(index)))
        {
            Debug.LogWarning("Save file not found.");
            return null;
        }

        string json = File.ReadAllText(SavePath(index));
        return JsonUtility.FromJson<SavedDataBlock>(json);
    }
}

[System.Serializable]
public class SavedDataBlock
{
    public GameData _GameData;
    public List<PlayerData> _PlayerData;
}
[System.Serializable]
public class GameData
{
    // game and level state
    public ListOfInventoryWrapper _ChestsWillBeSpawned;
    public List<Vector3> _PositionOfChestsWillBeSpawned;
    public List<Vector3> _RotationOfChestsWillBeSpawned;

    public ListOfInventoryWrapper _PocketsWillBeSpawned;
    public List<Vector3> _PositionOfPocketsWillBeSpawned;
    public List<Vector3> _RotationOfPocketsWillBeSpawned;
}
[System.Serializable]
public class PlayerData
{
    public PlayerData(ulong id)
    {
        _NetworkID = id;
    }
    //inventory, hollow level, equipment, uma data
    public ulong _NetworkID;
    public Vector3 _Position;
    public float _EulerAngleY;
    public InventoryWrapper _Inventory;
}


//Wrappers
[System.Serializable]
public class InventoryWrapper
{
    public Item[] _Items;
    public Item[] _Equipments;

    public InventoryWrapper(Item[] items, Item[] equipments)
    {
        _Items = items;
        _Equipments = equipments;
    }
}

[System.Serializable]
public class ListOfInventoryWrapper
{
    public List<InventoryWrapper> _AllInventories;

    public ListOfInventoryWrapper()
    {
        _AllInventories = new List<InventoryWrapper>();
    }
}