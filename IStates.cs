using UnityEngine;

public interface IStates
{
    public Humanoid Human { get; }
    void Enter<T>(T oldState) where T : IStates;
    void Exit<T>(T newState) where T : IStates;
    void DoStateUpdate();
    void DoStateFixedUpdate();
    void DoStateLateUpdate();
}

public interface MovementStates : IStates
{

}
public class LocomotionState : MovementStates
{
    public enum LocState
    {
        Idle,
        InAir,
        Walking,
        Running,
        Sprinting
    }
    public LocState _LocState;

    Humanoid IStates.Human => _human;
    private Humanoid _human;

    public LocomotionState(Humanoid human)
    {
        this._human = human;
    }

    void IStates.Enter<MovementStates>(MovementStates oldState)
    {
        
    }

    void IStates.Exit<MovementStates>(MovementStates newState)
    {
        
    }


    void IStates.DoStateUpdate()
    {
        if (_human._ActionState is StaggeredState) return;

        //Check For State Change
        /*if (human.CrouchInput)
        {
            human.EnterState(new Crouch(human));
        }*/

        //input handlers
        _human._LocSystemInput.MoveInput();
        _human._LocSystemInput.CameraInput();
        _human._LocSystemInput.SprintInput();
        _human._LocSystemInput.StrafeInput();
        _human._LocSystemInput.JumpInput();
        _human._LocSystemInput.LeanInput();

        _human._LocSystem.UpdateAnimator();


        /*LocomotionSystem.UpdateMoveDirection(human, GameManager.Instance.MainCamera.transform);
        LocomotionSystem.CheckJump(human);
        LocomotionSystem.CheckSprint(human);
        LocomotionSystem.UpdateAnimator(human);*/
    }
    void IStates.DoStateFixedUpdate()
    {
        if (_human._ActionState is StaggeredState) return;

        _human._LocSystem.UpdateMotor();
        _human._LocSystem.ControlLocomotionType();
        _human._LocSystem.ControlRotationType();


        /*LocomotionSystem.CheckGround(human);
        LocomotionSystem.CheckSlopeLimit(human);
        LocomotionSystem.ControlJumpBehaviour(human);
        LocomotionSystem.AirControl(human);
        LocomotionSystem.ControlLocomotionType(human);     // handle the controller locomotion type and movespeed
        LocomotionSystem.ControlRotationType(human);       // handle the controller rotation type*/
    }

    void IStates.DoStateLateUpdate()
    {
        
    }

    public LocState GetLocState()
    {
        if (!_human._LocSystem.isGrounded) return LocState.InAir;
        if (_human._Rb.linearVelocity.magnitude < 0.1f) return LocState.Idle;
        if (_human._LocSystem.isStrafing) return LocState.Walking;
        if (_human._LocSystem.isSprinting) return LocState.Sprinting;
        return LocState.Running;
    }

}

public class UnconsciousState : MovementStates
{
    public Humanoid Human => _human;


    private Humanoid _human;

    public UnconsciousState(Humanoid human)
    {
        this._human = human;
    }
    void IStates.Enter<MovementStates>(MovementStates oldState)
    {
        
    }

    void IStates.Exit<MovementStates>(MovementStates newState)
    {
        
    }

    void IStates.DoStateUpdate()
    {
        //Check For State Change
    }

    void IStates.DoStateFixedUpdate()
    {
        
    }

    void IStates.DoStateLateUpdate()
    {
        
    }
}

public class RestingState : MovementStates
{
    Humanoid IStates.Human => _human;

    private Humanoid _human;
    public RestingState(Humanoid human)
    {
        this._human = human;
    }
    void IStates.Enter<MovementState>(MovementState oldState)
    {

    }

    void IStates.Exit<MovementState>(MovementState newState)
    {

    }

    void IStates.DoStateUpdate()
    {
        
    }

    void IStates.DoStateFixedUpdate()
    {
        
    }

    void IStates.DoStateLateUpdate()
    {
        
    }
}



public interface ActionStates : IStates
{

}
public class NoneActionState : ActionStates
{
    Humanoid IStates.Human => _human;
    private Humanoid _human;
    public NoneActionState(Humanoid human)
    {
        this._human = human;
    }

    void IStates.DoStateFixedUpdate()
    {
        
    }

    void IStates.DoStateLateUpdate()
    {
        
    }

    void IStates.DoStateUpdate()
    {
        
    }

    void IStates.Enter<ActionStates>(ActionStates oldState)
    {
        
    }

