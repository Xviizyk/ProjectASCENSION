using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ModernMovement : MonoBehaviour
{
    [Header("Main Settings")]
    public float MovementSpeed = 10f;
    public float Acceleration = 50f;
    public float Deceleration = 40f;
    [Range(0.1f, 10f)] public float MassOfBody = 1f;
    public int MaxAmountJumps = 2;

    [Header("Physics & Gravity")]
    public float Gravity = 30f;
    public float JumpForce = 15f;
    [Range(0f, 1f)] public float JumpCutMultiplier = 0.5f;
    public float WallSlideSpeed = 2f;
    public float VerticalVelocity;
    public float HorizontalVelocity;

    [Header("Detection Settings")]
    public Transform WallCheckPosition;
    public Transform GroundCheckPosition;
    public float WallCheckDistance = 0.4f;
    public float GroundCheckDistance = 0.2f;
    public string GroundLayerName = "Ground";
    public string WallLayerName = "Wall";

    [Header("Leeway System")]
    public float CoyoteTime = 0.15f;
    public float JumpBufferTime = 0.15f;
    public float WallJumpLockTime = 0.15f;

    [Header("Dash Settings")]
    public float DashDistance = 5f;
    public float DashTime = 0.2f;

    [Header("Input Assets")]
    public InputActionAsset GeneralActions;
    private Collider2D _collider;
    private Vector2 _inputDirection;
    private int _jumpAmount;
    private bool _isDashing;
    private bool _isLeft;
    private bool _onGround;
    private bool _onWall;
    private bool _isJumpCutting;
    private bool _sprintTriggered;

    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private float _wallJumpLockCounter;

    private const float skin = 0.02f;

    private InputAction JumpAction;
    private InputAction SprintAction;
    private InputAction MovementAction;

    private void OnEnable()
    {
        _collider = GetComponent<Collider2D>();
        var map = GeneralActions.FindActionMap("PC");

        JumpAction = map.FindAction("Jump");
        SprintAction = map.FindAction("Sprint");
        MovementAction = map.FindAction("Moving");

        JumpAction.Enable();
        SprintAction.Enable();
        MovementAction.Enable();
    }

    private void OnDisable()
    {
        JumpAction.Disable();
        SprintAction.Disable();
        MovementAction.Disable();
    }

    private void Update()
    {
        _inputDirection = MovementAction.ReadValue<Vector2>();

        if (JumpAction.triggered) _jumpBufferCounter = JumpBufferTime;
        else _jumpBufferCounter -= Time.deltaTime;

        if (JumpAction.WasReleasedThisFrame()) _isJumpCutting = true;
        if (SprintAction.WasPressedThisFrame()) _sprintTriggered = true;

        if (_wallJumpLockCounter > 0) _wallJumpLockCounter -= Time.deltaTime;

        if (_wallJumpLockCounter <= 0)
        {
            if (HorizontalVelocity < -0.1f) _isLeft = true;
            else if (HorizontalVelocity > 0.1f) _isLeft = false;
        }

        FixRotation();
    }

    private void FixedUpdate()
    {
        CheckSurroundings();

        if (_onGround)
        {
            _coyoteTimeCounter = CoyoteTime;
            _jumpAmount = 0;
        }

        else _coyoteTimeCounter -= Time.fixedDeltaTime; 

        HandleInertia();
        HandleGravity();
        HandleJump();
        HandleSprint();
        ApplyMovement();

        _sprintTriggered = false;
    }

    private void HandleInertia()
    {
        if (_isDashing) return;

        float inputX = (_wallJumpLockCounter > 0) ? 0 : _inputDirection.x;

        float targetSpeed = inputX * MovementSpeed;
        if (SprintAction.IsPressed()) targetSpeed *= 1.5f;

        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Acceleration : Deceleration;
        accelRate /= MassOfBody;

        if (!_onGround) accelRate *= 0.5f;

        HorizontalVelocity = Mathf.MoveTowards(HorizontalVelocity, targetSpeed, accelRate * Time.fixedDeltaTime);
    }

    private void HandleGravity()
    {
        if (_isDashing) return;

        if (!_onGround)
        {
            float currentGravity = Gravity * MassOfBody;
            if (VerticalVelocity > 0 && _isJumpCutting) currentGravity *= 2f;
            VerticalVelocity -= currentGravity * Time.fixedDeltaTime;
            if (_onWall && VerticalVelocity < 0 && _inputDirection.x != 0) if (VerticalVelocity < -WallSlideSpeed) VerticalVelocity = -WallSlideSpeed; 
            else if (VerticalVelocity < -25f) VerticalVelocity = -25f;
        }

        else
        {
            _isJumpCutting = false;
            if (VerticalVelocity < 0) VerticalVelocity = -1f;
        }
    }

    private void HandleJump()
    {
        if (_jumpBufferCounter <= 0f) return;

        bool canCoyoteJump = _coyoteTimeCounter > 0f && _jumpAmount == 0;
        bool canDoubleJump = _jumpAmount < MaxAmountJumps && _jumpAmount > 0;

        if (canCoyoteJump || _onGround) ExecuteJump();
        else if (canDoubleJump) ExecuteJump(); 
        else if (_onWall && !_onGround) ExecuteWallJump(); 
    }

    private void ExecuteJump()
    {
        VerticalVelocity = JumpForce / MassOfBody;
        _jumpAmount++;
        _jumpBufferCounter = 0f;
        _coyoteTimeCounter = 0f;
        _isJumpCutting = false;
    }

    private void ExecuteWallJump()
    {
        VerticalVelocity = JumpForce / MassOfBody;
        float pushDir = _isLeft ? 1f : -1f;
        HorizontalVelocity = MovementSpeed * pushDir * 1.5f;

        _wallJumpLockCounter = WallJumpLockTime;
        _jumpAmount = 1;
        _jumpBufferCounter = 0f;
        _isJumpCutting = false;
    }

    private void HandleSprint()
    {
        if (_sprintTriggered && !_isDashing) StartCoroutine(DashHandler()); 
    }

    private IEnumerator DashHandler()
    {
        _isDashing = true;
        float timer = 0;
        Vector2 dashDir = _isLeft ? Vector2.left : Vector2.right;
        float dashSpeed = DashDistance / DashTime;

        while (timer < DashTime)
        {
            timer += Time.deltaTime;
            MoveAlongAxis(dashDir * dashSpeed * Time.deltaTime, true);
            VerticalVelocity = 0;
            yield return null;
        }

        _isDashing = false;
    }

    private void ApplyMovement()
    {
        if (_isDashing) return;

        Vector2 move = new Vector2(HorizontalVelocity, VerticalVelocity) * Time.fixedDeltaTime;

        MoveAlongAxis(new Vector2(move.x, 0), true);
        MoveAlongAxis(new Vector2(0, move.y), false);
    }

    private void MoveAlongAxis(Vector2 amount, bool isXAxis)
    {
        float distance = amount.magnitude;
        if (distance <= 0) return;

        Vector2 dir = amount.normalized;
        LayerMask mask = LayerMask.GetMask(GroundLayerName, WallLayerName);

        RaycastHit2D hit = Physics2D.BoxCast(
            _collider.bounds.center,
            _collider.bounds.size * 0.95f,
            0f,
            dir,
            distance + skin,
            mask
        );

        if (hit)
        {
            float safeDistance = hit.distance - skin;
            transform.Translate(dir * Mathf.Max(0, safeDistance), Space.World);

            if (isXAxis) HorizontalVelocity = 0;
            else VerticalVelocity = 0;
        }

        else transform.Translate(amount, Space.World); 
    }

    private void CheckSurroundings()
    {
        _onGround = Physics2D.Raycast(GroundCheckPosition.position, Vector2.down, GroundCheckDistance, LayerMask.GetMask(GroundLayerName));

        Vector2 wallDir = _isLeft ? Vector2.left : Vector2.right;
        _onWall = Physics2D.Raycast(WallCheckPosition.position, wallDir, WallCheckDistance, LayerMask.GetMask(WallLayerName));
    }

    private void FixRotation()
    {
        transform.eulerAngles = Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (GroundCheckPosition) Gizmos.DrawWireSphere(GroundCheckPosition.position, GroundCheckDistance);
        if (WallCheckPosition) Gizmos.DrawLine(WallCheckPosition.position, WallCheckPosition.position + (Vector3)(_isLeft ? Vector2.left : Vector2.right) * WallCheckDistance);
    }
}






