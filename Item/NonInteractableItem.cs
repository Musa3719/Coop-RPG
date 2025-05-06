using UnityEngine;
using UnityEngine.UI;

public class NonInteractableItem : Item
{
    public NonInteractableItem(string name, Image icon, int count = 1) : base(name, icon, count)
    {

    }
    public override void Interact()
    {
        return;
    }
}