    void IStates.Exit<ActionStates>(ActionStates newState)
    {
        
    }
}
public interface FightStates : ActionStates
{

}
public class AttackingState : FightStates
{
    Humanoid IStates.Human => _human;
    private Humanoid _human;
    public AttackingState(Humanoid human)
    {
        this._human = human;
    }

    void IStates.DoStateFixedUpdate()
    {

    }

    void IStates.DoStateLateUpdate()
    {

    }

    void IStates.DoStateUpdate()
    {

    }

    void IStates.Enter<ActionStates>(ActionStates oldState)
    {

    }

    void IStates.Exit<ActionStates>(ActionStates newState)
    {

    }
}
public class BlockingState : FightStates
{
    Humanoid IStates.Human => _human;
    private Humanoid _human;
    public BlockingState(Humanoid human)
    {
        this._human = human;
    }

    void IStates.DoStateFixedUpdate()
    {

    }

    void IStates.DoStateLateUpdate()
    {

    }

    void IStates.DoStateUpdate()
    {

    }

    void IStates.Enter<ActionStates>(ActionStates oldState)
    {

    }

    void IStates.Exit<ActionStates>(ActionStates newState)
    {

    }
}
public class DodgingState : FightStates
{
    Humanoid IStates.Human => _human;
    private Humanoid _human;
    public DodgingState(Humanoid human)
    {
        this._human = human;
    }

    void IStates.DoStateFixedUpdate()
    {

    }

    void IStates.DoStateLateUpdate()
    {

    }

    void IStates.DoStateUpdate()
    {

    }

    void IStates.Enter<ActionStates>(ActionStates oldState)
    {

    }

    void IStates.Exit<ActionStates>(ActionStates newState)
    {

    }
}
public class StaggeredState : FightStates
{
    Humanoid IStates.Human => _human;
    private Humanoid _human;
    public StaggeredState(Humanoid human)
    {
        this._human = human;
    }

    void IStates.DoStateFixedUpdate()
    {

    }

    void IStates.DoStateLateUpdate()
    {

    }

    void IStates.DoStateUpdate()
    {

    }

    void IStates.Enter<ActionStates>(ActionStates oldState)
    {

    }

    void IStates.Exit<ActionStates>(ActionStates newState)
    {

    }
}
public class PreparingThrowableState : FightStates
{
    Humanoid IStates.Human => _human;
    private Humanoid _human;
    public PreparingThrowableState(Humanoid human)
    {
        this._human = human;
    }

    void IStates.DoStateFixedUpdate()
    {

    }

    void IStates.DoStateLateUpdate()
    {

    }

    void IStates.DoStateUpdate()
    {

    }

    void IStates.Enter<ActionStates>(ActionStates oldState)
    {

    }

    void IStates.Exit<ActionStates>(ActionStates newState)
    {

    }
}

public interface OtherActionStates : ActionStates
{

}

public class UsingToolState : OtherActionStates
{
    Humanoid IStates.Human => _human;
    private Humanoid _human;
    public UsingToolState(Humanoid human)
    {
        this._human = human;
    }

    void IStates.DoStateFixedUpdate()
    {

    }

    void IStates.DoStateLateUpdate()
    {

    }

    void IStates.DoStateUpdate()
    {

    }

    void IStates.Enter<ActionStates>(ActionStates oldState)
    {

    }

    void IStates.Exit<ActionStates>(ActionStates newState)
    {

    }
}
public class CraftingState : OtherActionStates
{
    Humanoid IStates.Human => _human;
    private Humanoid _human;
    public CraftingState(Humanoid human)
    {
        this._human = human;
    }

    void IStates.DoStateFixedUpdate()
    {

    }

    void IStates.DoStateLateUpdate()
    {

    }

    void IStates.DoStateUpdate()
    {

    }

    void IStates.Enter<ActionStates>(ActionStates oldState)
    {

    }

    void IStates.Exit<ActionStates>(ActionStates newState)
    {

    }
}
public class BuildingState : OtherActionStates
{
    Humanoid IStates.Human => _human;
    private Humanoid _human;
    public BuildingState(Humanoid human)
    {
        this._human = human;
    }

    void IStates.DoStateFixedUpdate()
    {

    }

    void IStates.DoStateLateUpdate()
    {

    }

    void IStates.DoStateUpdate()
    {

    }

    void IStates.Enter<ActionStates>(ActionStates oldState)
    {

    }

    void IStates.Exit<ActionStates>(ActionStates newState)
    {

    }
}