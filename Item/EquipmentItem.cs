using UnityEngine;
using UnityEngine.UI;

public class EquipmentItem : Item
{
    public EquipmentItem(string name, Image icon, int count = 1) : base(name, icon, count)
    {

    }
    public override void Interact()
    {
        //equip or unequip
    }
}
