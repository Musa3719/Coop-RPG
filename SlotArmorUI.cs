using UnityEngine;
using TMPro;

public class SlotArmorUI : MonoBehaviour
{
    public void OnUpdate()
    {
        if (transform.parent.GetComponent<InventoryUISlot>() != null && transform.parent.GetComponent<InventoryUISlot>()._Item != null)
        {
            GetComponent<TextMeshProUGUI>().text = transform.parent.GetComponent<InventoryUISlot>()._Item._ProtectionValue.ToString();
        }
    }
}
