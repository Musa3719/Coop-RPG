using UnityEngine;

public class LocomotionSystem : MonoBehaviour
{
    [SerializeField] private Transform _lookAtForCam;
    private Ray _rayForLookPos;
    private float _lastTimeRotated;
    private float _stopAimCounter;
    private float _stopAimCounterMax = 0.3f;
    private Coroutine _rotateAroundCoroutine;

    public virtual void ControlAnimatorRootMotion()
    {
        if (!this.enabled) return;

        if (inputSmooth == Vector3.zero)
        {
            transform.position = animator.rootPosition;
            transform.rotation = animator.rootRotation;
        }

        if (useRootMotion)
            MoveCharacter(moveDirection);
    }

    public virtual void ControlLocomotionType()
    {
        if (lockMovement) return;

        if (locomotionType.Equals(LocomotionType.FreeWithStrafe) && !isStrafing || locomotionType.Equals(LocomotionType.OnlyFree))
        {
            SetControllerMoveSpeed(freeSpeed);
            SetAnimatorMoveSpeed(freeSpeed);
        }
        else if (locomotionType.Equals(LocomotionType.OnlyStrafe) || locomotionType.Equals(LocomotionType.FreeWithStrafe) && isStrafing)
        {
            isStrafing = true;
            SetControllerMoveSpeed(strafeSpeed);
            SetAnimatorMoveSpeed(strafeSpeed);
        }

        if (!useRootMotion)
            MoveCharacter(moveDirection);
    }

    public virtual void ControlRotationType()
    {
        if (lockRotation) return;

        bool validInput = input != Vector3.zero || (isStrafing ? strafeSpeed.rotateWithCamera : freeSpeed.rotateWithCamera);

        if (validInput)
        {
            // calculate input smooth
            inputSmooth = Vector3.Lerp(inputSmooth, input, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);

            Vector3 dir = (isStrafing && (!isSprinting || sprintOnlyFree == false) || (freeSpeed.rotateWithCamera && input == Vector3.zero)) && rotateTarget ? rotateTarget.forward : moveDirection;
            RotateToDirection(dir);
        }
    }

    public virtual void UpdateMoveDirection(Transform referenceTransform = null)
    {
        if (input.magnitude <= 0.01)
        {
            moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);
            return;
        }

