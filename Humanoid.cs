using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(LocomotionSystem))]
public class Humanoid : MonoBehaviour
{
   public Rigidbody _Rb { get; private set; }
   public Animator _Animator { get; private set; }
   public NetworkObject _NetworkObjComponent { get; private set; }
   public LocomotionSystem _LocSystem { get; private set; }
   public PlayerInputHandler _LocSystemInput { get; private set; }

   public MovementStates _MovementState { get; private set; }
   public ActionStates _ActionState { get; private set; }

    public Inventory _Inventory { get; set; }

    private void Awake()
    {
        _Rb = GetComponent<Rigidbody>();
        _Animator = GetComponent<Animator>();
        _NetworkObjComponent = GetComponent<NetworkObject>();
        _LocSystem = GetComponent<LocomotionSystem>();
        _LocSystemInput = GetComponent<PlayerInputHandler>();

        InitStates();

        _Inventory = GetComponent<Inventory>();
    }
    private void InitStates()
    {
        _MovementState = new LocomotionState(this);
        _ActionState = new NoneActionState(this);
    }
    public void ChangeMovementState(MovementStates newState)
    {
        _MovementState.Exit<MovementStates>(newState);
        newState.Enter<MovementStates>(_MovementState);
        _MovementState = newState;
    }
    public void ChangeActionState(ActionStates newState)
    {
        _ActionState.Exit<ActionStates>(newState);
        newState.Enter<ActionStates>(_ActionState);
        _ActionState = newState;
    }
    private void Update()
    {
        if (!_NetworkObjComponent.IsOwner || GameManager._Instance._IsGameStopped || GameManager._Instance._IsGameLoading) return;

        _MovementState.DoStateUpdate();
        _ActionState.DoStateUpdate();
    }
    private void FixedUpdate()
    {
        if (!_NetworkObjComponent.IsOwner || GameManager._Instance._IsGameStopped || GameManager._Instance._IsGameLoading) return;

        _MovementState.DoStateFixedUpdate();
        _ActionState.DoStateFixedUpdate();
    }
    private void LateUpdate()
    {
        if (!_NetworkObjComponent.IsOwner || GameManager._Instance._IsGameStopped || GameManager._Instance._IsGameLoading) return;

        _MovementState.DoStateLateUpdate();
        _ActionState.DoStateLateUpdate();
    }
    public void CopyHumanData(Humanoid anotherHuman)
    {
        //copy from another human
    }
}
