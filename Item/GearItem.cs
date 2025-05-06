using UnityEngine;
using UnityEngine.UI;

public class GearItem : Item
{
    public GearItem(string name, Image icon, int count = 1) : base(name, icon, count)
    {

    }
    public override void Interact()
    {
        //wear or undress
    }
}