        if (referenceTransform && !rotateByWorld)
        {
            //get the right-facing direction of the referenceTransform
            var right = referenceTransform.right;
            right.y = 0;
            //get the forward direction relative to referenceTransform Right
            var forward = Quaternion.AngleAxis(-90, Vector3.up) * right;
            // determine the direction the player will face based on input and the referenceTransform's right and forward directions
            moveDirection = (inputSmooth.x * right) + (inputSmooth.z * forward);
        }
        else
        {
            moveDirection = new Vector3(inputSmooth.x, 0, inputSmooth.z);
        }

    }

    public virtual void Sprint(bool value)
    {
        var sprintConditions = (input.sqrMagnitude > 0.1f && isGrounded && !isStrafing &&
            !(isStrafing && !strafeSpeed.walkByDefault && (horizontalSpeed >= 0.5 || horizontalSpeed <= -0.5 || verticalSpeed <= 0.1f)));

        if (value && sprintConditions)
        {
            if (input.sqrMagnitude > 0.1f)
            {
                if (isGrounded && useContinuousSprint)
                {
                    isSprinting = !isSprinting;
                }
                else if (!isSprinting)
                {
                    isSprinting = true;
                }
            }
            else if (!useContinuousSprint && isSprinting)
            {
                isSprinting = false;
            }
        }
        else if (isSprinting)
        {
            isSprinting = false;
        }
    }

    public virtual void Jump()
    {
        // trigger jump behaviour
        jumpCounter = jumpTimer;
        isJumping = true;

        // trigger jump animations
        if (input.sqrMagnitude < 0.1f)
            animator.CrossFadeInFixedTime("Jump", 0.1f);
        else
            animator.CrossFadeInFixedTime("JumpMove", .2f);
    }

    public virtual void Strafe(bool isAiming, Vector3 mousePosition)
    {

        if (isStrafing && !isAiming && _stopAimCounter > 0)
        {
            _stopAimCounter -= Time.deltaTime;
        }
        else
        {
            isStrafing = isAiming;
            _stopAimCounter = Mathf.Lerp(_stopAimCounter, _stopAimCounterMax, Time.deltaTime * 1.5f);
        }

        if (isStrafing)
        {
            _rayForLookPos = Camera.main.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(_rayForLookPos, out RaycastHit hit, 200f))
            {

                if (_rigidbody.linearVelocity.magnitude > 0.1f)
                    LookAt(_lookAtForCam.transform.position, 8f / _stopAimCounterMax * _stopAimCounter);
                else
                    LookAt(_lookAtForCam.transform.position, 6f / _stopAimCounterMax * _stopAimCounter);
                transform.localEulerAngles = new Vector3(0f, transform.localEulerAngles.y, 0f);

                Vector3 distance = hit.point - transform.position;
                distance.y = 0f;
                Vector3 targetPos = transform.position + Vector3.ClampMagnitude(distance / 3.5f, 3.5f);
                _lookAtForCam.transform.position = Vector3.Lerp(_lookAtForCam.transform.position, targetPos, Time.deltaTime * 1.5f);

                /* Vector2 first = new Vector2(transform.forward.x, transform.forward.z);
                 Vector2 second = new Vector2(targetPos.x - transform.position.x, targetPos.z - transform.position.z);
                 float angle = Vector2.Angle(first, second);
                 bool isRight = Vector2.SignedAngle(first, second) < 0 ? true : false;
                 if (angle > 30f && _rigidbody.linearVelocity.magnitude < 0.1f)
                     Rotate60Degrees(isRight, angle);*/
            }

        }
        else
        {
            _lookAtForCam.transform.position = Vector3.Lerp(_lookAtForCam.transform.position, transform.position + _rigidbody.linearVelocity / 4.5f, Time.deltaTime * 1.5f);
        }
    }
    public void LookAt(Vector3 pos, float lerpSpeed = 8f)
    {
        if (pos == transform.position) return;

        transform.forward = Vector3.Lerp(transform.forward, (pos - transform.position).normalized, Time.deltaTime * lerpSpeed);
        transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
    }
    public void ChangeAnimation(string animName, float lerpTime = 0.2f)
    {
        animator.CrossFade(animName, lerpTime);
    }
    /*public void Rotate60Degrees(bool isRight, float angle)
    {
        if (_lastTimeRotated + 0.5f > Time.time) return;

        _lastTimeRotated = Time.time;
        if (isRight)
        {
            ChangeAnimation("TurnRightLocomotion");
        }
        else
        {
            ChangeAnimation("TurnLeftLocomotion");
        }
        GameManager.Instance.CoroutineCall(ref _rotateAroundCoroutine, RotateCoroutine(isRight, angle), GameManager.Instance);
    }
    private IEnumerator RotateCoroutine(bool isRight, float angle)
    {
        float startTime = Time.time;
        float startY = transform.localEulerAngles.y;
        while (startTime + angle / 250f > Time.time)
        {
            if (_rigidbody.linearVelocity.magnitude > 0.1f)
            {
                ChangeAnimation("Strafing Movement");
                yield break;
            }

            transform.Rotate(isRight ? Vector3.up : -Vector3.up, Time.deltaTime * 60f * angle / 30f, Space.Self);
            yield return null;
        }
    }*/
    #region Variables                

    public const float walkSpeed = 0.5f;
    public const float runningSpeed = 1f;
    public const float sprintSpeed = 1.5f;

    #endregion

    public virtual void UpdateAnimator()
    {
        if (animator == null || !animator.enabled) return;

        animator.SetBool(vAnimatorParameters.IsStrafing, isStrafing); ;
        animator.SetBool(vAnimatorParameters.IsSprinting, isSprinting);
        animator.SetBool(vAnimatorParameters.IsGrounded, isGrounded);
        animator.SetFloat(vAnimatorParameters.GroundDistance, groundDistance);

        if (isStrafing)
        {
            animator.SetFloat(vAnimatorParameters.InputHorizontal, stopMove ? 0 : horizontalSpeed, strafeSpeed.animationSmooth, Time.deltaTime);
            animator.SetFloat(vAnimatorParameters.InputVertical, stopMove ? 0 : verticalSpeed, strafeSpeed.animationSmooth, Time.deltaTime);
        }
        else
        {
            animator.SetFloat(vAnimatorParameters.InputVertical, stopMove ? 0 : verticalSpeed, freeSpeed.animationSmooth, Time.deltaTime);
        }

        animator.SetFloat(vAnimatorParameters.InputMagnitude, stopMove ? 0f : inputMagnitude, isStrafing ? strafeSpeed.animationSmooth : freeSpeed.animationSmooth, Time.deltaTime);
    }

    public virtual void SetAnimatorMoveSpeed(vMovementSpeed speed)
    {
        Vector3 relativeInput = transform.InverseTransformDirection(moveDirection);
        verticalSpeed = relativeInput.z;
        horizontalSpeed = relativeInput.x;

        var newInput = new Vector2(verticalSpeed, horizontalSpeed);

        if (speed.walkByDefault)
            inputMagnitude = Mathf.Clamp(newInput.magnitude, 0, isSprinting ? runningSpeed : walkSpeed);
        else
            inputMagnitude = Mathf.Clamp(isSprinting ? newInput.magnitude + 0.5f : newInput.magnitude, 0, isSprinting ? sprintSpeed : runningSpeed);
    }

    #region Inspector Variables

    [Header("- Movement")]

    [Tooltip("Turn off if you have 'in place' animations and use this values above to move the character, or use with root motion as extra speed")]
    public bool useRootMotion = false;
    [Tooltip("Use this to rotate the character using the World axis, or false to use the camera axis - CHECK for Isometric Camera")]
    public bool rotateByWorld = false;
    [Tooltip("Check This to use sprint on press button to your Character run until the stamina finish or movement stops\nIf uncheck your Character will sprint as long as the SprintInput is pressed or the stamina finishes")]
    public bool useContinuousSprint = true;
    [Tooltip("Check this to sprint always in free movement")]
    public bool sprintOnlyFree = true;
    public enum LocomotionType
    {
        FreeWithStrafe,
        OnlyStrafe,
        OnlyFree,
    }
    public LocomotionType locomotionType = LocomotionType.FreeWithStrafe;

    public vMovementSpeed freeSpeed, strafeSpeed;

    [Header("- Airborne")]

    [Tooltip("Use the currently Rigidbody Velocity to influence on the Jump Distance")]
    public bool jumpWithRigidbodyForce = false;
    [Tooltip("Rotate or not while airborne")]
    public bool jumpAndRotate = true;
    [Tooltip("How much time the character will be jumping")]
    public float jumpTimer = 0.3f;
    [Tooltip("Add Extra jump height, if you want to jump only with Root Motion leave the value with 0.")]
    public float jumpHeight = 4f;

    [Tooltip("Speed that the character will move while airborne")]
    public float airSpeed = 5f;
    [Tooltip("Smoothness of the direction while airborne")]
    public float airSmooth = 6f;
    [Tooltip("Apply extra gravity when the character is not grounded")]
    public float extraGravity = -10f;
    [HideInInspector]
    public float limitFallVelocity = -15f;

    [Header("- Ground")]
    [Tooltip("Layers that the character can walk on")]
    public LayerMask groundLayer = 1 << 0;
    [Tooltip("Distance to became not grounded")]
    public float groundMinDistance = 0.25f;
    public float groundMaxDistance = 0.5f;
    [Tooltip("Max angle to walk")]
    [Range(30, 80)] public float slopeLimit = 75f;
    #endregion

    #region Components

    internal Animator animator;
    internal Rigidbody _rigidbody;                                                      // access the Rigidbody component
    internal PhysicsMaterial frictionPhysics, maxFrictionPhysics, slippyPhysics;         // create PhysicMaterial for the Rigidbody
    internal CapsuleCollider _capsuleCollider;                                          // access CapsuleCollider information

    #endregion

    #region Internal Variables

    // movement bools
    internal bool isJumping;
    internal bool isStrafing
    {
        get
        {
            return _isStrafing;
        }
        set
        {
            _isStrafing = value;
        }
    }
    internal bool isGrounded { get; set; }
    internal bool isSprinting { get; set; }
    public bool stopMove { get; protected set; }

    internal float inputMagnitude;                      // sets the inputMagnitude to update the animations in the animator controller
    internal float verticalSpeed;                       // set the verticalSpeed based on the verticalInput
    internal float horizontalSpeed;                     // set the horizontalSpeed based on the horizontalInput       
    internal float moveSpeed;                           // set the current moveSpeed for the MoveCharacter method
    internal float verticalVelocity;                    // set the vertical velocity of the rigidbody
    internal float colliderRadius, colliderHeight;      // storage capsule collider extra information        
    internal float heightReached;                       // max height that character reached in air;
    internal float jumpCounter;                         // used to count the routine to reset the jump
    internal float groundDistance;                      // used to know the distance from the ground
    internal RaycastHit groundHit;                      // raycast to hit the ground 
    internal bool lockMovement = false;                 // lock the movement of the controller (not the animation)
    internal bool lockRotation = false;                 // lock the rotation of the controller (not the animation)        
    internal bool _isStrafing;                          // internally used to set the strafe movement                
    internal Transform rotateTarget;                    // used as a generic reference for the camera.transform
    internal Vector3 input;                             // generate raw input for the controller
    internal Vector3 colliderCenter;                    // storage the center of the capsule collider info                
    internal Vector3 inputSmooth;                       // generate smooth input based on the inputSmooth value       
    internal Vector3 moveDirection;                     // used to know the direction you're moving 

    #endregion

    public void Init()
    {
        animator = GetComponent<Animator>();
        animator.updateMode = AnimatorUpdateMode.Fixed;
        animator.animatePhysics = true;

        // slides the character through walls and edges
        frictionPhysics = new PhysicsMaterial();
        frictionPhysics.name = "frictionPhysics";
        frictionPhysics.staticFriction = .25f;
        frictionPhysics.dynamicFriction = .25f;
        frictionPhysics.frictionCombine = PhysicsMaterialCombine.Multiply;

        // prevents the collider from slipping on ramps
        maxFrictionPhysics = new PhysicsMaterial();
        maxFrictionPhysics.name = "maxFrictionPhysics";
        maxFrictionPhysics.staticFriction = 1f;
        maxFrictionPhysics.dynamicFriction = 1f;
        maxFrictionPhysics.frictionCombine = PhysicsMaterialCombine.Maximum;

        // air physics 
        slippyPhysics = new PhysicsMaterial();
        slippyPhysics.name = "slippyPhysics";
        slippyPhysics.staticFriction = 0f;
        slippyPhysics.dynamicFriction = 0f;
        slippyPhysics.frictionCombine = PhysicsMaterialCombine.Minimum;

        // rigidbody info
        _rigidbody = GetComponent<Rigidbody>();

        // capsule collider info
        _capsuleCollider = GetComponent<CapsuleCollider>();

        // save your collider preferences 
        colliderCenter = GetComponent<CapsuleCollider>().center;
        colliderRadius = GetComponent<CapsuleCollider>().radius;
        colliderHeight = GetComponent<CapsuleCollider>().height;

        isGrounded = true;
    }

    public virtual void UpdateMotor()
    {
        CheckGround();
        CheckSlopeLimit();
        ControlJumpBehaviour();
        AirControl();
    }

    #region Locomotion

    public virtual void SetControllerMoveSpeed(vMovementSpeed speed)
    {
        if (speed.walkByDefault)
            moveSpeed = Mathf.Lerp(moveSpeed, isSprinting ? speed.runningSpeed : speed.walkSpeed, speed.movementSmooth * Time.deltaTime);
        else
            moveSpeed = Mathf.Lerp(moveSpeed, isSprinting ? speed.sprintSpeed : speed.runningSpeed, speed.movementSmooth * Time.deltaTime);
    }

    public virtual void MoveCharacter(Vector3 _direction)
    {
        // calculate input smooth
        inputSmooth = Vector3.Lerp(inputSmooth, input, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);
        if (!isGrounded || isJumping) return;

        _direction.y = 0;
        _direction.x = Mathf.Clamp(_direction.x, -1f, 1f);
        _direction.z = Mathf.Clamp(_direction.z, -1f, 1f);
        // limit the input
        if (_direction.magnitude > 1f)
            _direction.Normalize();

        Vector3 targetPosition = (useRootMotion ? animator.rootPosition : _rigidbody.position) + _direction * (stopMove ? 0 : moveSpeed) * Time.deltaTime;
        Vector3 targetVelocity = (targetPosition - transform.position) / Time.deltaTime;

        bool useVerticalVelocity = true;
        if (useVerticalVelocity) targetVelocity.y = _rigidbody.linearVelocity.y;
        _rigidbody.linearVelocity = targetVelocity;
    }

    public virtual void CheckSlopeLimit()
    {
        if (input.sqrMagnitude < 0.1) return;

        RaycastHit hitinfo;
        var hitAngle = 0f;

        if (Physics.Linecast(transform.position + Vector3.up * (_capsuleCollider.height * 0.5f), transform.position + moveDirection.normalized * (_capsuleCollider.radius + 0.2f), out hitinfo, groundLayer))
        {
            hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);

            var targetPoint = hitinfo.point + moveDirection.normalized * _capsuleCollider.radius;
            if ((hitAngle > slopeLimit) && Physics.Linecast(transform.position + Vector3.up * (_capsuleCollider.height * 0.5f), targetPoint, out hitinfo, groundLayer))
            {
                hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);

                if (hitAngle > slopeLimit && hitAngle < 85f)
                {
                    stopMove = true;
                    return;
                }
            }
        }
        stopMove = false;
    }

    public virtual void RotateToPosition(Vector3 position)
    {
        Vector3 desiredDirection = position - transform.position;
        RotateToDirection(desiredDirection.normalized);
    }

    public virtual void RotateToDirection(Vector3 direction)
    {
        RotateToDirection(direction, isStrafing ? strafeSpeed.rotationSpeed : freeSpeed.rotationSpeed);
    }

    public virtual void RotateToDirection(Vector3 direction, float rotationSpeed)
    {
        if (!jumpAndRotate && !isGrounded) return;
        direction.y = 0f;
        Vector3 desiredForward = Vector3.RotateTowards(transform.forward, direction.normalized, rotationSpeed * Time.deltaTime, .1f);
        Quaternion _newRotation = Quaternion.LookRotation(desiredForward);
        transform.rotation = _newRotation;
    }

    #endregion

    #region Jump Methods

    protected virtual void ControlJumpBehaviour()
    {
        if (!isJumping) return;

        jumpCounter -= Time.deltaTime;
        if (jumpCounter <= 0)
        {
            jumpCounter = 0;
            isJumping = false;
        }
        // apply extra force to the jump height   
        var vel = _rigidbody.linearVelocity;
        vel.y = GetJumpVelocity();
        _rigidbody.linearVelocity = vel;
    }
    private float GetJumpVelocity()
    {
        Vector3 velWithoutY = _rigidbody.linearVelocity;
        velWithoutY.y = 0f;
        return jumpHeight * 0.5f + jumpHeight * Mathf.Clamp(velWithoutY.magnitude, 0f, 6f) / 10f;
    }
    public virtual void AirControl()
    {
        if ((isGrounded && !isJumping)) return;
        if (transform.position.y > heightReached) heightReached = transform.position.y;
        inputSmooth = Vector3.Lerp(inputSmooth, input, airSmooth * Time.deltaTime);

        if (jumpWithRigidbodyForce && !isGrounded)
        {
            _rigidbody.AddForce(moveDirection * airSpeed * Time.deltaTime, ForceMode.VelocityChange);
            return;
        }

        moveDirection.y = 0;
        moveDirection.x = Mathf.Clamp(moveDirection.x, -1f, 1f);
        moveDirection.z = Mathf.Clamp(moveDirection.z, -1f, 1f);

        Vector3 targetPosition = _rigidbody.position + (moveDirection * airSpeed) * Time.deltaTime;
        Vector3 targetVelocity = (targetPosition - transform.position) / Time.deltaTime;

        targetVelocity.y = _rigidbody.linearVelocity.y;
        _rigidbody.linearVelocity = Vector3.Lerp(_rigidbody.linearVelocity, targetVelocity, airSmooth * Time.deltaTime);
    }

    protected virtual bool jumpFwdCondition
    {
        get
        {
            Vector3 p1 = transform.position + _capsuleCollider.center + Vector3.up * -_capsuleCollider.height * 0.5F;
            Vector3 p2 = p1 + Vector3.up * _capsuleCollider.height;
            return Physics.CapsuleCastAll(p1, p2, _capsuleCollider.radius * 0.5f, transform.forward, 0.6f, groundLayer).Length == 0;
        }
    }

    #endregion

    #region Ground Check                

    protected virtual void CheckGround()
    {
        CheckGroundDistance();
        ControlMaterialPhysics();

        if (groundDistance <= groundMinDistance)
        {
            isGrounded = true;
            if (!isJumping && groundDistance > 0.05f)
                _rigidbody.AddForce(transform.up * (extraGravity * 2 * Time.deltaTime), ForceMode.VelocityChange);

            heightReached = transform.position.y;
        }
        else
        {
            if (groundDistance >= groundMaxDistance)
            {
                // set IsGrounded to false 
                isGrounded = false;
                // check vertical velocity
                verticalVelocity = _rigidbody.linearVelocity.y;
                // apply extra gravity when falling
                if (!isJumping)
                {
                    _rigidbody.AddForce(transform.up * extraGravity * Time.deltaTime, ForceMode.VelocityChange);
                }
            }
            else if (!isJumping)
            {
                _rigidbody.AddForce(transform.up * (extraGravity * 2 * Time.deltaTime), ForceMode.VelocityChange);
            }
        }
    }

    protected virtual void ControlMaterialPhysics()
    {
        // change the physics material to very slip when not grounded
        _capsuleCollider.material = (isGrounded && GroundAngle() <= slopeLimit + 1) ? frictionPhysics : slippyPhysics;

        if (isGrounded && input == Vector3.zero)
            _capsuleCollider.material = maxFrictionPhysics;
        else if (isGrounded && input != Vector3.zero)
            _capsuleCollider.material = frictionPhysics;
        else
            _capsuleCollider.material = slippyPhysics;
    }

    protected virtual void CheckGroundDistance()
    {
        if (_capsuleCollider != null)
        {
            // radius of the SphereCast
            float radius = _capsuleCollider.radius * 0.9f;
            var dist = 10f;
            // ray for RayCast
            Ray ray2 = new Ray(transform.position + new Vector3(0, colliderHeight / 2, 0), Vector3.down);
            // raycast for check the ground distance
            if (Physics.Raycast(ray2, out groundHit, (colliderHeight / 2) + dist, groundLayer) && !groundHit.collider.isTrigger)
                dist = transform.position.y - groundHit.point.y;
            // sphere cast around the base of the capsule to check the ground distance
            if (dist >= groundMinDistance)
            {
                Vector3 pos = transform.position + Vector3.up * (_capsuleCollider.radius);
                Ray ray = new Ray(pos, -Vector3.up);
                if (Physics.SphereCast(ray, radius, out groundHit, _capsuleCollider.radius + groundMaxDistance, groundLayer) && !groundHit.collider.isTrigger)
                {
                    Physics.Linecast(groundHit.point + (Vector3.up * 0.1f), groundHit.point + Vector3.down * 0.15f, out groundHit, groundLayer);
                    float newDist = transform.position.y - groundHit.point.y;
                    if (dist > newDist) dist = newDist;
                }
            }
            groundDistance = (float)System.Math.Round(dist, 2);
        }
    }

    public virtual float GroundAngle()
    {
        var groundAngle = Vector3.Angle(groundHit.normal, Vector3.up);
        return groundAngle;
    }

    public virtual float GroundAngleFromDirection()
    {
        var dir = isStrafing && input.magnitude > 0 ? (transform.right * input.x + transform.forward * input.z).normalized : transform.forward;
        var movementAngle = Vector3.Angle(dir, groundHit.normal) - 90;
        return movementAngle;
    }

    #endregion

    [System.Serializable]
    public class vMovementSpeed
    {
        [Range(1f, 20f)]
        public float movementSmooth = 6f;
        [Range(0f, 1f)]
        public float animationSmooth = 0.2f;
        [Tooltip("Rotation speed of the character")]
        public float rotationSpeed = 16f;
        [Tooltip("Character will limit the movement to walk instead of running")]
        public bool walkByDefault = false;
        [Tooltip("Rotate with the Camera forward when standing idle")]
        public bool rotateWithCamera = false;
        [Tooltip("Speed to Walk using rigidbody or extra speed if you're using RootMotion")]
        public float walkSpeed = 2f;
        [Tooltip("Speed to Run using rigidbody or extra speed if you're using RootMotion")]
        public float runningSpeed = 4f;
        [Tooltip("Speed to Sprint using rigidbody or extra speed if you're using RootMotion")]
        public float sprintSpeed = 6f;
    }
}

public static partial class vAnimatorParameters
{
    public static int InputHorizontal = Animator.StringToHash("InputHorizontal");
    public static int InputVertical = Animator.StringToHash("InputVertical");
    public static int InputMagnitude = Animator.StringToHash("InputMagnitude");
    public static int IsGrounded = Animator.StringToHash("IsGrounded");
    public static int IsStrafing = Animator.StringToHash("IsStrafing");
    public static int IsSprinting = Animator.StringToHash("IsSprinting");
    public static int GroundDistance = Animator.StringToHash("GroundDistance");
}