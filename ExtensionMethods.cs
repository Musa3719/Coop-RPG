using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class ExtensionMethods
{

    #region ItemMethods

    public static Item Copy(this Item item)
    {
        Item newItem = new Item();

        newItem._Weight = item._Weight;
        newItem._Name = item._Name;
        newItem._Count = item._Count;
        newItem._AttackSpeedIncreaseValue = item._AttackSpeedIncreaseValue;
        newItem._DamageIncreaseValue = item._DamageIncreaseValue;
        newItem._HealthChange = item._HealthChange;
        newItem._HealthIncreaseValue = item._HealthIncreaseValue;
        newItem._HungerChange = item._HungerChange;
        newItem._ItemType = item._ItemType;
        newItem._ProtectionValue = (int)Time.time;
        newItem._SpeedIncreaseValue = item._SpeedIncreaseValue;
        newItem._StaminaIncreaseValue = item._StaminaIncreaseValue;
        newItem._IsEquipped = item._IsEquipped;
        newItem._WorldInstanceIndex = item._WorldInstanceIndex;
        newItem._Level = item._Level;
        newItem._Durability = item._Durability;

        return newItem;
    }
    public static void Add(this Item[] items, Item item, Inventory inventory, int index)
    {
        if (items == null) return;
        
        if (index == -1)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    items[i] = item;
                    break;
                }
            }
        }
        else
            items[index] = item;

    }
    public static void Remove(this Item[] items, Item item, Inventory inventory, int index)
    {
        if (items == null) return;

        if (index == -1)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == item)
                {
                    items[i] = null;
                    break;
                }
            }
        }
        else
            items[index] = null;
    }


    public static bool IsSame(this Item item, Item otherItem)
    {
        return item == otherItem;
    }
    public static Item FindByName(this Item[] itemArray, string name)
    {
        if (itemArray == null) return null;

        foreach (var item in itemArray)
        {
            if (item != null && item._Name == name)
                return item;
        }
        return null;
    }
    public static int IndexOf(this Inventory inventory, Item lookingItem)
    {
        if (inventory == null) return -1;

        if (lookingItem._IsEquipped)
            return inventory._Equipments.IndexOf(lookingItem);
        else
            return inventory._Items.IndexOf(lookingItem);
    }
    public static int IndexOf(this Item[] itemArray, Item lookingItem)
    {
        if (itemArray == null) return -1;

        for (int i = 0; i < itemArray.Length; i++)
        {
            if (itemArray[i] != null && itemArray[i].IsSame(lookingItem))
                return i;
        }
        return -1;
    }
    public static int GetFirstEmptyIndex(this Item[] itemArray)
    {
        if (itemArray == null) return -1;

        for (int i = 0; i < itemArray.Length; i++)
        {
            if (itemArray[i] == null)
                return i;
        }
        return -1;
    }
    public static bool Contains(this Item[] itemArray, Item lookingItem)
    {
        if (itemArray == null) return false;

        for (int i = 0; i < itemArray.Length; i++)
        {
            if (itemArray != null && itemArray[i].IsSame(lookingItem))
                return true;
        }
        return false;
    }
    public static int Count(this Item[] itemArray)
    {
        if (itemArray == null) return 0;

        int sum = 0;
        for (int i = 0; i < itemArray.Length; i++)
        {
            if (itemArray[i] != null)
                sum++;
        }
        return sum;
    }
    public static void SetNullFromSave(this Item[] items)
    {
        if (items == null) return;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i]._Name == "") items[i] = null;
        }
    }
    public static void Clear(this Item[] itemArray)
    {
        if (itemArray == null) return;

        for (int i = 0; i < itemArray.Length; i++)
        {
            itemArray[i] = null;
        }
    }
    public static PlayerData GetPlayerData(this List<PlayerData> data, ulong id)
    {
        foreach (var item in data)
        {
            if (item._NetworkID == id) return item;
        }
        return null;
    }

    #endregion
}
