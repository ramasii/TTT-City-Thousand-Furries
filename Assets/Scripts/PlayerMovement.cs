using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// This script handles the player's movement using Rigidbody physics. It allows the player to move in four directions (forward, backward, left, right) based on input from the keyboard. The movement is smooth and physics-based, giving a more realistic feel to the player's movement. The Rigidbody's rotation is frozen to prevent unwanted rotations due to physics interactions.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public Transform orientation;
    float desiredMoveSpeed = 5f;
    public float runningSpeed;
    public float wallRunningSpeed;
    public float groundDrag;
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump = true;

    [Header("GroundCheck")]
    public float playerHeight;
    public float checkExtend;
    public LayerMask groundLayer;
    [SerializeField] bool grounded;
    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;
    Rigidbody rb;
    [Header("State")]
    public MovementState state;
    public bool wallRunning;

    [Header("Custom Gravity (Advanced)")]
    [Tooltip("Seberapa berat karakter saat jatuh ke bawah. Rekomendasi: 2 sampai 4")]
    public float fallMultiplier = 2.5f;

    [Header("References")]
    public Transform playerObjOrientation;
    public ParticleSystem dustParticle;
    public ParticleSystem stompParticle;
    bool wasGrounded;
    WallRunning wallRunningScript;

    public enum MovementState
    {
        runnning,
        wallRunning,
        air
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Mencegah karakter berputar akibat interaksi fisika
        wallRunningScript = GetComponent<WallRunning>();
    }

    void Update()
    {
        // cek grounded
        Vector3 rayOrigin = transform.position + Vector3.up * playerHeight;
        grounded = Physics.Raycast(rayOrigin, Vector3.down, playerHeight * 0.5f + checkExtend, groundLayer);

        SpeedControl();
        StateHandler();
        ToggleDust();

        if(grounded && !wasGrounded)
        {
            PlayStompParticle();
        }

        wasGrounded = grounded;

        // tangani drag saat di tanah
        if (grounded)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 rayOrigin = transform.position + Vector3.up * playerHeight;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * (playerHeight * 0.5f + checkExtend));
    }

    void FixedUpdate()
    {
        MovePlayer();
        ApplyCustomGravity();
    }

    public void OnMove(InputAction.CallbackContext value)
    {
        Vector2 input = value.ReadValue<Vector2>();
        horizontalInput = input.x;
        verticalInput = input.y;
    }

    public void OnJump(InputAction.CallbackContext value)
    {
        if (value.started && readyToJump && grounded && state != MovementState.wallRunning)
        {
            readyToJump = false;
            // Jump();
            TryJump(false);
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void StateHandler()
    {
        // mode wall running
        if(wallRunning)
        {
            state = MovementState.wallRunning;
            desiredMoveSpeed = wallRunningSpeed;
        }

        // mode running
        if(grounded)
        {
            state = MovementState.runnning;
            desiredMoveSpeed = runningSpeed;
        }
        else
        {
            state = MovementState.air;
        }
    }

    void ToggleDust()
    {
        bool isMoving = rb.linearVelocity.magnitude > 0.1f;
        if(isMoving && (grounded || wallRunning))
        {
            if(!dustParticle.isPlaying) dustParticle.Play();
        }
        else
        {
            if(dustParticle.isPlaying) dustParticle.Stop();
        }
    }

    void PlayStompParticle()
    {
        if(stompParticle != null)
        {
            stompParticle.Stop();
            stompParticle.Play();
        }
    }

    void ApplyCustomGravity()
    {
        // Cek apakah karakter sedang bergerak ke bawah (jatuh) di udara
        // Unity 6 menggunakan rb.linearVelocity, bukan rb.velocity lagi
        if (!grounded && rb.linearVelocity.y < 0)
        {
            // Tambahkan ekstra gaya ke bawah menggunakan ForceMode.Acceleration 
            // agar mengabaikan berat Mass karakter.
            float extraGravity = (fallMultiplier - 1) * 9.81f;
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }
    }

    void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // gerak di tanah
        if(grounded)
        {
            rb.AddForce(moveDirection.normalized * desiredMoveSpeed * 10f, ForceMode.Force);
        }
        
        // gerak di udara
        else if(!grounded)
        {
            rb.AddForce(moveDirection.normalized * desiredMoveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude > desiredMoveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * desiredMoveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    public void Jump()
    {
        if (wallRunning) return;
        AudioManager.instance.PlayJump();
        // reset y vel
        // rb.linearVelocity = new Vector3(rb.linearVelocity.x*airMultiplier, 0f, rb.linearVelocity.z*airMultiplier);
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

        Debug.Log("Player Jumped!");

        // efek visual saat lompat
        PlayStompParticle();
    }

    // mencegah force lompatan biasa dengan lompatan dinding menyatu
    public void TryJump(bool isWallJump)
    {
        if (isWallJump)
        {
            wallRunningScript.WallJump();
        }
        else
        {
            Jump();
        }
    }

    void ResetJump()
    {
        readyToJump = true;
    }

    public void DoTiltPlayerObj(float tiltAngle)
    {
        Vector3 currentRotation = playerObjOrientation.localEulerAngles;
        playerObjOrientation.DOLocalRotate(new Vector3(0, currentRotation.y, tiltAngle), 0.25f).SetId(playerObjOrientation);
    }
}
