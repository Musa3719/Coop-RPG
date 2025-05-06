using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InputsAndPlayerNetworking : NetworkBehaviour
{
    public NetworkVariable<bool> _IsLoadingScene;
    #region Variables       

    [Header("Controller Input")]
    public string _HorizontalInput = "Horizontal";
    public string _VerticallInput = "Vertical";
    public KeyCode _JumpInput = KeyCode.Space;
    public KeyCode _StrafeInput = KeyCode.Mouse1;
    public KeyCode _SprintInput = KeyCode.LeftShift;
    public KeyCode _LeftLeanInput = KeyCode.Q;
    public KeyCode _RightLeanInput = KeyCode.E;

    [Header("Camera Input")]
    public string _RotateCameraXInput = "Mouse X";
    public string _RotateCameraYInput = "Mouse Y";

    [HideInInspector] public LocomotionSystem _LocomotionSystem;
    [HideInInspector] public NetworkObject _NetworkObject;
    private float _targetHorizontalAxisForCamera;

    #endregion
    private void OnEnable()
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoad;
    }
    private void OnDisable()
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoad;
    }

    public void OnLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        _IsLoadingScene.Value = false;
        InitializeTpCamera();
    }
   
    public override void OnNetworkSpawn()
    {
        GameManager._Instance._Players.Clear();

        foreach (var id in NetworkManager.ConnectedClientsIds)
        {
            foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (player.GetComponent<NetworkObject>().OwnerClientId == id)
                {
                    if (!GameManager._Instance._Players.ContainsKey(player.GetComponent<NetworkObject>().OwnerClientId))
                        GameManager._Instance._Players.Add(player.GetComponent<NetworkObject>().OwnerClientId, player);
                    continue;
                }

            }
        }

        SpawnClientRpc(NetworkObject.OwnerClientId);

        _NetworkObject = GetComponent<NetworkObject>();
        if (!_NetworkObject.IsOwner)
        {
            GetComponent<LocomotionSystem>().enabled = false;
            //this.enabled = false;
            Destroy(GetComponent<Humanoid>());
        }
        else
        {
            InitilizeController();
            InitializeTpCamera();
        }
    }
    public override void OnNetworkDespawn()
    {
        DespawnClientRpc(NetworkObject.OwnerClientId);
    }


    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void SpawnClientRpc(ulong ID)
    {
        if (!GameManager._Instance._Players.ContainsKey(ID))
            GameManager._Instance._Players.Add(ID, GetNetworkObject(ID).gameObject);
    }
    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void DespawnClientRpc(ulong ID)
    {
        if (GameManager._Instance._Players.ContainsKey(ID))
            GameManager._Instance._Players.Remove(ID);
    }

    protected virtual void FixedUpdate()
    {
        /*locomotionSystem.UpdateMotor();               // updates the ThirdPersonMotor methods
        locomotionSystem.ControlLocomotionType();     // handle the controller locomotion type and movespeed
        locomotionSystem.ControlRotationType();       // handle the controller rotation type*/
    }

    protected virtual void Update()
    {
        /*InputHandle();                  // update the input methods
        locomotionSystem.UpdateAnimator();            // updates the Animator Parameters*/
    }

    public virtual void OnAnimatorMove()
    {
        if (IsOwner)
            _LocomotionSystem.ControlAnimatorRootMotion(); // handle root motion animations 
    }

    #region Basic Locomotion Inputs

    protected virtual void InitilizeController()
    {
        _LocomotionSystem = GetComponent<LocomotionSystem>();

        if (_LocomotionSystem != null)
            _LocomotionSystem.Init();
    }

    protected virtual void InitializeTpCamera()
    {
        GameManager._Instance._CinemachineCamera.Follow = transform;
    }

    protected virtual void InputHandle()
    {
        MoveInput();
        CameraInput();
        SprintInput();
        StrafeInput();
        JumpInput();
        LeanInput();
    }
    public virtual void LeanInput()
    {
        if (Input.GetKeyDown(_LeftLeanInput))
        {
            if (Input.GetKey(_StrafeInput))
            {
                //lean animation and combat
            }
            else
            {
                _targetHorizontalAxisForCamera -= 90f;

            }
        }
        else if (Input.GetKeyDown(_RightLeanInput))
        {
            if (Input.GetKey(_StrafeInput))
            {
                //lean animation and combat
            }
            else
            {
                _targetHorizontalAxisForCamera += 90f;
            }
        }

        CinemachineOrbitalFollow orbital = GameManager._Instance._CinemachineCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachineOrbitalFollow;
        orbital.HorizontalAxis.Value = Mathf.Lerp(orbital.HorizontalAxis.Value, _targetHorizontalAxisForCamera, Time.deltaTime * 3f);
    }
    public virtual void MoveInput()
    {
        _LocomotionSystem.input.x = Input.GetAxis(_HorizontalInput);
        _LocomotionSystem.input.z = Input.GetAxis(_VerticallInput);
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
        _LocomotionSystem.Strafe(Input.GetKey(_StrafeInput), Input.mousePosition);
    }

    public virtual void SprintInput()
    {
        if (Input.GetKeyDown(_SprintInput))
            _LocomotionSystem.Sprint(true);
        else if (Input.GetKeyUp(_SprintInput))
            _LocomotionSystem.Sprint(false);
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
        if (Input.GetKeyDown(_JumpInput) && JumpConditions())
            _LocomotionSystem.Jump();
    }

    #endregion
}