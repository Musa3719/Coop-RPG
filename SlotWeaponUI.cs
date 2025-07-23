using TMPro;
using UnityEngine;

public class SlotWeaponUI : MonoBehaviour
{
    public void OnUpdate()
    {
        if (transform.parent.GetComponent<InventoryUISlot>() != null && transform.parent.GetComponent<InventoryUISlot>()._Item != null)
        {
            transform.Find("LevelText").GetComponent<TextMeshProUGUI>().text = "+" + transform.parent.GetComponent<InventoryUISlot>()._Item._Level.ToString();
            transform.Find("DurabilityText").GetComponent<TextMeshProUGUI>().text = "+" + (transform.parent.GetComponent<InventoryUISlot>()._Item._Durability / transform.parent.GetComponent<InventoryUISlot>()._Item._Durability).ToString();
            if ((transform.parent.GetComponent<InventoryUISlot>()._Item._Durability / transform.parent.GetComponent<InventoryUISlot>()._Item._Durability) <= 5f)
                transform.Find("BrokenImage").gameObject.SetActive(true);
            else
                transform.Find("BrokenImage").gameObject.SetActive(false);
        }
    }
}
