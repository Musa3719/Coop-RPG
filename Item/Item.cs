using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

[System.Serializable]
public abstract class Item
{
    public string _Name;
    public Image _InventoryIcon;
    public int _Count;
    public Item(string name, Image icon, int count = 1)
    {
        _Name = name;
        _InventoryIcon = icon;
        _Count = count;
    }
    public abstract void Interact();

    /*private void OnCollisionEnter(Collision collision)
    {
        FindObjectOfType<Humanoid>()._Inventory.PickUpItem(gameObject);
    }*/

    /* [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void SetCountForItemClientRpc(int dropCount)
    {
        _Count = dropCount;
    }*/
}

