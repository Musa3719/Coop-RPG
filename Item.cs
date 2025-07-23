using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Collections;

public enum ItemType
{
    FoodItem,
    PotionItem,
    HandItem,
    HeadGearItem,
    BodyGearItem,
    LegsGearItem,
    RingGearItem,
    NonInteractableItem,
    ThrowableItem
}

[System.Serializable]
public class Item : INetworkSerializable
{
    //Dynamics

    public int _Count;
    public bool _IsEquipped;

    public int _Level;
    public float _Durability;
    public float _MaxDurability;

    //Predefineds

    public float _Weight;
    public string _Name;
    public ItemType _ItemType;
    public int _WorldInstanceIndex;

    public int _HungerChange;
    public int _HealthChange;
    public int _ProtectionValue;

    public int _SpeedIncreaseValue;
    public int _AttackSpeedIncreaseValue;
    public int _DamageIncreaseValue;
    public int _HealthIncreaseValue;
    public int _StaminaIncreaseValue;

    //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    //CHANGE COPY EXTENSION METHOD Too!
    //CHANGE NETWORK SERIALIZE METHOD Also!
    //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

    public Item()
    {

    }


    public void Interact(Humanoid userHuman, Inventory inventory)
    {
        if (IsEquippableItemType())
        {
            if (userHuman.GetComponent<Inventory>() == inventory && inventory.IsEquipped(this))
            {
                if (!inventory.IsFull())
                    NetworkController._Instance.UnEquipRequestSend(this, inventory, inventory._Equipments.IndexOf(this));
            }
            else if (userHuman.GetComponent<Inventory>() == inventory && !inventory.IsEquipped(this))
            {
                NetworkController._Instance.EquipRequestSend(this, userHuman, inventory);
            }
            else if (userHuman.GetComponent<Inventory>() != inventory)
            {
                NetworkController._Instance.EquipRequestSend(this, userHuman, inventory, inventory._Items.IndexOf(this));
            }
        }
        else
        {
            if (_ItemType == ItemType.FoodItem)
            {
                //use food
            }
            else if (_ItemType == ItemType.PotionItem)
            {
                //use potion
            }
        }
    }
    public int GetEquipIndex(Inventory inventory)
    {
        if (inventory.IsEquipped(this))
        {
            Debug.LogError("Item is equipped?");
            //return -1;
        }

        //is not equipped

        switch (_ItemType)
        {
            case ItemType.HandItem:
                if (inventory._Equipments[3] == null || inventory._Equipments[4] != null)
                    return 3;
                else
                    return 4;
            case ItemType.HeadGearItem:
                return 0;
            case ItemType.BodyGearItem:
                return 1;
            case ItemType.LegsGearItem:
                return 2;
            case ItemType.RingGearItem:
                if (inventory._Equipments[9] == null)
                    return 9;
                else if (inventory._Equipments[10] == null)
                    return 10;
                else if (inventory._Equipments[11] == null)
                    return 11;
                else if (inventory._Equipments[12] == null)
                    return 12;
                else
                    return 12;
            case ItemType.ThrowableItem:
                if (inventory._Equipments[5] == null)
                    return 5;
                else if (inventory._Equipments[6] == null)
                    return 6;
                else if (inventory._Equipments[7] == null)
                    return 7;
                else if (inventory._Equipments[8] == null)
                    return 8;
                else
                    return 8;
            default:
                Debug.LogError("type not found");
                return -1;
        }
    }


    public void EquipForSync(Inventory inventory, int equipIndex)
    {
        if (!IsEquippableItemType() || equipIndex == -1)
        {
            Debug.LogError("Equip For Sync Error!");
            return;
        }

        /*if (inventory._Equipments[equipIndex] != null && inventory._Equipments[equipIndex] != this)
        {
            //MonoBehaviour.Destroy(inventory._Equipments[equipIndex]._WorldInstance);
            Debug.LogError("Equipment Slot contains another equipment!");
        }*/

        inventory._Equipments[equipIndex] = this;
        GameManager._Instance.CheckInventoryUpdate(inventory);
    }

    public bool IsUniqueItemType()
    {
        switch (_ItemType)
        {
            case ItemType.FoodItem:
                return false;
            case ItemType.PotionItem:
                return false;
            case ItemType.HandItem:
                return true;
            case ItemType.HeadGearItem:
                return true;
            case ItemType.BodyGearItem:
                return true;
            case ItemType.LegsGearItem:
                return true;
            case ItemType.RingGearItem:
                return true;
            case ItemType.NonInteractableItem:
                return false;
            case ItemType.ThrowableItem:
                return false;
            default:
                Debug.LogError("Item Type Not Found!!");
                return false;
        }
    }
    public bool IsEquippableItemType()
    {
        return IsUniqueItemType() || _ItemType == ItemType.ThrowableItem;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _Weight);
        serializer.SerializeValue(ref _ItemType);
        serializer.SerializeValue(ref _Name);
        serializer.SerializeValue(ref _Count);
        serializer.SerializeValue(ref _IsEquipped);
        serializer.SerializeValue(ref _HungerChange);
        serializer.SerializeValue(ref _HealthChange);
        serializer.SerializeValue(ref _ProtectionValue);
        serializer.SerializeValue(ref _AttackSpeedIncreaseValue);
        serializer.SerializeValue(ref _DamageIncreaseValue);
        serializer.SerializeValue(ref _HealthIncreaseValue);
        serializer.SerializeValue(ref _SpeedIncreaseValue);
        serializer.SerializeValue(ref _StaminaIncreaseValue);
        serializer.SerializeValue(ref _WorldInstanceIndex);
        serializer.SerializeValue(ref _Level);
        serializer.SerializeValue(ref _Durability);
        serializer.SerializeValue(ref _MaxDurability);
    }
}

