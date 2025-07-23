using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    public static GameManager _Instance;

    public LayerMask _UseInventoryLayerMask;
    public GameObject _MainCamera { get; private set; }
    public CinemachineCamera _CinemachineCamera { get; private set; }
    public GameObject _StopScreen { get; private set; }
    public GameObject _InGameScreen { get; private set; }
    public GameObject _OptionsScreen { get; private set; }
    public GameObject _InventoryScreen { get; private set; }
    public GameObject _OtherInventoryScreen { get; private set; }
    public GameObject _BookScreen { get; private set; }
    public GameObject _LoadingScreen { get; private set; }
    public GameObject _HoldingItemUI { get; private set; }
    public GameObject _ItemPopupUI { get; private set; }

    public InputActionAsset _InputActions;


    public List<GameObject> _AllNetworkPrefabs;
    public List<Item> _AllItems;
    public List<Sprite> _AllItemIcons;
    public Sprite _ItemBackgroundIcon;
    public Sprite _HandsBackgroundIcon;
    public Sprite _ThrowableBackgroundIcon;
    public Sprite _RingBackgroundIcon;
    public Sprite _HeadGearBackgroundIcon;
    public Sprite _BodyGearBackgroundIcon;
    public Sprite _LegsGearBackgroundIcon;

    public List<GameObject> _AllStaticInventories;

    public bool _IsGameStopped { get; private set; }
    public bool _IsGameLoading { get; set; }
    public int _SaveIndex { get; set; }
    public int _LevelIndex { get; private set; }
    public int _LastLoadedGameIndex { get; set; }

    public ulong _OtherInventoryObjectID { get; private set; }

    public GraphicRaycaster _Raycaster { get; set; }
    public PointerEventData _PointerEventData { get; set; }
    public EventSystem _EventSystem { get; set; }

    private Coroutine _slowTimeCoroutine;
    private Color _uniqueItemColor;

    private void Awake()
    {
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
        _Instance = this;
        _uniqueItemColor = new Color(1f, 0.9f, 0.6f);
        _MainCamera = Camera.main.gameObject;
        _CinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
        //Application.targetFrameRate = 30;
        _OptionsScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("Options").gameObject;
        _LoadingScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("Loading").gameObject;

        _LevelIndex = SceneManager.GetActiveScene().buildIndex;
        if (_LevelIndex != 0)
        {
            _StopScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").gameObject;
            _InGameScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").gameObject;
            _BookScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("BookScreen").gameObject;
            _InventoryScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("Inventories").Find("OwnInventory").gameObject;
            _OtherInventoryScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("Inventories").Find("OtherInventory").gameObject;
            _HoldingItemUI = GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("Inventories").Find("HoldingItemUI").gameObject;
            _ItemPopupUI = GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("Inventories").Find("Popup").gameObject;
            _Raycaster = GameObject.Find("UI").GetComponent<GraphicRaycaster>();
            _EventSystem = FindFirstObjectByType<EventSystem>();
        }
        
        _AllStaticInventories = new List<GameObject>();
    }

    private void Testing()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsClient && Input.GetKeyDown(KeyCode.F1))
        {
            NetworkManager.Singleton.StartHost();
        }
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsClient && Input.GetKeyDown(KeyCode.F2))
        {
            NetworkManager.Singleton.StartClient();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            NetworkController._Instance.GetOwnPlayerObject().GetComponent<Inventory>().TakeItemFromNothing(GameManager._Instance._AllItems[2].Copy(),1);
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            NetworkController._Instance.GetOwnPlayerObject().GetComponent<Inventory>().TakeItemFromNothing(GameManager._Instance._AllItems[0].Copy(),1);
        }

        if (Input.GetKeyDown(KeyCode.L)) { SaveSystemHandler.LoadGame(0); }
        if (Input.GetKeyDown(KeyCode.K)) { SaveSystemHandler.SaveGame(0); }

    }
    private void Update()
    {
        Testing();

        if (_InputActions.FindAction("Inventory").triggered && _LevelIndex != 0 && !_IsGameStopped)
        {
            OpenOrCloseInventoryScreen(!_InventoryScreen.activeInHierarchy);
        }
        else if (_InputActions.FindAction("Book").triggered && _LevelIndex != 0 && !_IsGameStopped)
        {
            OpenOrCloseBookScreen(!_BookScreen.activeInHierarchy);
        }

        if (_InputActions.FindAction("Cancel").triggered)
        {
            if (_LevelIndex != 0)
            {
                if (_IsGameStopped)
                {
                    if (_OptionsScreen.activeInHierarchy)
                        CloseOptionsScreen();
                    else if (_StopScreen.activeInHierarchy)
                        UnstopGame();
                    else
                        StopGame();
                }
                else
                {
                    if (_BookScreen.activeInHierarchy)
                        OpenOrCloseBookScreen(false);
                    else if (_OtherInventoryScreen.activeInHierarchy)
                    {
                        OpenOrCloseOtherInventoryScreen(false, null);
                        OpenOrCloseInventoryScreen(false);
                    }
                    else if (_InventoryScreen.activeInHierarchy)
                        OpenOrCloseInventoryScreen(false);
                    else
                        StopGame();
                }
            }
            else
            {
                if (_OptionsScreen.activeInHierarchy)
                    CloseOptionsScreen();
            }
        }
    }


    #region CommonMethods

    public T GetRandomFromList<T>(List<T> list)
    {
        return list[UnityEngine.Random.Range(0, list.Count)];
    }
    public static void ShuffleList(List<string> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            string value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    /// <param name="speed">1/second</param>
    public float LinearLerpFloat(float startValue, float endValue, float speed, float startTime)
    {
        float endTime = startTime + 1 / speed;
        return Mathf.Lerp(startValue, endValue, (Time.time - startTime) / (endTime - startTime));
    }
    /// <param name="speed">1/second</param>
    public Vector2 LinearLerpVector2(Vector2 startValue, Vector2 endValue, float speed, float startTime)
    {
        float endTime = startTime + 1 / speed;
        return Vector2.Lerp(startValue, endValue, (Time.time - startTime) / (endTime - startTime));
    }
    /// <param name="speed">1/second</param>
    public Vector3 LinearLerpVector3(Vector3 startValue, Vector3 endValue, float speed, float startTime)
    {
        float endTime = startTime + 1 / speed;
        return Vector3.Lerp(startValue, endValue, (Time.time - startTime) / (endTime - startTime));
    }

    /// <param name="speed">1/second</param>
    public float LimitLerpFloat(float startValue, float endValue, float speed)
    {
        if (endValue - startValue != 0f)
            return Mathf.Lerp(startValue, endValue, Time.deltaTime * speed * 7f * (endValue - startValue));
        return endValue;
    }
    /// <param name="speed">1/second</param>
    public Vector2 LimitLerpVector2(Vector2 startValue, Vector2 endValue, float speed)
    {
        if ((endValue - startValue).magnitude != 0f)
            return Vector2.Lerp(startValue, endValue, Time.deltaTime * speed * 7f / (endValue - startValue).magnitude);
        return endValue;
    }
    /// <param name="speed">1/second</param>
    public Vector3 LimitLerpVector3(Vector3 startValue, Vector3 endValue, float speed)
    {
        if ((endValue - startValue).magnitude != 0f)
            return Vector3.Lerp(startValue, endValue, Time.deltaTime * speed * 7f / (endValue - startValue).magnitude);
        return endValue;
    }

    public bool RandomPercentageChance(float percentage)
    {
        return percentage >= UnityEngine.Random.Range(1f, 99f);
    }

    public void CoroutineCall(ref Coroutine coroutine, IEnumerator method, MonoBehaviour script)
    {
        if (coroutine != null)
            script.StopCoroutine(coroutine);
        coroutine = script.StartCoroutine(method);
    }
    public void CallForAction(System.Action action, float time, bool isRealtime)
    {
        StartCoroutine(CallForActionCoroutine(action, time, isRealtime));
    }
    private IEnumerator CallForActionCoroutine(System.Action action, float time, bool isRealtime)
    {
        if (isRealtime)
            yield return new WaitForSecondsRealtime(time);
        else
            yield return new WaitForSeconds(time);
        action?.Invoke();
    }
    public Transform GetParent(Transform tr)
    {
        Transform parentTransform = tr.transform;
        while (parentTransform.parent != null)
        {
            parentTransform = parentTransform.parent;
        }
        return parentTransform;
    }
    public Vector3 RotateVector3OnYAxis(Vector3 baseVector, float angle)
    {
        return Quaternion.AngleAxis(angle, Vector3.up) * baseVector;

    }
    public void BufferActivated(ref bool buffer, MonoBehaviour coroutineHolderScript, ref Coroutine coroutine)
    {
        buffer = false;
        if (coroutine != null)
            coroutineHolderScript.StopCoroutine(coroutine);
    }
    #endregion


    public void QuitGame()
    {
        Application.Quit();
    }


    public void StopGame(bool isStopScreen = true)
    {
        if (isStopScreen)
        {
            _StopScreen.SetActive(true);
            _InGameScreen.SetActive(false);
        }

        Time.timeScale = 0f;
        _IsGameStopped = true;
        SoundManager._Instance.PauseAllSound();
        SoundManager._Instance.PauseMusic();
    }
    public void UnstopGame()
    {
        _StopScreen.SetActive(false);
        _InGameScreen.SetActive(true);
        CloseOptionsScreen(false);
        _IsGameStopped = false;
        Time.timeScale = 1f;
        SoundManager._Instance.ContinueAllSound();
        SoundManager._Instance.ContinueMusic();
    }

    public void OpenOptionsScreen()
    {
        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").gameObject.SetActive(false);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").gameObject.SetActive(false);
        }
    }
    public void CloseOptionsScreen(bool isOpeningMenu = true)
    {
        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").gameObject.SetActive(false);
            if (isOpeningMenu)
                GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").gameObject.SetActive(true);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").gameObject.SetActive(false);
            if (isOpeningMenu)
                GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").gameObject.SetActive(true);
        }
    }
    public void OpenOrCloseBookScreen(bool isOpening)
    {
        _BookScreen.SetActive(isOpening);
    }

    public void ItemPopupFirstButtonClicked()
    {
        NetworkController._Instance.GetOwnPlayerObject().GetComponent<PlayerInputHandler>().ItemPopupFirstButtonClicked();
        _ItemPopupUI.SetActive(false);
    }
    public void ItemPopupSecondButtonClicked()
    {
        NetworkController._Instance.GetOwnPlayerObject().GetComponent<PlayerInputHandler>().ItemPopupSecondButtonClicked();
        _ItemPopupUI.SetActive(false);
    }
    public void ItemPopupThirdButtonClicked()
    {
        NetworkController._Instance.GetOwnPlayerObject().GetComponent<PlayerInputHandler>().ItemPopupThirdButtonClicked();
        _ItemPopupUI.SetActive(false);
    }
    public void ItemPopupFourthButtonClicked()
    {
        NetworkController._Instance.GetOwnPlayerObject().GetComponent<PlayerInputHandler>().ItemPopupFourthButtonClicked();
        _ItemPopupUI.SetActive(false);
    }
    public void UpdateSliderCount(float number)
    {
        NetworkController._Instance.GetOwnPlayerObject().GetComponent<PlayerInputHandler>().UpdateSliderCount(number);
    }
    public Sprite GetItemSprite(Item item)
    {
        int index = NetworkController._Instance.GetIndexByItem(item);
        return GameManager._Instance._AllItemIcons[index];
    }

    public bool CheckInventoryUpdate(Inventory inventory)
    {
        if (_InventoryScreen.activeInHierarchy && inventory.GetComponent<PlayerNetworking>() != null && inventory.GetComponent<PlayerNetworking>().IsOwner)
        {
            UpdateInventoryScreen(true, inventory);
        }
        if (_OtherInventoryScreen.activeInHierarchy && inventory.NetworkObjectId == _OtherInventoryObjectID)
        {
            UpdateInventoryScreen(false, inventory);
        }
        return false;
    }
    public void OpenOrCloseInventoryScreen(bool isOpening)
    {
        if (isOpening)
        {
            UpdateInventoryScreen(true, NetworkController._Instance.GetOwnPlayerObject().GetComponent<Inventory>());
        }
        else
        {
            NetworkController._Instance.GetOwnPlayerObject().GetComponent<PlayerInputHandler>().DisableHolding(false);
            _ItemPopupUI.SetActive(false);
        }

        _InventoryScreen.SetActive(isOpening);
    }

    public void OpenOrCloseOtherInventoryScreen(bool isOpening, Inventory inventory)
    {
        if (isOpening)
        {
            OpenOrCloseInventoryScreen(true);
            UpdateInventoryScreen(false, inventory);
        }
        else
        {
            Transform slots = _OtherInventoryScreen.transform.Find("InventorySlots");
            for (int i = 0; i < slots.childCount; i++)
            {
                Transform child = slots.GetChild(i);
                child.GetComponent<InventoryUISlot>()._Inventory = null;
            }
        }

        _OtherInventoryScreen.SetActive(isOpening);
    }
    public void CloseOtherInventoryFromUI()
    {
        OpenOrCloseOtherInventoryScreen(false, null);
    }
    public void UpdateInventoryScreen(bool isOwnInventory, Inventory inventory)
    {
        if (isOwnInventory)
        {
            UpdateInventorySlots(inventory, _InventoryScreen.transform.Find("InventorySlots"));
            UpdateEquipmentSlots(inventory);
        }
        else
        {
            _OtherInventoryObjectID = inventory.NetworkObjectId;
            UpdateInventorySlots(inventory, _OtherInventoryScreen.transform.Find("InventorySlots"));
        }
        NetworkController._Instance.GetOwnPlayerObject().GetComponent<PlayerInputHandler>().UpdateHoldingSprite();
        NetworkController._Instance.GetOwnPlayerObject().GetComponent<PlayerInputHandler>().UpdateItemPopup();
        NetworkController._Instance.GetOwnPlayerObject().GetComponent<PlayerInputHandler>().UpdateSliderCount(_ItemPopupUI.transform.Find("Slider").GetComponent<Slider>().value);
    }
    private void UpdateInventorySlots(Inventory inventory, Transform inventoryScreen)
    {
        for (int i = 0; i < inventoryScreen.childCount; i++)
        {
            Transform child = inventoryScreen.GetChild(i);
            UpdateSlotsCommon(inventory, child, i, false);
        }
    }
    private void UpdateEquipmentSlots(Inventory inventory)
    {
        Transform slots = _InventoryScreen.transform.Find("EquipmentSlots");
        for (int i = 3; i < slots.childCount + 3; i++)
        {
            Transform child = slots.GetChild(i - 3);
            UpdateSlotsCommon(inventory, child, i, true);
        }

        slots = _InventoryScreen.transform.Find("ArmorSlots");
        for (int i = 0; i < 3; i++)
        {
            Transform child = slots.GetChild(i);
            UpdateSlotsCommon(inventory, child, i, true);
        }
    }
    private void UpdateSlotsCommon(Inventory inventory, Transform child, int i, bool isEquipments)
    {
        Item[] tempArray = isEquipments ? inventory._Equipments : inventory._Items;
        if (tempArray[i] == null)
        {
            child.GetComponent<Image>().sprite = isEquipments ? GetBackgroundIcon(i) : _ItemBackgroundIcon;
            child.GetComponent<Image>().color = Color.white;
            child.Find("CountText").gameObject.SetActive(false);
            child.Find("WeaponUI").gameObject.SetActive(false);
            child.Find("ArmorLevelText").gameObject.SetActive(false);
        }
        else
        {
            child.GetComponent<Image>().sprite = GetItemSprite(tempArray[i]);

            if (tempArray[i].IsUniqueItemType())
            {
                child.Find("CountText").gameObject.SetActive(false);
                child.GetComponent<Image>().color = _uniqueItemColor;
            }
            else
            {
                child.Find("CountText").gameObject.SetActive(true);
                child.GetComponent<Image>().color = Color.white;
            }

            if (tempArray[i]._ItemType == ItemType.HandItem)
                child.Find("WeaponUI").gameObject.SetActive(true);
            else
                child.Find("WeaponUI").gameObject.SetActive(false);

            if (tempArray[i]._ItemType == ItemType.HeadGearItem || tempArray[i]._ItemType == ItemType.BodyGearItem || tempArray[i]._ItemType == ItemType.LegsGearItem)
                child.Find("ArmorLevelText").gameObject.SetActive(true);
            else
                child.Find("ArmorLevelText").gameObject.SetActive(false);
        }

        child.GetComponent<InventoryUISlot>()._Inventory = inventory;
        child.GetComponent<InventoryUISlot>()._IsEquipmentSlot = isEquipments;
        child.GetComponent<InventoryUISlot>()._Index = i;

        child.GetComponent<InventoryUISlot>().OnUpdate();
    }
    private Sprite GetBackgroundIcon(int index)
    {
        switch (index)
        {
            case 0:
                return _HeadGearBackgroundIcon;
            case 1:
                return _BodyGearBackgroundIcon;
            case 2:
                return _LegsGearBackgroundIcon;
            case 3:
            case 4:
                return _HandsBackgroundIcon;
            case 5:
            case 6:
            case 7:
            case 8:
                return _ThrowableBackgroundIcon;
            case 9:
            case 10:
            case 11:
            case 12:
                return _RingBackgroundIcon;
            default:
                Debug.LogError("Item background not found!");
                return null;
        }
    }

    public void Slowtime(float time)
    {
        CoroutineCall(ref _slowTimeCoroutine, SlowTimeCoroutine(time), this);
    }
    private IEnumerator SlowTimeCoroutine(float time)
    {
        SoundManager._Instance.SlowDownAllSound();

        float targetTimeScale = 0.2f;
        float slowInAndOutTime = 0.5f;

        float startTime = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - startTime < slowInAndOutTime)
        {
            Time.timeScale = Mathf.Lerp(Time.timeScale, targetTimeScale, (Time.realtimeSinceStartup - startTime) / slowInAndOutTime);
        }
        Time.timeScale = targetTimeScale;

        yield return new WaitForSecondsRealtime(time);

        startTime = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - startTime < slowInAndOutTime)
        {
            Time.timeScale = Mathf.Lerp(Time.timeScale, 1f, (Time.realtimeSinceStartup - startTime) / slowInAndOutTime);
        }
        Time.timeScale = 1f;

        SoundManager._Instance.UnSlowDownAllSound();
    }
}
