using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
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

    private float _targetHorizontalAxisForCamera;
    private float _cameraRotateSpeed = 10f;
    private float _leftMouseLastTriggeredTime;
    private bool _isDoubleClickedThisFrame;
    private Item _leftClickLastTriggeredItem;
    private Item _holdingItem;
    private Inventory _holdingItemInventory;
    private int _holdingItemIndex;
    private bool _holdingItemIsEquipped;
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

        if (_ObjForUIRaycast == null || _ObjForUIRaycast.GetComponent<InventoryUISlot>() == null || _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item == null)
        {
            if (_holdingItem != null)
                HoldItem();
            return;
        }
        if (_holdingItem != null)
        {
            HoldItem();
            return;
        }

        if (GameManager._Instance._InputActions.FindAction("Attack").triggered)
        {
            _holdingItem = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item;
            _holdingItemInventory = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Inventory;
            _holdingItemIsEquipped = _holdingItem._IsEquipped;
            _holdingItemIndex = _holdingItemInventory.IndexOf(_holdingItem);
            GameManager._Instance._HoldingItemUI.SetActive(true);
            UpdateHoldingSprite();
        }
        _isDoubleClickedThisFrame = false;

        if (GameManager._Instance._InputActions.FindAction("Attack").ReadValue<float>() != 0f && GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() != 0f)
        {
            DisableHolding(false);
            QuickTakeOrSend();
        }
        else if (GameManager._Instance._InputActions.FindAction("Attack").triggered && GameManager._Instance._InputActions.FindAction("Crouch").ReadValue<float>() != 0f)
        {
            DisableHolding(false);
            SplitInHalf();
        }
        else if (GameManager._Instance._InputActions.FindAction("Attack").triggered && Time.realtimeSinceStartup - _leftMouseLastTriggeredTime <= 0.5f && _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item == _leftClickLastTriggeredItem)
        {
            DisableHolding(false);
            DoubleClicked();
        }

        //Open Item Popup
        else if (GameManager._Instance._InputActions.FindAction("Aim").ReadValue<float>() != 0f)
        {

        }


        if (GameManager._Instance._InputActions.FindAction("Attack").triggered && !_isDoubleClickedThisFrame)
        {
            _leftClickLastTriggeredItem = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item;
            _leftMouseLastTriggeredTime = Time.realtimeSinceStartup;
        }
    }
    public void UpdateHoldingSprite()
    {
        if (_holdingItem == null) return;

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
            GameManager._Instance._HoldingItemUI.GetComponentInChildren<SlotWeaponUI>().transform.Find("DurabilityText").GetComponent<TextMeshProUGUI>().text = "+" + _holdingItem._Durability.ToString();
            if (_holdingItem._Durability <= 5f)
                GameManager._Instance._HoldingItemUI.GetComponentInChildren<SlotWeaponUI>().transform.Find("BrokenImage").gameObject.SetActive(true);
            else
                GameManager._Instance._HoldingItemUI.GetComponentInChildren<SlotWeaponUI>().transform.Find("BrokenImage").gameObject.SetActive(false);
        }
    }
    private void DisableHolding(bool isCheckingForTransition)
    {
        GameManager._Instance.CheckInventoryUpdate(GetComponent<Inventory>());
        if (GameManager._Instance._OtherInventoryScreen.activeInHierarchy)
            GameManager._Instance.CheckInventoryUpdate(NetworkMethods._Instance.GetObjectFromNetworkID(GameManager._Instance._OtherInventoryObjectID).GetComponent<Inventory>());

        if (_holdingItem != null && isCheckingForTransition)
        {
            if (_ObjForUIRaycast == null)
            {
                if (_holdingItem._IsEquipped)
                    _holdingItemInventory.FromEquipmentToGround(_holdingItem, _holdingItem._Count, _holdingItemInventory.IndexOf(_holdingItem), true);
                else
                    _holdingItemInventory.ItemToGround(_holdingItem, _holdingItem._Count, _holdingItemInventory.IndexOf(_holdingItem), true);
            }
            else if (_ObjForUIRaycast != null && _ObjForUIRaycast.GetComponent<InventoryUISlot>() == null)
            {
                if (!IsHoldingItemStillInSameInventory())
                    QuickTakeOrSend(_holdingItem, _holdingItemInventory);
            }
            else if (_ObjForUIRaycast != null && _ObjForUIRaycast.GetComponent<InventoryUISlot>() != null && (_ObjForUIRaycast.transform.parent.name == "ArmorSlots" || _ObjForUIRaycast.transform.parent.name == "EquipmentSlots"))
            {
                _holdingItem.Equip(GetComponent<Humanoid>(), _holdingItemInventory, _holdingItemIndex, _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Index);
            }
            else if (_ObjForUIRaycast != null && _ObjForUIRaycast.GetComponent<InventoryUISlot>() != null && _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item == null)
            {
                if (!IsHoldingItemStillInSameInventory())
                    QuickTakeOrSend(_holdingItem, _holdingItemInventory);
            }
            else if (_ObjForUIRaycast != null && _ObjForUIRaycast.GetComponent<InventoryUISlot>() != null && _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item != null)
            {
                //change two items
            }
        }

        _holdingItem = null;
        GameManager._Instance._HoldingItemUI.SetActive(false);
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
                return NetworkMethods._Instance.GetObjectFromNetworkID(GameManager._Instance._OtherInventoryObjectID).GetComponent<Inventory>() == _holdingItemInventory;
            }
            temp = temp.parent;
        }
        Debug.LogError("UI not found.");
        return false;
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
    private void SplitInHalf()
    {
        Item item = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item;
        Inventory inventory = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Inventory;
        if (item._Count <= 1 || inventory.IsFull()) return;

        int index = inventory._Items.GetFirstEmptyIndex();
        int halfAmount = item._Count / 2;
        item._Count -= halfAmount;
        inventory.GainItem(item.Copy(), halfAmount, index);
    }
    private void QuickTakeOrSend(Item item = null, Inventory inventory = null)
    {
        if (item == null)
            item = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Item;
        if (inventory == null)
            inventory = _ObjForUIRaycast.GetComponent<InventoryUISlot>()._Inventory;
        int fromIndex = inventory.IndexOf(item);
        bool isFromEquipments = inventory.IsEquipped(item);

        if (NetworkMethods._Instance.GetOwnPlayerObject().GetComponent<Inventory>() == inventory)
        {
            if (GameManager._Instance._OtherInventoryScreen.activeInHierarchy && NetworkMethods._Instance.GetObjectFromNetworkID(GameManager._Instance._OtherInventoryObjectID) != null)
            {
                if (isFromEquipments)
                    NetworkMethods._Instance.GetObjectFromNetworkID(GameManager._Instance._OtherInventoryObjectID).GetComponent<Inventory>().TakeItemFromNothing(item, item._Count);
                else
                    NetworkMethods._Instance.GetObjectFromNetworkID(GameManager._Instance._OtherInventoryObjectID).GetComponent<Inventory>().TakeItemRequestSend(item, inventory, item._Count);

                if (isFromEquipments && NetworkMethods._Instance.GetObjectFromNetworkID(GameManager._Instance._OtherInventoryObjectID).GetComponent<Inventory>().CanTakeThisItem(item))
                    item.UnEquip(inventory, fromIndex, true, false);
            }
            else
            {
                if (isFromEquipments)
                    inventory.FromEquipmentToGround(item, item._Count, fromIndex, true);
                else
                    inventory.ItemToGround(item, item._Count, fromIndex, true);
            }
        }
        else
        {
            NetworkMethods._Instance.GetOwnPlayerObject().GetComponent<Inventory>().TakeItemRequestSend(item, inventory, item._Count);
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
        bool isStrafingConditions = GameManager._Instance._InputActions.FindAction("Aim").ReadValue<float>() != 0f && _ObjForUIRaycast == null;
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
