using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PlayerController : MonoBehaviour
{
    [Header("Assignables")]
    [Tooltip("this is a reference to the MainCamera object, not the parent of it.")]
    public Transform PlayerCam;
    [Tooltip("reference to orientation object, needed for moving forward and not up or something.")]
    public Transform Orientation;
    [Tooltip("LayerMask for ground layer, important because otherwise the collision detection wont know what ground is")]
    public LayerMask WhatIsGround;
    private Rigidbody _rb;

    [Header("Rotation and look")]
    public float Sensitivity = 50f;
    private float _xRotation;
    [Tooltip("mouse/look Sensitivity")]
    private float _sensMultiplier = 1.5f;

    [Header("Movement")]
    [Tooltip("additive force amount. every physics update that forward is pressed, this force (multiplied by 1/tickrate) will be added to the player.")]
    public float MoveSpeed = 2500;
    [Tooltip("maximum local velocity before input is cancelled")]
    public float maxSpeed = 10;
    [Tooltip("normal countermovement when not Crouching.")]
    public float counterMovement = 0.175f;
    private float _threshold = 0.01f;
    [Tooltip("the maximum angle the ground can have relative to the players up direction.")]
    public float MaxSlopeAngle = 35f;
    private Vector3 _crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 _playerScale;
    [Tooltip("forward force for when a crouch is started.")]
    public float SlideForce = 400;
    [Tooltip("countermovement when sliding. this doesnt work the same way as normal countermovement.")]
    public float SlideCounterMovement = 0.2f;
    private bool _readyToJump = true;
    private float _jumpCooldown = 0.25f;
    [Tooltip("this determines the jump force but is also applied when jumping off of walls, if you decrease it, you may end up being able to walljump and then get back onto the wall leading to infinite height.")]
    public float JumpForce = 250f; 
    float x, y;
    bool jumping;
    private Vector3 _normalVector = Vector3.up;

    [Header("Wallrunning")]
    private float _actualWallRotation;
    private float _wallRotationVel;
    private Vector3 _wallNormalVector;
    [Tooltip("when wallrunning, an upwards force is constantly applied to negate gravity by about half (at default), increasing this value will lead to more upwards force and decreasing will lead to less upwards force.")]
    public float WallRunGravity = 1;
    [Tooltip("when a wallrun is started, an upwards force is applied, this describes that force.")]
    public float InitialForce = 5f; 
    [Tooltip("float to choose how much force is applied outwards when ending a wallrun. this should always be greater than Jump Force")]
    public float EscapeForce = 300f;
    private float _wallRunRotation;
    [Tooltip("how much you want to rotate the _Camera sideways while wallrunning")]
    public float WallRunRotateAmount = 15f;
    [Tooltip("a bool to check if the player is wallrunning because thats kinda necessary.")]
    public bool IsWallRunning;
    [Tooltip("a bool to determine whether or not to actually allow wallrunning.")]
    public bool UseWallrunning = true;

    [Header("Collisions")]
    [Tooltip("a bool to check if the player is on the ground.")]
    public bool Grounded;
    [Tooltip("a bool to check if the player is currently Crouching.")]
    public bool Crouching;
    private bool _surfing;
    private bool _cancellingGrounded;
    private bool _cancellingSurf;
    private bool _cancellingWall;
    private bool _cancelling;

    [Header("CameraFOV")]
    [SerializeField] private Camera _Cam;
    [SerializeField] private float _Fov;
    [SerializeField] private float _WallRunFov;
    [SerializeField] private float _WallRunFovTime;
    [Header("VFX Speedlines")]
    [SerializeField] private VisualEffect _Speedlines;
    [SerializeField] private float _SpeedToActivateLines;
    private bool _isSpeedLining = false;

    public static PlayerController Instance { get; private set; }

    void Awake()
    {

        Instance = this;

        _rb = GetComponent<Rigidbody>();

        _Speedlines.Stop();
    }

    void Start()
    {
        _playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _readyToJump = true;
        _wallNormalVector = Vector3.up;
    }


    private void FixedUpdate()
    {
        Movement();
    }

    private void Update()
    {
        MyInput();
        Look();

        SpeedLineChecker();
    }

    private void LateUpdate()
    {
        //call the wallrunning Function
        WallRunning();
        WallRunRotate();
    }

    private void WallRunRotate()
    {
        FindWallRunRotation();
        float num = 33f;
        _actualWallRotation = Mathf.SmoothDamp(_actualWallRotation, _wallRunRotation, ref _wallRotationVel, num * Time.deltaTime);
        PlayerCam.localRotation = Quaternion.Euler(PlayerCam.rotation.eulerAngles.x, PlayerCam.rotation.eulerAngles.y, _actualWallRotation);
    }

    /// <summary>
    /// Find user input. Should put this in its own class but im lazy
    /// </summary>
    private void MyInput()
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        jumping = Input.GetButton("Jump");
        Crouching = Input.GetButton("Crouch");

        //Crouching Crouch
        if (Input.GetButtonDown("Crouch"))
            StartCrouch();
        if (Input.GetButtonUp("Crouch"))
            StopCrouch();
    }

    private void StartCrouch()
    {
        transform.localScale = _crouchScale;
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
        if (_rb.velocity.magnitude > 0.2f && Grounded)
        {
            if (Grounded)
            {
                _rb.AddForce(Orientation.transform.forward * SlideForce);
            }
        }
    }

    private void StopCrouch()
    {
        transform.localScale = _playerScale;
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
    }

    private void Movement()
    {
        //Extra gravity
        _rb.AddForce(Vector3.down * Time.deltaTime * 10);

        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        //Counteract sliding and sloppy movement
        CounterMovement(x, y, mag);

        //If holding jump && ready to jump, then jump
        if (_readyToJump && jumping) Jump();

        //Set max speed
        float maxSpeed = this.maxSpeed;

        //If sliding down a ramp, add force down so player stays Grounded and also builds speed
        if (Crouching && Grounded && _readyToJump)
        {
            _rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            return;
        }

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        //Some multipliers
        float multiplier = 1f, multiplierV = 1f;

        // Movement in air
        if (!Grounded)
        {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }

        // Movement while sliding
        if (Grounded && Crouching) multiplierV = 0f;

        //Apply forces to move player
        _rb.AddForce(Orientation.transform.forward * y * MoveSpeed * Time.deltaTime * multiplier * multiplierV);
        _rb.AddForce(Orientation.transform.right * x * MoveSpeed * Time.deltaTime * multiplier);
    }

    private void Jump()
    {
        if ((Grounded || IsWallRunning || _surfing) && _readyToJump)
        {
            Vector3 velocity = _rb.velocity;
            _readyToJump = false;
            _rb.AddForce(Vector2.up * JumpForce * 1.5f);
            _rb.AddForce(_normalVector * JumpForce * 0.5f);
            if (_rb.velocity.y < 0.5f)
            {
                _rb.velocity = new Vector3(velocity.x, 0f, velocity.z);
            }
            else if (_rb.velocity.y > 0f)
            {
                _rb.velocity = new Vector3(velocity.x, velocity.y / 2f, velocity.z);
            }
            if (IsWallRunning)
            {
                _rb.AddForce(_wallNormalVector * JumpForce * 3f);
            }
            Invoke("ResetJump", _jumpCooldown);
            if (IsWallRunning)
            {
                IsWallRunning = false;
            }
        }
    }

    private void ResetJump()
    {
        _readyToJump = true;
    }

    private float desiredX;
    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * Sensitivity * Time.fixedDeltaTime * _sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * Sensitivity * Time.fixedDeltaTime * _sensMultiplier;

        //Find current look rotation
        Vector3 rot = PlayerCam.transform.localRotation.eulerAngles;
        desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        _xRotation -= mouseY;
        float clamp = 89.5f;
        _xRotation = Mathf.Clamp(_xRotation, -clamp, clamp);

        //Perform the rotations
        PlayerCam.transform.localRotation = Quaternion.Euler(_xRotation, desiredX, 0);
        Orientation.transform.localRotation = Quaternion.Euler(0, desiredX, 0);
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!Grounded || jumping) return;

        //Slow down sliding
        if (Crouching)
        {
            _rb.AddForce(MoveSpeed * Time.deltaTime * -_rb.velocity.normalized * SlideCounterMovement);
            return;
        }

        //Counter movement
        if (Mathf.Abs(mag.x) > _threshold && Mathf.Abs(x) < 0.05f || (mag.x < -_threshold && x > 0) || (mag.x > _threshold && x < 0))
        {
            _rb.AddForce(MoveSpeed * Orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Mathf.Abs(mag.y) > _threshold && Mathf.Abs(y) < 0.05f || (mag.y < -_threshold && y > 0) || (mag.y > _threshold && y < 0))
        {
            _rb.AddForce(MoveSpeed * Orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }

        //Limit diagonal running. This will also cause a full stop if sliding fast and un-Crouching, so not optimal.
        if (Mathf.Sqrt((Mathf.Pow(_rb.velocity.x, 2) + Mathf.Pow(_rb.velocity.z, 2))) > maxSpeed)
        {
            float fallspeed = _rb.velocity.y;
            Vector3 n = _rb.velocity.normalized * maxSpeed;
            _rb.velocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    /// </summary>
    /// <returns></returns>
    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = Orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(_rb.velocity.x, _rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = _rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }
    //a lot of math (dont touch)
    private void FindWallRunRotation()
    {

        if (!IsWallRunning)
        {
            _wallRunRotation = 0f;
            return;
        }
        _ = new Vector3(0f, PlayerCam.transform.rotation.y, 0f).normalized;
        new Vector3(0f, 0f, 1f);
        float num = 0f;
        float current = PlayerCam.transform.rotation.eulerAngles.y;
        if (Mathf.Abs(_wallNormalVector.x - 1f) < 0.1f)
        {
            num = 90f;
        }
        else if (Mathf.Abs(_wallNormalVector.x - -1f) < 0.1f)
        {
            num = 270f;
        }
        else if (Mathf.Abs(_wallNormalVector.z - 1f) < 0.1f)
        {
            num = 0f;
        }
        else if (Mathf.Abs(_wallNormalVector.z - -1f) < 0.1f)
        {
            num = 180f;
        }
        num = Vector3.SignedAngle(new Vector3(0f, 0f, 1f), _wallNormalVector, Vector3.up);
        float num2 = Mathf.DeltaAngle(current, num);
        _wallRunRotation = (0f - num2 / 90f) * WallRunRotateAmount;
        if (!UseWallrunning)
        {
            return;
        }
        if ((Mathf.Abs(_wallRunRotation) < 4f && y > 0f && Mathf.Abs(x) < 0.1f) || (Mathf.Abs(_wallRunRotation) > 22f && y < 0f && Mathf.Abs(x) < 0.1f))
        {
            if (!_cancelling)
            {
                _cancelling = true;
                CancelInvoke("CancelWallrun");
                Invoke("CancelWallrun", 0.2f);
            }
        }
        else
        {
            _cancelling = false;
            CancelInvoke("CancelWallrun");
        }
    }

    private bool IsFloor(Vector3 v)
    {
        return Vector3.Angle(Vector3.up, v) < MaxSlopeAngle;
    }

    private bool IsSurf(Vector3 v)
    {
        float num = Vector3.Angle(Vector3.up, v);
        if (num < 89f)
        {
            return num > MaxSlopeAngle;
        }
        return false;
    }

    private bool IsWall(Vector3 v)
    {
        return Mathf.Abs(90f - Vector3.Angle(Vector3.up, v)) < 0.05f;
    }

    private bool IsRoof(Vector3 v)
    {
        return v.y == -1f;
    }

    /// <summary>
    /// Handle ground detection
    /// </summary>
    private void OnCollisionStay(Collision other)
    {
        int layer = other.gameObject.layer;
        if ((int)WhatIsGround != ((int)WhatIsGround | (1 << layer)))
        {
            return;
        }
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            if (IsFloor(normal))
            {
                if (IsWallRunning)
                {
                    IsWallRunning = false;
                }
                Grounded = true;
                _normalVector = normal;
                _cancellingGrounded = false;
                CancelInvoke("StopGrounded");
            }
            if (IsWall(normal) && (layer == (int)WhatIsGround || (int)WhatIsGround == -1 || layer == LayerMask.NameToLayer("Ground") || layer == LayerMask.NameToLayer("ground"))) //seriously what is this
            {
                StartWallRun(normal);
                _cancellingWall = false;
                CancelInvoke("StopWall");
            }
            if (IsSurf(normal))
            {
                _surfing = true;
                _cancellingSurf = false;
                CancelInvoke("StopSurf");
            }
            IsRoof(normal);
        }
        float num = 3f;
        if (!_cancellingGrounded)
        {
            _cancellingGrounded = true;
            Invoke("StopGrounded", Time.deltaTime * num);
        }
        if (!_cancellingWall)
        {
            _cancellingWall = true;
            Invoke("StopWall", Time.deltaTime * num);
        }
        if (!_cancellingSurf)
        {
            _cancellingSurf = true;
            Invoke("StopSurf", Time.deltaTime * num);
        }
    }

    private void StopGrounded()
    {
        Grounded = false;
    }

    private void StopWall()
    {
        IsWallRunning = false;
    }

    private void StopSurf()
    {
        _surfing = false;
    }

    //wallrunning functions
    private void CancelWallrun()
    {
        //for when we want to stop wallrunning
        //Invoke("GetReadyToWallrun", 0.1f); <- this line is causing bug
        _rb.AddForce(_wallNormalVector * EscapeForce);
        //UseWallrunning = false; <- this line is causing "bug" (disabling wall runs for some reason)
    }

    private void StartWallRun(Vector3 normal)
    {
        //cancels all y momentum and then applies an upwards force.
        if (!Grounded && UseWallrunning)
        {
            _wallNormalVector = normal;
            if (!IsWallRunning)
            {
                _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
                _rb.AddForce(Vector3.up * InitialForce, ForceMode.Impulse);
            }
            IsWallRunning = true;
        }
    }

    private void WallRunning()
    {
        //checks if the wallrunning bool is set to true and if it is then applies
        //a force to counter gravity enough to make it feel like wallrunning
        if (IsWallRunning)
        {
            _rb.AddForce(-_wallNormalVector * Time.deltaTime * MoveSpeed);
            _rb.AddForce(Vector3.up * Time.deltaTime * _rb.mass * 40f * WallRunGravity);

            //FOV section
            _Cam.fieldOfView = Mathf.Lerp(_Cam.fieldOfView, _WallRunFov, _WallRunFovTime *Time.deltaTime);
        } else {
            //FOV section
            _Cam.fieldOfView = Mathf.Lerp(_Cam.fieldOfView, _Fov, _WallRunFovTime * Time.deltaTime);
        }
    }

    // Speed Lines VFX
    private void SpeedLineChecker()
    {
        float playerSpeedX = Mathf.Abs(_rb.velocity.x);
        float playerSpeedZ = Mathf.Abs(_rb.velocity.z);
        if(_isSpeedLining == false)
        {
            if(playerSpeedX > _SpeedToActivateLines || playerSpeedZ > _SpeedToActivateLines)
            {
                // ACTIVATE SPEED LINES
                _Speedlines.Play();
                _isSpeedLining = true;
            }
        }
        else {
            // STOP SPEED LINES
            if(playerSpeedX < _SpeedToActivateLines && playerSpeedZ < _SpeedToActivateLines) _Speedlines.Stop();
            _Speedlines.Stop();
            _isSpeedLining = false;
        }
    }
}
