using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethods
{
    public static Item FindByName(this List<Item> ýtemList, string name)
    {
        foreach (var item in ýtemList)
        {
            if (item._Name == name)
                return item;
        }
        return null;
    }
    public static void Add(this List<Item> itemList, Item item, Inventory inventory)
    {
        itemList.Add(item);
        inventory.UpdateUI();
    }
    public static void Remove(this List<Item> itemList, Item item, Inventory inventory)
    {
        itemList.Remove(item);
        inventory.UpdateUI();
    }
}
