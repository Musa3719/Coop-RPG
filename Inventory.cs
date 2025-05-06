using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Inventory : NetworkBehaviour
{

    public List<Item> _Items { get; set; }

    private float _destroyCounter;

    private void Awake()
    {
        _Items = new List<Item>();
    }
    private void Update()
    {
        if (!IsServer) return;

        if (name.StartsWith("Pocket") && _Items.Count == 0)
        {
            _destroyCounter += Time.deltaTime;
            if (_destroyCounter > 10f)
                NetworkMethods._Instance.DespawnItemServerRpc(GetComponent<NetworkObject>().NetworkObjectId);
        }
    }
    public void UpdateUI()
    {
        //
    }

    private void GainItem(Item item, int gainCount = 1)
    {
        var foundItem = _Items.FindByName(item._Name);
        if (foundItem == null)
            _Items.Add(item, this);
        else
            foundItem._Count += gainCount;

        //pickup sound
    }


    private void LoseItem(Item item, int dropCount)
    {
        var foundItem = _Items.FindByName(item._Name);
        if (foundItem != null && foundItem._Count >= dropCount)
        {
            foundItem._Count -= dropCount;
            if (foundItem._Count <= 0) _Items.Remove(item, this);
        }

        //drop sound
        //drop movement
    }

    /// <summary>
    /// when players dropping items
    /// </summary>
    public void ItemToGround(Item item, int count = 1)
    {
        if (_Items.FindByName(item._Name) == null || _Items.FindByName(item._Name)._Count < count) return;
        LoseItem(item, count);
        NetworkMethods._Instance.SpawnItemServerRpc(NetworkMethods._Instance.GetIndexByItem(item), transform.position, count);
    }

    /// <summary>
    /// When one inventory acquires an item from another
    /// </summary>
    /// <param name="fromInventory">
    /// The inventory this item was taken from
    /// </param>
    public void TakeItem(Item item, Inventory fromInventory, int count = 1)
    {
        if (fromInventory._Items.FindByName(item._Name) == null || fromInventory._Items.FindByName(item._Name)._Count < count) return;
        GainItem(item);
        fromInventory.LoseItem(item, count);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void InitOneItemFromSaveClientRpc(int index, int count)
    {
        Item tempItem = GameManager._Instance._AllItems[index];
        Item item = null;
        switch (tempItem)
        {
            case EquipmentItem e:
                item = new EquipmentItem(tempItem._Name, tempItem._InventoryIcon, count);
                break;
            case FoodItem e:
                item = new FoodItem((tempItem as FoodItem)._HungerChange, tempItem._Name, tempItem._InventoryIcon, count);
                (item as FoodItem)._HungerChange = (tempItem as FoodItem)._HungerChange;
                break;
            case GearItem e:
                item = new GearItem(tempItem._Name, tempItem._InventoryIcon, count);
                break;
            case MedicineItem e:
                item = new MedicineItem((tempItem as MedicineItem)._HealthChange, tempItem._Name, tempItem._InventoryIcon, count);
                (item as MedicineItem)._HealthChange = (tempItem as MedicineItem)._HealthChange;
                break;
            case NonInteractableItem e:
                item = new NonInteractableItem(tempItem._Name, tempItem._InventoryIcon, count);
                break;
            default:
                Debug.LogError("ITEM TYPE NOT EXIST!!!");
                break;
        }
        if (item == null)
        {
            Debug.LogError("ITEM CREATION ERROR");
            return;
        }

        _Items.Add(item);
    }

    public void UseItem(Item item)
    {
        if (_Items.FindByName(item._Name) == null) return;

        item.Interact();
    }
}
