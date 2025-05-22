using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Inventory : NetworkBehaviour
{
    public Item[] _Items { get; set; }
    public Item[] _Equipments { get; set; }

    public NetworkVariable<ulong> _HeadGearWorldInstanceID;
    public NetworkVariable<ulong> _BodyGearWorldInstanceID;
    public NetworkVariable<ulong> _LegsGearWorldInstanceID;
    public NetworkVariable<ulong> _HandsItemWorldInstanceID;

    public int _ItemLenghtLimit;
    /*
    public Item HeadGear; 0
    public Item BodyGear; 1
    public Item LegsGear; 2
    public Item HandsItem; 3
    public Item SecondaryHandsItem; 4
    public Item Throwable_1; 5 
    public Item Throwable_2; 6
    public Item Throwable_3; 7
    public Item Throwable_4; 8
    public Item RingGear_1; 9 
    public Item RingGear_2; 10
    public Item RingGear_3; 11
    public Item RingGear_4; 12
    */

    private float _destroyCounter;
    private bool _isAboutToBeDestroyed;
    private float _lastTimeSynced;
    private int _lastSyncRPCCount;

    private void Awake()
    {
        _ItemLenghtLimit = GetComponent<Humanoid>() == null ? 42 : 24;
        _Items = new Item[_ItemLenghtLimit];
        _Equipments = new Item[13];
    }
    private void Update()
    {
        if (!IsServer) return;

        if (name.StartsWith("Pocket") && _Items.Count() == 0)
        {
            _destroyCounter += Time.deltaTime;
            if (_destroyCounter > 10f)
            {
                _isAboutToBeDestroyed = true;
            }
            if (_destroyCounter > 12f)
            {
                NetworkMethods._Instance.DespawnObject(gameObject);
            }
        }
        else if (_isAboutToBeDestroyed)
        {
            _isAboutToBeDestroyed = false;
            _destroyCounter = 0f;
        }
        else
        {
            _destroyCounter = 0f;
        }
    }
    public override void OnNetworkSpawn()
    {
        if (GetComponent<Humanoid>() == null)
        {
            if (!GameManager._Instance._AllStaticInventories.Contains(gameObject))
                GameManager._Instance._AllStaticInventories.Add(gameObject);

            RequestStaticInventoryRpc(NetworkManager.LocalClientId);
        }

    }
    public override void OnNetworkDespawn()
    {
        if (GameManager._Instance._AllStaticInventories.Contains(gameObject))
            GameManager._Instance._AllStaticInventories.Remove(gameObject);

    }

    public void OpenOrCloseUseUI(bool isOpen)
    {
        transform.Find("Canvas").gameObject.SetActive(isOpen);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void RequestStaticInventoryRpc(ulong clientID)
    {
        SyncInventory(clientID, true);
    }


    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void CreateWorldInstanceForItemRpc(int worldInstanceIndex, int equipIndex, ulong inventoryIndex)
    {
        GameObject obj = MonoBehaviour.Instantiate(GameManager._Instance._AllNetworkPrefabs[worldInstanceIndex]);
        obj.GetComponent<NetworkObject>().Spawn();

        DestroyWorldInstanceForItemRpc(equipIndex, inventoryIndex);
        switch (equipIndex)
        {
            case 0:
                NetworkMethods._Instance.GetObjectFromNetworkID(inventoryIndex).GetComponent<Inventory>()._HeadGearWorldInstanceID.Value = obj.GetComponent<NetworkObject>().NetworkObjectId;
                break;
            case 1:
                NetworkMethods._Instance.GetObjectFromNetworkID(inventoryIndex).GetComponent<Inventory>()._BodyGearWorldInstanceID.Value = obj.GetComponent<NetworkObject>().NetworkObjectId;
                break;
            case 2:
                NetworkMethods._Instance.GetObjectFromNetworkID(inventoryIndex).GetComponent<Inventory>()._LegsGearWorldInstanceID.Value = obj.GetComponent<NetworkObject>().NetworkObjectId;
                break;
            case 3:
                NetworkMethods._Instance.GetObjectFromNetworkID(inventoryIndex).GetComponent<Inventory>()._HandsItemWorldInstanceID.Value = obj.GetComponent<NetworkObject>().NetworkObjectId;
                break;
            default:
                break;
        }
    }
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void DestroyWorldInstanceForItemRpc(int equippedToIndex, ulong inventoryIndex)
    {
        switch (equippedToIndex)
        {
            case 0:
                if (NetworkMethods._Instance.GetObjectFromNetworkID(NetworkMethods._Instance.GetObjectFromNetworkID(inventoryIndex).GetComponent<Inventory>()._HeadGearWorldInstanceID.Value) != null)
                    NetworkMethods._Instance.GetObjectFromNetworkID(NetworkMethods._Instance.GetObjectFromNetworkID(inventoryIndex).GetComponent<Inventory>()._HeadGearWorldInstanceID.Value).GetComponent<NetworkObject>().Despawn();
                break;
            case 1:
                if (NetworkMethods._Instance.GetObjectFromNetworkID(NetworkMethods._Instance.GetObjectFromNetworkID(inventoryIndex).GetComponent<Inventory>()._BodyGearWorldInstanceID.Value) != null)
                    NetworkMethods._Instance.GetObjectFromNetworkID(NetworkMethods._Instance.GetObjectFromNetworkID(inventoryIndex).GetComponent<Inventory>()._BodyGearWorldInstanceID.Value).GetComponent<NetworkObject>().Despawn();
                break;
            case 2:
                if (NetworkMethods._Instance.GetObjectFromNetworkID(NetworkMethods._Instance.GetObjectFromNetworkID(inventoryIndex).GetComponent<Inventory>()._LegsGearWorldInstanceID.Value) != null)
                    NetworkMethods._Instance.GetObjectFromNetworkID(NetworkMethods._Instance.GetObjectFromNetworkID(inventoryIndex).GetComponent<Inventory>()._LegsGearWorldInstanceID.Value).GetComponent<NetworkObject>().Despawn();
                break;
            case 3:
                if (NetworkMethods._Instance.GetObjectFromNetworkID(NetworkMethods._Instance.GetObjectFromNetworkID(inventoryIndex).GetComponent<Inventory>()._HandsItemWorldInstanceID.Value) != null)
                    NetworkMethods._Instance.GetObjectFromNetworkID(NetworkMethods._Instance.GetObjectFromNetworkID(inventoryIndex).GetComponent<Inventory>()._HandsItemWorldInstanceID.Value).GetComponent<NetworkObject>().Despawn();
                break;
            default:
                break;
        }
    }

    public void SyncInventory(ulong clientID = 1, bool isSendingToClientID = false, bool isFromLoadGameMethod = false)
    {
        float senderTime = Time.realtimeSinceStartup;

        for (int i = 0; i < _Items.Length; i++)
        {
            if (_Items[i] == null)
            {
                SyncInventoryNullRpc(false, i, senderTime, isSendingToClientID, clientID);
                continue;
            }

            if (_Items[i].IsUniqueItemType())
                SyncInventoryRpc(i, _Items[i], _Items[i]._Count, senderTime, -1, isSendingToClientID, clientID);
            else
                SyncInventoryRpc(i, NetworkMethods._Instance.GetIndexByItem(_Items[i]), _Items[i]._IsEquipped, _Items[i]._Count, senderTime, -1, isSendingToClientID, clientID);
        }

        for (int i = 0; i < _Equipments.Length; i++)
        {
            if (_Equipments[i] == null)
            {
                SyncInventoryNullRpc(true, i, senderTime, isSendingToClientID, clientID);
                continue;
            }

            Item item = _Equipments[i];
            if (item.IsUniqueItemType())
                SyncInventoryRpc(i, item, item._Count, senderTime, i, isSendingToClientID, clientID);
            else
                SyncInventoryRpc(i, NetworkMethods._Instance.GetIndexByItem(item), item._IsEquipped, item._Count, senderTime, -1, isSendingToClientID, clientID);

            if (isFromLoadGameMethod && !isSendingToClientID)
                CreateWorldInstanceForItemRpc(item._WorldInstanceIndex, i, GetComponent<NetworkObject>().NetworkObjectId);
        }
    }

    [Rpc(SendTo.NotMe, Delivery = RpcDelivery.Reliable)]
    private void SyncInventoryNullRpc(bool isEquippment, int i, float senderTime, bool isSendingToClientID = false, ulong clientID = 1)
    {
        if (isSendingToClientID && NetworkManager.LocalClientId != clientID) return;

        if (_lastTimeSynced > senderTime) return;
        else if (_lastTimeSynced == senderTime)
            _lastSyncRPCCount++;
        else
            _lastSyncRPCCount = 0;
        _lastTimeSynced = senderTime;

        if (isEquippment)
            _Equipments[i] = null;
        else
            _Items[i] = null;

        if (_lastSyncRPCCount == _ItemLenghtLimit + (GetComponent<Humanoid>() == null ? 0 : _Equipments.Length))
            GameManager._Instance.CheckInventoryUpdate(this);
    }

    [Rpc(SendTo.NotMe, Delivery = RpcDelivery.Reliable)]
    private void SyncInventoryRpc(int i, int itemIndex, bool isEquipped, int count, float senderTime, int equipIndex = -1, bool isSendingToClientID = false, ulong clientID = 1)
    {
        Item item = NetworkMethods._Instance.GetNewItemByIndex(itemIndex, count);
        item._IsEquipped = isEquipped;
        SyncInventoryCommon(i, item, senderTime, equipIndex, isSendingToClientID, clientID);
    }
    [Rpc(SendTo.NotMe, Delivery = RpcDelivery.Reliable)]
    private void SyncInventoryRpc(int i, Item item, int count, float senderTime, int equipIndex = -1, bool isSendingToClientID = false, ulong clientID = 1)
    {
        item._Count = count;
        SyncInventoryCommon(i, item, senderTime, equipIndex, isSendingToClientID, clientID);
    }
    private void SyncInventoryCommon(int i, Item item, float senderTime, int equipIndex, bool isSendingToClientID, ulong clientID)
    {
        if (isSendingToClientID && NetworkManager.LocalClientId != clientID) return;

        if (_lastTimeSynced > senderTime) return;
        else if (_lastTimeSynced == senderTime)
            _lastSyncRPCCount++;
        else
            _lastSyncRPCCount = 0;
        _lastTimeSynced = senderTime;

        if (item._IsEquipped)
            item.EquipForSync(this, equipIndex);
        else
            _Items[i] = item;

        if (_lastSyncRPCCount == _ItemLenghtLimit + (GetComponent<Humanoid>() == null ? 0 : _Equipments.Length))
            GameManager._Instance.CheckInventoryUpdate(this);
    }


    public void GainItem(Item item, int gainCount, int index, bool isSync = true)
    {
        if (!CanTakeThisItem(item)) return;

        item._Count = gainCount;
        if (index == -1)
        {
            var foundItem = _Items.FindByName(item._Name);
            if (foundItem == null || item.IsUniqueItemType())
                _Items.Add(item, this, index);
            else
                foundItem._Count += gainCount;
        }
        else
        {
            if (_Items[index] == null)
                _Items.Add(item, this, index);
            else if (!_Items[index].IsUniqueItemType() && _Items[index]._Name == item._Name)
                _Items[index]._Count += gainCount;
            else
                Debug.LogError("Index is not empty. It cannot stack with already existing item..");
        }

        if (isSync)
            SyncInventory();
        //pickup sound
        GameManager._Instance.CheckInventoryUpdate(this);
    }

    public void LoseItem(Item item, int dropCount, int index, bool isSync = true)
    {
        Item foundItem = null;
        if (index == -1)
            foundItem = _Items.FindByName(item._Name);
        else
            foundItem = _Items[index];

        if (item.IsUniqueItemType())
        {
            _Items.Remove(item, this, index);
        }
        else if (foundItem != null && foundItem._Count >= dropCount)
        {
            foundItem._Count -= dropCount;
            if (foundItem._Count <= 0) _Items.Remove(foundItem, this, index);
        }

        if (isSync)
            SyncInventory();
        //drop sound
        //drop movement
        GameManager._Instance.CheckInventoryUpdate(this);
    }

    public bool IsEquipped(Item item)
    {
        foreach (var equipment in _Equipments)
        {
            if (equipment != null && equipment.IsSame(item))
                return true;
        }
        return false;
    }
    public bool IsFull()
    {
        return _Items.Count() >= _ItemLenghtLimit;
    }

    public void ItemToGround(Item item, int count, int fromIndex, bool isSync)
    {
        if (item.IsUniqueItemType() && item._Count >= count)
        {
            LoseItem(item, count, fromIndex, isSync);
            NetworkMethods._Instance.SpawnPocketWithItemRpc(item, transform.position, count);
        }
        else if (_Items.FindByName(item._Name) != null && _Items.FindByName(item._Name)._Count >= count)
        {
            LoseItem(item, count, fromIndex, isSync);
            NetworkMethods._Instance.SpawnPocketWithItemRpc(NetworkMethods._Instance.GetIndexByItem(item), transform.position, count);
        }
    }
    public void FromEquipmentToGround(Item item, int count, int fromIndex, bool isSync)
    {
        if (!item._IsEquipped)
        {
            Debug.LogError("Item Not Equipped!!");
            return;
        }
        item.UnEquip(this, fromIndex, isSync, false);

        if (item.IsUniqueItemType())
        {
            NetworkMethods._Instance.SpawnPocketWithItemRpc(item, transform.position, count);
        }
        else if (_Equipments.FindByName(item._Name) != null && _Equipments.FindByName(item._Name)._Count >= count)
        {
            NetworkMethods._Instance.SpawnPocketWithItemRpc(NetworkMethods._Instance.GetIndexByItem(item), transform.position, count);
        }
    }


    public void TakeItemRequestSend(Item item, Inventory anotherInventory, int count)
    {
        if (!anotherInventory._Items.Contains(item))
        {
            Debug.LogError("request already sent or item does not exist in another inventory");
            return;
        }

        TakeItemRequestRpc(item._Name, anotherInventory._Items.IndexOf(item), anotherInventory.NetworkObjectId, count);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void TakeItemRequestRpc(string itemName, int itemIndex, ulong anotherInventoryNetworkID, int count)
    {
        Inventory anotherInventory = NetworkMethods._Instance.GetObjectFromNetworkID(anotherInventoryNetworkID).GetComponent<Inventory>();
        Item item = anotherInventory._Items[itemIndex];

        if (item != null && item._Name == itemName)
            TakeItemFromAnotherInventory(item, anotherInventory, count, itemIndex);
    }

    private void TakeItemFromAnotherInventory(Item item, Inventory anotherInventory, int count, int fromIndex)
    {
        if (_isAboutToBeDestroyed || anotherInventory == null) return;
        if (!item.IsUniqueItemType() && (anotherInventory._Items.FindByName(item._Name) == null || anotherInventory._Items.FindByName(item._Name)._Count < count)) return;
        if (item.IsUniqueItemType() && !anotherInventory._Items.Contains(item)) return;


        if (GetComponent<Humanoid>() != null && item.IsEquippableItemType() && GetAvailableEquipmentIndex(item) != -1)
            item.Equip(GetComponent<Humanoid>(), anotherInventory, fromIndex);
        else if (CanTakeThisItem(item))
        {
            anotherInventory.LoseItem(item, count, fromIndex);
            GainItem(item, count, -1);
        }
    }
    public void TakeItemFromNothing(Item item, int count = 1, bool isSync = true)
    {
        if (_isAboutToBeDestroyed) return;

        GainItem(item, count, -1, isSync);
    }
    public bool CanTakeThisItem(Item item)
    {
        if (!IsFull()) return true;
        if (!item.IsUniqueItemType() && _Items.FindByName(item._Name) != null) return true;
        return false;
    }
    public void UseItem(Humanoid userHuman, Item item)
    {
        if (_Items.FindByName(item._Name) == null && !_Equipments.Contains(item)) return;

        item.Interact(userHuman, this);
    }

    public int GetAvailableEquipmentIndex(Item item)
    {
        switch (item._ItemType)
        {
            case ItemType.HandItem:
                if (_Equipments[3] == null)
                    return 3;
                else if (_Equipments[4] == null)
                    return 4;
                return -1;
            case ItemType.HeadGearItem:
                if (_Equipments[0] == null)
                    return 0;
                return -1;
            case ItemType.BodyGearItem:
                if (_Equipments[1] == null)
                    return 1;
                return -1;
            case ItemType.LegsGearItem:
                if (_Equipments[2] == null)
                    return 2;
                return -1;
            case ItemType.RingGearItem:
                if (_Equipments[9] == null)
                    return 9;
                else if (_Equipments[10] == null)
                    return 10;
                else if (_Equipments[11] == null)
                    return 11;
                else if (_Equipments[12] == null)
                    return 12;
                return -1;
            case ItemType.ThrowableItem:
                if (_Equipments[5] == null)
                    return 5;
                else if (_Equipments[6] == null)
                    return 6;
                else if (_Equipments[7] == null)
                    return 7;
                else if (_Equipments[8] == null)
                    return 8;
                return -1;
            default:
                Debug.LogError("type not found");
                return -1;
        }
    }
}
