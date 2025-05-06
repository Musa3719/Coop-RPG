using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Netcode;
using System.Collections;
using UnityEngine.SceneManagement;

public static class SaveSystemHandler
{
    private static string SavePath(int index) => Path.Combine(Application.persistentDataPath, "Save" + index.ToString() + ".json");

    public static void SaveGame(int index)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        SavedDataBlock data = new SavedDataBlock
        {
            _GameData = new GameData(),
            _PlayerData = new List<PlayerData>()
        };

        //add every game data here
        //data.GameData.  
        var holders = MonoBehaviour.FindObjectsByType<Inventory>(FindObjectsSortMode.None);
        data._GameData._NetworkObjectsWillBeSpawned = new List<GameObject>();
        foreach (var itemHolder in holders)
        {
            if (itemHolder.GetComponent<Humanoid>() == null)
            {
                data._GameData._NetworkObjectsWillBeSpawned.Add(itemHolder.gameObject);
            }
        }

        foreach (var player in GameManager._Instance._Players)
        {
            PlayerData playerData = new PlayerData(player.Key);
            data._PlayerData.Add(playerData);
            //add every player data here
            playerData._Inventory = player.Value.GetComponent<Humanoid>()._Inventory._Items;
        }

        SaveGameData(index, data);
    }

    public static void LoadGame(int index)
    {
        if (!NetworkManager.Singleton.IsServer) return;

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
    public List<GameObject> _NetworkObjectsWillBeSpawned;
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
    public List<Item> _Inventory;
}