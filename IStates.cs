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
        _human._LocSystemInput.ArrangeOtherInventory();
        _human._LocSystemInput.CheckInventoryActivity();
        _human._LocSystemInput.MoveInput();
        _human._LocSystemInput.CameraInput();
        _human._LocSystemInput.SprintInput();
        _human._LocSystemInput.StrafeInput();
        _human._LocSystemInput.JumpInput();
        _human._LocSystemInput.CameraRotateInput();

        _human._LocSystem.UpdateAnimator();

    }
    void IStates.DoStateFixedUpdate()
    {
        if (_human._ActionState is StaggeredState) return;

        _human._LocSystem.UpdateMotor();
        _human._LocSystem.ControlLocomotionType();
        _human._LocSystem.ControlRotationType();

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
public class DodgingState : MovementStates
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
        //check for dodge attack
    }

    void IStates.Enter<ActionStates>(ActionStates oldState)
    {

    }

    void IStates.Exit<ActionStates>(ActionStates newState)
    {

    }
}
public class UsingUIState : MovementStates
{
    Humanoid IStates.Human => _human;
    private Humanoid _human;
    public UsingUIState(Humanoid human)
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
public class StaggeredState : MovementStates
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


public class UnconsciousState : MovementStates
{
    public Humanoid Human => _human;

    private Humanoid _human;

    private bool _isDead;

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
        if (!(_human._MovementState is LocomotionState)) return;

        //check for attack block dodge jumpattack kick&punch
    }

    void IStates.Enter<ActionStates>(ActionStates oldState)
    {
        
    }

    void IStates.Exit<ActionStates>(ActionStates newState)
    {
        
    }
}

public class AttackingState : ActionStates
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
        if (_human._MovementState is UnconsciousState || _human._MovementState is StaggeredState) _human.ChangeActionState(new NoneActionState(_human));
    }

    void IStates.Enter<ActionStates>(ActionStates oldState)
    {

    }

    void IStates.Exit<ActionStates>(ActionStates newState)
    {

    }
}
public class BlockingState : ActionStates
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
        if (_human._MovementState is UnconsciousState || _human._MovementState is StaggeredState) _human.ChangeActionState(new NoneActionState(_human));

        //check for parry
    }

    void IStates.Enter<ActionStates>(ActionStates oldState)
    {

    }

    void IStates.Exit<ActionStates>(ActionStates newState)
    {

    }
}

public class ThrowState : ActionStates
{
    Humanoid IStates.Human => _human;
    private Humanoid _human;
    public ThrowState(Humanoid human)
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
        if (_human._MovementState is UnconsciousState || _human._MovementState is StaggeredState) _human.ChangeActionState(new NoneActionState(_human));
    }

    void IStates.Enter<ActionStates>(ActionStates oldState)
    {

    }

    void IStates.Exit<ActionStates>(ActionStates newState)
    {

    }
}

