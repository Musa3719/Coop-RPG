using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUISlot : MonoBehaviour
{
    public Inventory _Inventory { get; set; }
    public bool _IsEquipmentSlot { get; set; }
    public int _Index { get; set; }
    public Item _Item
    {
        get
        {
            if (_Inventory == null) return null;
            if (_IsEquipmentSlot) return _Inventory._Equipments[_Index];
            else return _Inventory._Items[_Index];
        }
    }
    public void OnUpdate()
    {
        if (GetComponentInChildren<SlotArmorUI>() != null)
            GetComponentInChildren<SlotArmorUI>().OnUpdate();
        if (GetComponentInChildren<SlotWeaponUI>() != null)
            GetComponentInChildren<SlotWeaponUI>().OnUpdate();
        if (GetComponentInChildren<SlotCountTextUI>() != null)
            GetComponentInChildren<SlotCountTextUI>().OnUpdate();
    }

}