/*


using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


public class ModernMovement : MonoBehaviour
{
    [Header("Main settings")]
    public float MovementSpeed;
    public int MaxAmountJumps;
    public float WallSlideDownSpeed;

    [Header("Check points settings")]
    public Transform WallCheckPosition;
    public Transform GroundCheckPosition;
    public float WallCheckDistance;
    public float GroundCheckDistance;

    [Header("Layers")]
    public string GroundLayerName;
    public string WallLayerName;

    [Header("Player actions")]
    public InputActionAsset GeneralActions;
    [SerializeField] private InputActionMap ComputerPlayerActions;
    [SerializeField] private InputAction JumpAction;
    [SerializeField] private InputAction SprintAction;
    [SerializeField] private InputAction MovementAction;

    [Header("Private states")]
    [SerializeField] private int _jumpAmount;
    [SerializeField] private bool _isDashing;
    [SerializeField] private bool _isLeft;
    [SerializeField] private bool _onGround;
    [SerializeField] private bool _onWall;
    [SerializeField] private bool _isWaitDashing;

    [Header("Gravity system")]
    public float MassOfBody;
    public float VerticalVelocity;
    public float Gravity;
    public float JumpForce;
    private bool _sprintTriggered;

    [Header("Moving system")]
    private const float skin = 0.02f;
    private Collider2D _collider;
    public Vector2 JumpMoving;
    public float MinTimeDashed;
    public float MaxTimeDashed;
    public float DashDistance;
    public float DashTime;
    [SerializeField] private Vector2 _inputDirection;
    [SerializeField] private Vector2 _dashTargetPos;
    [SerializeField] private Vector2 _dashStartPos;
    [SerializeField] private float _groundBuffer;
    [SerializeField] private float _lastTimeDashed;
    [SerializeField] private bool _jumpBuffer;

    private void OnEnable()
    {
        _collider = GetComponent<Collider2D>();

        ComputerPlayerActions = GeneralActions.FindActionMap("PC");
        // MobilePlayerActions = GeneralActions.FindActionMap("Mobile");

        JumpAction = ComputerPlayerActions.FindAction("Jump");
        SprintAction = ComputerPlayerActions.FindAction("Sprint");
        MovementAction = ComputerPlayerActions.FindAction("Moving");

        JumpAction.Enable();
        SprintAction.Enable();
        MovementAction.Enable();
    }

    private void OnDisable()
    {
        JumpAction.Disable();
        SprintAction.Disable();
        MovementAction.Disable();
    }

    private void Update()
    {
        if (JumpAction.triggered) _jumpBuffer = true;
        if (SprintAction.WasPressedThisFrame()) _sprintTriggered = true;

        HandleMovementInput();
    }

    private void FixedUpdate()
    {
        FixRotation();
        CheckSurroundings();
        HandlePhysics();
        HandleJump();
        HandleSprint();
        _sprintTriggered = false;
    }

    private void HandleJump()
    {
        if (!_jumpBuffer) return;
        if (_onWall && !_onGround) TryWallJump();
        else if (_jumpAmount < MaxAmountJumps || _groundBuffer > 0f) TryGroundJump();
        _jumpBuffer = false;
    }

    private void TryGroundJump()
    {
        VerticalVelocity = JumpForce / MassOfBody;
        _jumpAmount++;
    }

    private void TryWallJump()
    {
        VerticalVelocity = JumpForce / MassOfBody;
        float dir = _isLeft ? 1f : -1f;
        Vector2 jumpOffset = new Vector2(JumpMoving.x * dir, JumpMoving.y);
        MoveAlongAxis(jumpOffset);
        _jumpAmount = 1;
        _groundBuffer = 0f;
    }

    private void HandleSprint()
    {
        if (_isWaitDashing) _lastTimeDashed += Time.fixedDeltaTime;

        // Используем флаг из Update
        if (_sprintTriggered)
        {
            if (_isWaitDashing && _lastTimeDashed >= MinTimeDashed && _lastTimeDashed <= MaxTimeDashed)
            {
                if (!_isDashing) StartCoroutine(DashHandler());
                _isWaitDashing = false;
                _lastTimeDashed = 0;
            }
            else
            {
                _isWaitDashing = true;
                _lastTimeDashed = 0;
            }
        }

        if (_lastTimeDashed > MaxTimeDashed) _isWaitDashing = false;
    }

    private void HandleMovementInput()
    {
        _inputDirection = MovementAction.ReadValue<Vector2>();
        if (_inputDirection.x < 0) _isLeft = true;
        else if (_inputDirection.x > 0) _isLeft = false;
    }

    private void FixRotation()
    {
        Vector3 rotate = transform.eulerAngles;
        rotate.z = 0f;
        transform.eulerAngles = rotate;
    }

    private IEnumerator DashHandler()
    {
        _isDashing = true;
        float _time = 0f;
        Vector2 dashDir = _isLeft ? Vector2.left : Vector2.right;
        while (_time < DashTime)
        {
            _time += Time.deltaTime;
            float frameDistance = (DashDistance / DashTime) * Time.deltaTime;
            MoveAlongAxis(dashDir * frameDistance);
            yield return null;
        }
        _isDashing = false;
    }

    private void MoveAlongAxis(Vector2 amount)
    {
        float distance = amount.magnitude;
        Vector2 dir = amount.normalized;
        LayerMask mask = LayerMask.GetMask(GroundLayerName, WallLayerName);
        RaycastHit2D hit = Physics2D.BoxCast(
            _collider.bounds.center,
            _collider.bounds.size * 0.95f,
            0f,
            dir,
            distance + skin,
            mask
        );

        if (hit)
        {
            float safeDistance = hit.distance - skin;
            if (safeDistance < 0) safeDistance = 0;
            transform.Translate(dir * safeDistance, Space.World);
            if (dir.y != 0) VerticalVelocity = 0;
        }

        else transform.Translate(amount, Space.World);
    }

    private void CheckSurroundings()
    {
        _onGround = Physics2D.Raycast(
            GroundCheckPosition.position,
            Vector2.down, GroundCheckDistance,
            LayerMask.GetMask(GroundLayerName)
        );

        Vector2 wallDir = _isLeft ? Vector2.left : Vector2.right;

        _onWall = Physics2D.Raycast(
            WallCheckPosition.position,
            wallDir, WallCheckDistance,
            LayerMask.GetMask(WallLayerName)
        );

        if (_onGround) { _groundBuffer = 0.1f; _jumpAmount = 0; }

        else _groundBuffer = Mathf.Max(_groundBuffer - Time.fixedDeltaTime, 0f);
    }

    private void HandlePhysics()
    {
        if (_isDashing) return;

        if (!_onGround)
        {
            float currentGravity = Gravity * MassOfBody;

            if (VerticalVelocity > -20f)
            {
                if (_onWall && VerticalVelocity < 0 && _inputDirection.x != 0) VerticalVelocity -= currentGravity * 0.3f * Time.fixedDeltaTime;
                else VerticalVelocity -= currentGravity * Time.fixedDeltaTime;
            }
        }

        else VerticalVelocity = -1f;

        float speed = MovementSpeed;
        if (SprintAction.IsPressed()) speed *= 2;
        float finalSpeed = speed / Mathf.Sqrt(MassOfBody);
        Vector2 moveStep = new Vector2(_inputDirection.x * finalSpeed, VerticalVelocity) * Time.fixedDeltaTime;
        if (moveStep.x != 0) MoveAlongAxis(new Vector2(moveStep.x, 0));
        if (moveStep.y != 0) MoveAlongAxis(new Vector2(0, moveStep.y));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 1f);
        Gizmos.DrawLine(WallCheckPosition.transform.position, new Vector3(WallCheckPosition.transform.position.x + WallCheckDistance, WallCheckPosition.transform.position.y, WallCheckPosition.transform.position.z));
        Gizmos.color = new Color(1f, 0f, 0f, 1f);
        Gizmos.DrawWireSphere(GroundCheckPosition.position, GroundCheckDistance);
    }
}


*/