using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInputHandler : MonoBehaviour
{
    public LocomotionSystem _LocomotionSystem { get; set; }
    public GameObject _ObjForUIRaycast { get; private set; }
    private PlayerNetworking _playerNetworking;

    private GameObject _otherInventoryObject;
    private GameObject _lastClosestInventory;
    public bool _IsInCombatMode { get; private set; }

    private float _targetHorizontalAxisForCamera;
    private float _cameraRotateSpeed = 10f;
    private float _leftMouseLastTriggeredTime;
    private bool _isDoubleClickedThisFrame;
    private Item _leftClickLastTriggeredItem;
    private Item _holdingItem;
    private Inventory _holdingItemInventory;
    private int _holdingItemIndex;
    private bool _holdingItemIsEquipped;
    private bool _popupItemIsEquipped;
    private Item _popupItem;
    private int _popupItemIndex;
    private Inventory _popupInventory;

    public int _PopupSliderCount;
    public bool _IsHolding;


    private void Awake()
    {
        _playerNetworking = GetComponent<PlayerNetworking>();
    }
    private void Update()
    {
        _LocomotionSystem._MovementSpeedMultiplier = 1f;//armor quality(also decrease stamina), inventory load, from food
    }
    private void LateUpdate()
    {
        if (GameManager._Instance._HoldingItemUI.activeInHierarchy)
            GameManager._Instance._HoldingItemUI.GetComponent<RectTransform>().position = Mouse.current.position.ReadValue();
    }
    public virtual void OnAnimatorMove()
    {
        if (_LocomotionSystem == null) return;

        if (!GameManager._Instance._IsGameStopped && !GameManager._Instance._IsGameLoading)
            _LocomotionSystem.ControlAnimatorRootMotion(); // handle root motion animations 
    }

    #region UIMethods

    public void ArrangeOtherInventory()
    {
        if (_playerNetworking._ClosestInventory != _lastClosestInventory)
        {
            if (_lastClosestInventory != null)
                _lastClosestInventory.GetComponent<Inventory>().OpenOrCloseUseUI(false);

            _lastClosestInventory = _playerNetworking._ClosestInventory;

            if (_lastClosestInventory != null)
                _lastClosestInventory.GetComponent<Inventory>().OpenOrCloseUseUI(true);
        }

        if (GameManager._Instance._InputActions.FindAction("Use").triggered && _playerNetworking._ClosestInventory != null)
        {
            _otherInventoryObject = _playerNetworking._ClosestInventory;
            GameManager._Instance.OpenOrCloseOtherInventoryScreen(true, _playerNetworking._ClosestInventory.GetComponent<Inventory>());
        }

        if (GameManager._Instance._OtherInventoryScreen.activeInHierarchy && (_otherInventoryObject == null || (_otherInventoryObject.transform.position - transform.position).magnitude > 1.5f))
        {
            GameManager._Instance.OpenOrCloseOtherInventoryScreen(false, null);
        }
    }
    public void CheckInventoryActivity()
    {
        GraphicRaycast();

        if (GameManager._Instance._InputActions.FindAction("Attack").ReadValue<float>() == 0f) DisableHolding(true);
        if (GameManager._Instance._InputActions.FindAction("Aim").ReadValue<float>() == 0f) GameManager._Instance._ItemPopupUI.SetActive(false);
        if (!GameManager._Instance._ItemPopupUI.activeInHierarchy) _popupItem = null;
        if (GameManager._Instance._ItemPopupUI.activeInHierarchy) HoldPopup();

        if (_ObjForUIRaycast == null || _ObjForUIRaycast.GetComponent<InventoryUISlot>() == null || _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item == null)
        {
            if (_IsHolding)
                HoldItem();
            else
                CheckForStartHolding();
            return;
        }
        if (!_IsHolding && _ObjForUIRaycast == null || _ObjForUIRaycast.GetComponent<InventoryUISlot>() == null || _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item != _holdingItem)
            CheckForStartHolding();
        if (_IsHolding)
            HoldItem();

        if (GameManager._Instance._InputActions.FindAction("Attack").triggered)
        {
            _holdingItem = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item;
            _holdingItemInventory = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Inventory;
            _holdingItemIsEquipped = _holdingItem._IsEquipped;
            _holdingItemIndex = _holdingItemInventory.IndexOf(_holdingItem);
        }

        _isDoubleClickedThisFrame = false;

        if (_IsHolding) return;
        if (GameManager._Instance._InputActions.FindAction("Attack").ReadValue<float>() != 0f && GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() != 0f)
        {
            _holdingItem = null;
            QuickTakeOrSend();
        }
        else if (GameManager._Instance._InputActions.FindAction("Attack").triggered && GameManager._Instance._InputActions.FindAction("Crouch").ReadValue<float>() != 0f)
        {
            _holdingItem = null;
            SplitRequestSend();
        }
        else if (GameManager._Instance._InputActions.FindAction("Attack").triggered && Time.realtimeSinceStartup - _leftMouseLastTriggeredTime <= 0.3f && _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item == _leftClickLastTriggeredItem)
        {
            _holdingItem = null;
            DoubleClicked();
        }
        //Open Item Popup
        else if (GameManager._Instance._InputActions.FindAction("Aim").triggered && !_IsHolding)
        {
            _holdingItem = null;
            OpenItemPopup();
        }

        //first click for double click
        if (GameManager._Instance._InputActions.FindAction("Attack").triggered && !_isDoubleClickedThisFrame)
        {
            _leftClickLastTriggeredItem = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item;
            _leftMouseLastTriggeredTime = Time.realtimeSinceStartup;
        }
    }
    private void OpenItemPopup()
    {
        if (GameManager._Instance._ItemPopupUI.activeInHierarchy || _ObjForUIRaycast.GetComponent<InventoryUISlot>() == null || _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item == null) return;

        _popupItem = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item;
        _popupInventory = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Inventory;
        _popupItemIsEquipped = _popupItem._IsEquipped;
        _popupItemIndex = _popupItemIsEquipped ? _popupInventory._Equipments.IndexOf(_popupItem) : _popupInventory._Items.IndexOf(_popupItem);

        GameManager._Instance._ItemPopupUI.GetComponent<RectTransform>().position = _ObjForUIRaycast.GetComponent<RectTransform>().position;
        GameManager._Instance._ItemPopupUI.GetComponent<RectTransform>().anchoredPosition = new Vector3(Mathf.Clamp(GameManager._Instance._ItemPopupUI.GetComponent<RectTransform>().anchoredPosition.x, -1000f, 600f),
        Mathf.Clamp(GameManager._Instance._ItemPopupUI.GetComponent<RectTransform>().anchoredPosition.y, -400f, 50f));

        UpdateItemPopup();
    }
    public void UpdateItemPopup()
    {
        if (_popupItem == null) return;

        _PopupSliderCount = _popupItem._Count;
        ArrangeItemPopupButtons();
        ArrangeItemPopupDescription();

        GameManager._Instance._ItemPopupUI.SetActive(true);
    }
    private void ArrangeItemPopupDescription()
    {
        GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("Name").GetComponent<TextMeshProUGUI>().text = _popupItem._Name;
        GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("CountText").gameObject.SetActive(false);
        GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("EquippedText").gameObject.SetActive(false);
        GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("ArmorLevelText").gameObject.SetActive(false);
        GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("WeaponLevelText").gameObject.SetActive(false);
        GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("WeaponDurabilityText").gameObject.SetActive(false);

        if (!_popupItem.IsUniqueItemType())
        {
            GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("CountText").GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[21] + _popupItem._Count;
            GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("CountText").gameObject.SetActive(true);
        }
        else if (_popupItem._ItemType == ItemType.HandItem)
        {
            GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("WeaponLevelText").GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[23] + _popupItem._Level;
            GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("WeaponDurabilityText").GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[24] + _popupItem._Durability + "/" + _popupItem._MaxDurability;
            GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("EquippedText").GetComponent<TextMeshProUGUI>().text = _popupItemIsEquipped ? Localization._Instance._UI[25] : Localization._Instance._UI[26];

            GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("WeaponLevelText").gameObject.SetActive(true);
            GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("WeaponDurabilityText").gameObject.SetActive(true);
            GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("EquippedText").gameObject.SetActive(true);
        }
        else if (_popupItem._ItemType == ItemType.HeadGearItem || _popupItem._ItemType == ItemType.BodyGearItem || _popupItem._ItemType == ItemType.LegsGearItem)
        {
            GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("ArmorLevelText").GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[22] + _popupItem._ProtectionValue;
            GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("EquippedText").GetComponent<TextMeshProUGUI>().text = _popupItemIsEquipped ? Localization._Instance._UI[25] : Localization._Instance._UI[26];

            GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("ArmorLevelText").gameObject.SetActive(true);
            GameManager._Instance._ItemPopupUI.transform.Find("Description").Find("EquippedText").gameObject.SetActive(true);
        }
    }
    private void ArrangeItemPopupButtons()
    {
        GameManager._Instance._ItemPopupUI.transform.Find("Slider").GetComponent<Slider>().value = 1;
        GameManager._Instance._ItemPopupUI.transform.Find("SliderText").GetComponent<TextMeshProUGUI>().text = _PopupSliderCount.ToString() + "/" + _popupItem._Count;
        GameManager._Instance._ItemPopupUI.transform.Find("FirstButton").Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[!_popupItem.IsEquippableItemType() ? 16 : (_popupItem._IsEquipped ? 18 : 17)];
        GameManager._Instance._ItemPopupUI.transform.Find("SecondButton").Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[_popupInventory == GetComponent<Inventory>() ? 15 : 14];
        GameManager._Instance._ItemPopupUI.transform.Find("ThirdButton").Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[19];
        GameManager._Instance._ItemPopupUI.transform.Find("FourthButton").Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[20];

        if (_popupItem.IsUniqueItemType())
            OpenOrCloseButton(false, GameManager._Instance._ItemPopupUI.transform.Find("FourthButton").gameObject);
        else
            OpenOrCloseButton(true, GameManager._Instance._ItemPopupUI.transform.Find("FourthButton").gameObject);

        if (_popupItem.IsUniqueItemType() || _popupItem._ItemType == ItemType.FoodItem || _popupItem._ItemType == ItemType.PotionItem)
            OpenOrCloseButton(true, GameManager._Instance._ItemPopupUI.transform.Find("FirstButton").gameObject);
        else
            OpenOrCloseButton(false, GameManager._Instance._ItemPopupUI.transform.Find("FirstButton").gameObject);
    }
    private void OpenOrCloseButton(bool isOpening, GameObject button)
    {
        if (isOpening)
        {
            button.GetComponent<Button>().interactable = true;
        }
        else
        {
            button.GetComponent<Button>().interactable = false;
        }
    }
    public void UpdateSliderCount(float number)
    {
        if (_popupItem == null) return;
        _PopupSliderCount = (int)(number * _popupItem._Count);
        GameManager._Instance._ItemPopupUI.transform.Find("SliderText").GetComponent<TextMeshProUGUI>().text = _PopupSliderCount.ToString() + "/" + _popupItem._Count;
    }
    public void ItemPopupFirstButtonClicked()
    {
        _popupItem.Interact(GetComponent<Humanoid>(), _popupInventory);
    }
    public void ItemPopupSecondButtonClicked()
    {
        QuickTakeOrSend(_popupItem, _popupInventory, _PopupSliderCount);
    }
    public void ItemPopupThirdButtonClicked()
    {
        SplitRequestSend(_popupItem, _popupInventory, _PopupSliderCount);
    }
    public void ItemPopupFourthButtonClicked()
    {
        CombineAllSameItemsRequestSend(_popupItem, GetComponent<Inventory>());
    }
    private void CheckForStartHolding()
    {
        if (!_IsHolding && _holdingItem != null && !GameManager._Instance._ItemPopupUI.activeInHierarchy)
        {
            GameManager._Instance._HoldingItemUI.SetActive(true);
            _IsHolding = true;
            UpdateHoldingSprite();
        }
    }
    public void UpdateHoldingSprite()
    {
        if (!_IsHolding) return;

        GameManager._Instance._HoldingItemUI.GetComponent<Image>().sprite = GameManager._Instance.GetItemSprite(_holdingItem);

        if (_holdingItem.IsUniqueItemType())
            GameManager._Instance._HoldingItemUI.transform.Find("CountText").gameObject.SetActive(false);
        else
            GameManager._Instance._HoldingItemUI.transform.Find("CountText").gameObject.SetActive(true);

        if (_holdingItem._ItemType == ItemType.HandItem)
            GameManager._Instance._HoldingItemUI.transform.Find("WeaponUI").gameObject.SetActive(true);
        else
            GameManager._Instance._HoldingItemUI.transform.Find("WeaponUI").gameObject.SetActive(false);

        if (_holdingItem._ItemType == ItemType.HeadGearItem || _holdingItem._ItemType == ItemType.BodyGearItem || _holdingItem._ItemType == ItemType.LegsGearItem)
            GameManager._Instance._HoldingItemUI.transform.Find("ArmorLevelText").gameObject.SetActive(true);
        else
            GameManager._Instance._HoldingItemUI.transform.Find("ArmorLevelText").gameObject.SetActive(false);

        if (GameManager._Instance._HoldingItemUI.GetComponentInChildren<SlotArmorUI>() != null)
            GameManager._Instance._HoldingItemUI.GetComponentInChildren<SlotArmorUI>().GetComponent<TextMeshProUGUI>().text = _holdingItem._ProtectionValue.ToString();

        if (GameManager._Instance._HoldingItemUI.GetComponentInChildren<SlotCountTextUI>() != null)
            GameManager._Instance._HoldingItemUI.GetComponentInChildren<SlotCountTextUI>().GetComponent<TextMeshProUGUI>().text = _holdingItem._Count.ToString();

        if (GameManager._Instance._HoldingItemUI.GetComponentInChildren<SlotWeaponUI>() != null)
        {
            GameManager._Instance._HoldingItemUI.GetComponentInChildren<SlotWeaponUI>().transform.Find("LevelText").GetComponent<TextMeshProUGUI>().text = "+" + _holdingItem._Level.ToString();
            GameManager._Instance._HoldingItemUI.GetComponentInChildren<SlotWeaponUI>().transform.Find("DurabilityText").GetComponent<TextMeshProUGUI>().text = "+" + (_holdingItem._Durability / _holdingItem._MaxDurability).ToString();
            if ((_holdingItem._Durability / _holdingItem._MaxDurability) <= 5f)
                GameManager._Instance._HoldingItemUI.GetComponentInChildren<SlotWeaponUI>().transform.Find("BrokenImage").gameObject.SetActive(true);
            else
                GameManager._Instance._HoldingItemUI.GetComponentInChildren<SlotWeaponUI>().transform.Find("BrokenImage").gameObject.SetActive(false);
        }
    }
    public void DisableHolding(bool isCheckingForTransition)
    {
        if (_holdingItem == null) return;

        if (_IsHolding && isCheckingForTransition)
        {
            if (_ObjForUIRaycast == null)
            {
                if (_holdingItem._IsEquipped)
                    _holdingItemInventory.FromEquipmentToGroundRequestSend(_holdingItem._Count, _holdingItemInventory._Equipments.IndexOf(_holdingItem));
                else
                    _holdingItemInventory.ItemToGroundRequestSend(_holdingItem._Count, _holdingItemInventory._Items.IndexOf(_holdingItem));
            }
            else if (_ObjForUIRaycast != null && _ObjForUIRaycast.GetComponent<InventoryUISlot>() == null)
            {
                if (!IsHoldingItemStillInSameInventory())
                    QuickTakeOrSend(_holdingItem, _holdingItemInventory);
                else if (_holdingItem._IsEquipped && _holdingItemInventory.CanTakeThisItem(_holdingItem))
                    NetworkController._Instance.UnEquipRequestSend(_holdingItem, _holdingItemInventory, _holdingItemIndex);
            }
            else if (_ObjForUIRaycast != null && _ObjForUIRaycast.GetComponent<InventoryUISlot>() != null && _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item != null)
            {
                ItemExchanceRequestSend(_holdingItemInventory, _holdingItemIndex, _holdingItem, _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Inventory, _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Index, _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item);
            }
            else if (_ObjForUIRaycast != null && _ObjForUIRaycast.GetComponent<InventoryUISlot>() != null && _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item == null && _holdingItem._IsEquipped && IsHoldingItemStillInSameInventory() && _holdingItemInventory.CanTakeThisItem(_holdingItem) && _ObjForUIRaycast.transform.parent.name == "InventorySlots")
            {
                NetworkController._Instance.UnEquipRequestSend(_holdingItem, _holdingItemInventory, _holdingItemIndex);
            }
            else if (_ObjForUIRaycast != null && _ObjForUIRaycast.GetComponent<InventoryUISlot>() != null && _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item == null && _holdingItemInventory.CanEquipThisItemType(_holdingItem, _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Index) && (_ObjForUIRaycast.transform.parent.name == "ArmorSlots" || _ObjForUIRaycast.transform.parent.name == "EquipmentSlots"))
            {
                NetworkController._Instance.EquipRequestSend(_holdingItem, GetComponent<Humanoid>(), _holdingItemInventory, _holdingItemIndex, _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Index);
            }
            else if (_ObjForUIRaycast != null && _ObjForUIRaycast.GetComponent<InventoryUISlot>() != null && _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item == null)
            {
                if (!IsHoldingItemStillInSameInventory())
                    QuickTakeOrSend(_holdingItem, _holdingItemInventory);
            }

        }

        if (_IsHolding)
            _leftMouseLastTriggeredTime = 0f;
        _holdingItem = null;
        GameManager._Instance._HoldingItemUI.SetActive(false);
        _IsHolding = false;

        GameManager._Instance.CheckInventoryUpdate(GetComponent<Inventory>());
        if (GameManager._Instance._OtherInventoryScreen.activeInHierarchy)
            GameManager._Instance.CheckInventoryUpdate(NetworkController._Instance.GetObjectFromNetworkID(GameManager._Instance._OtherInventoryObjectID).GetComponent<Inventory>());

    }
    
    private void CombineAllSameItemsRequestSend(Item item, Inventory inventory)
    {
        _playerNetworking.CombineAllSameItemsRequestRpc(inventory.NetworkObjectId, inventory.IndexOf(item), item._IsEquipped, item._Name);
    }
    private void ItemExchanceRequestSend(Inventory firstInventory, int firstIndex, Item firstItem, Inventory secondInventory, int secondIndex, Item secondItem)
    {
        _playerNetworking.ItemExchangeRequestRpc(firstInventory.NetworkObjectId, firstInventory.IndexOf(firstItem), firstItem._IsEquipped, firstItem._Name, secondInventory.NetworkObjectId, secondInventory.IndexOf(secondItem), secondItem._IsEquipped, secondItem._Name);
    }

    private bool IsHoldingItemStillInSameInventory()
    {
        Transform temp = _ObjForUIRaycast.transform;
        while (temp.parent != null)
        {
            if (temp.name == "OwnInventory")
            {
                return GetComponent<Inventory>() == _holdingItemInventory;
            }
            if (temp.name == "OtherInventory")
            {
                return NetworkController._Instance.GetObjectFromNetworkID(GameManager._Instance._OtherInventoryObjectID).GetComponent<Inventory>() == _holdingItemInventory;
            }
            temp = temp.parent;
        }
        Debug.LogError("UI not found.");
        return false;
    }
    private void HoldPopup()
    {
        if (GetPopupItemFromIndex() != _popupItem)
            GameManager._Instance._ItemPopupUI.SetActive(false);
    }
    private Item GetPopupItemFromIndex()
    {
        if (_popupItemIsEquipped)
            return _popupInventory._Equipments[_popupItemIndex];
        else
            return _popupInventory._Items[_popupItemIndex];
    }
    private void HoldItem()
    {
        if (GetHoldingItemFromIndex() != _holdingItem)
            DisableHolding(false);
    }

    private Item GetHoldingItemFromIndex()
    {
        if (_holdingItemIsEquipped)
            return _holdingItemInventory._Equipments[_holdingItemIndex];
        else
            return _holdingItemInventory._Items[_holdingItemIndex];
    }
    private void DoubleClicked()
    {
        if (_ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item.IsEquippableItemType())
            _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item.Interact(GetComponent<Humanoid>(), _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Inventory);
        else
            QuickTakeOrSend();

        _isDoubleClickedThisFrame = true;
        _leftMouseLastTriggeredTime = 0f;
    }
    private void SplitRequestSend(Item item = null, Inventory inventory = null, int count = -1)
    {
        if (item == null)
            item = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item;
        if (inventory == null)
            inventory = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Inventory;

        _playerNetworking.SplitRequestRpc(inventory.IndexOf(item), inventory.NetworkObjectId, item._Name, item._IsEquipped, count);
    }

    private void QuickTakeOrSend(Item item = null, Inventory inventory = null, int count = -1)
    {
        if (count == 0) return;

        if (item == null)
            item = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item;
        if (inventory == null)
            inventory = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Inventory;
        if (count == -1)
            count = item._Count;
        int fromIndex = inventory.IndexOf(item);
        if (fromIndex == -1) return;
        bool isFromEquipments = inventory.IsEquipped(item);

        if (NetworkController._Instance.GetOwnPlayerObject().GetComponent<Inventory>() == inventory)
        {
            if (GameManager._Instance._OtherInventoryScreen.activeInHierarchy && NetworkController._Instance.GetObjectFromNetworkID(GameManager._Instance._OtherInventoryObjectID) != null)
            {
                NetworkController._Instance.GetObjectFromNetworkID(GameManager._Instance._OtherInventoryObjectID).GetComponent<Inventory>().TakeItemFromAnotherRequestSend(item, inventory, count, isFromEquipments);
            }
            else
            {
                if (isFromEquipments)
                    inventory.FromEquipmentToGroundRequestSend(count, fromIndex);
                else
                    inventory.ItemToGroundRequestSend(count, fromIndex);
            }
        }
        else
        {
            NetworkController._Instance.GetOwnPlayerObject().GetComponent<Inventory>().TakeItemFromAnotherRequestSend(item, inventory, count, isFromEquipments);
        }
    }
    private void GraphicRaycast()
    {
        GameManager._Instance._PointerEventData = new PointerEventData(GameManager._Instance._EventSystem);
        GameManager._Instance._PointerEventData.position = Mouse.current.position.ReadValue();
        List<RaycastResult> results = new List<RaycastResult>();

        GameManager._Instance._Raycaster.Raycast(GameManager._Instance._PointerEventData, results);

        _ObjForUIRaycast = null;
        foreach (RaycastResult result in results)
        {
            if (result.gameObject != null && result.gameObject.GetComponent<InventoryUISlot>() != null)
            {
                _ObjForUIRaycast = result.gameObject;
                break;
            }
        }
        if (_ObjForUIRaycast == null)
        {
            foreach (RaycastResult result in results)
            {
                if (result.gameObject != null)
                {
                    _ObjForUIRaycast = result.gameObject;
                    break;
                }
            }
        }
    }
    #endregion

    #region Basic Locomotion Methods

    public virtual void InitilizeController()
    {
        _LocomotionSystem = GetComponent<LocomotionSystem>();

        if (_LocomotionSystem != null)
            _LocomotionSystem.Init();
    }

    public virtual void ArrangeCameraFollow()
    {
        if (GameManager._Instance._CinemachineCamera.Follow != transform)
            GameManager._Instance._CinemachineCamera.Follow = transform;
    }

    public virtual void CameraRotateInput()
    {
        if (GameManager._Instance._InputActions.FindAction("CameraLook").ReadValue<float>() != 0f)
        {
            _targetHorizontalAxisForCamera += Time.deltaTime * _cameraRotateSpeed * GameManager._Instance._InputActions.FindAction("CameraRotation").ReadValue<Vector2>().x;
        }

        CinemachineOrbitalFollow orbital = GameManager._Instance._CinemachineCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachineOrbitalFollow;
        orbital.HorizontalAxis.Value = Mathf.Lerp(orbital.HorizontalAxis.Value, _targetHorizontalAxisForCamera, Time.deltaTime * 4f);
    }
    public virtual void MoveInput()
    {
        var moveInput = GameManager._Instance._InputActions.FindAction("Move").ReadValue<Vector2>();
        _LocomotionSystem.input.x = moveInput.x;
        _LocomotionSystem.input.z = moveInput.y;
    }
    public virtual void CameraInput()
    {
        if (GameManager._Instance._MainCamera != null)
        {
            _LocomotionSystem.rotateTarget = GameManager._Instance._MainCamera.transform;
            _LocomotionSystem.UpdateMoveDirection(GameManager._Instance._MainCamera.transform);
        }
    }
    public virtual void StrafeInput()
    {
        if (GameManager._Instance._InputActions.FindAction("CombatMode").triggered)
            _IsInCombatMode = !_IsInCombatMode;

        bool isStrafingConditions = _IsInCombatMode && _ObjForUIRaycast == null;
        _LocomotionSystem.Strafe(isStrafingConditions, Mouse.current.position.ReadValue());
    }

    public virtual void SprintInput()
    {
        if (_LocomotionSystem._rigidbody.linearVelocity.magnitude < 1.5f || _LocomotionSystem.input.magnitude == 0f || GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0f)
            _LocomotionSystem.Sprint(false);
        else if (GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() != 0f)
            _LocomotionSystem.Sprint(true);
    }

    /// <summary>
    /// Conditions to trigger the Jump animation & behavior
    /// </summary>
    /// <returns></returns>
    public virtual bool JumpConditions()
    {
        return _LocomotionSystem.isGrounded && _LocomotionSystem.GroundAngle() < _LocomotionSystem.slopeLimit && !_LocomotionSystem.isJumping && !_LocomotionSystem.stopMove;
    }

    /// <summary>
    /// Input to trigger the Jump 
    /// </summary>
    public virtual void JumpInput()
    {
        if (GameManager._Instance._InputActions.FindAction("Jump").triggered && JumpConditions())
            _LocomotionSystem.Jump();
    }

    #endregion
}
