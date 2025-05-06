using UnityEngine;
using UnityEngine.UI;

public class FoodItem : Item
{
    public float _HungerChange;
    public FoodItem(float hungerChange ,string name, Image icon, int count = 1) : base(name, icon, count)
    {
        _HungerChange = hungerChange;
    }
    public override void Interact()
    {
        //lower hanger by HungerChange
    }
}