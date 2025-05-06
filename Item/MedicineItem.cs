using UnityEngine;
using UnityEngine.UI;

public class MedicineItem : Item
{
    public float _HealthChange;
    public MedicineItem(float healthChange, string name, Image icon, int count = 1) : base(name, icon, count)
    {
        _HealthChange = healthChange;
    }
    public override void Interact()
    {
        //increase health by HealthChange

    }
}
