using TMPro;
using UnityEngine;

public class SlotCountTextUI : MonoBehaviour
{
    public void OnUpdate()
    {
        if (transform.parent.GetComponent<InventoryUISlot>() != null && transform.parent.GetComponent<InventoryUISlot>()._Item != null)
            GetComponent<TextMeshProUGUI>().text = transform.parent.GetComponent<InventoryUISlot>()._Item._Count.ToString();
    }
}
