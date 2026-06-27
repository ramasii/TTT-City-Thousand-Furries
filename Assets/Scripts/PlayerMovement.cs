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

    [Header("Animation Settings")]
    public Animator playerAnimator;

    [Header("Elastic Settings")]
    [SerializeField] Transform elasticTarget;
    [SerializeField] private float squishDuration = 0.1f;
    [SerializeField] private float elasticDuration = 0.5f;
    
    [Header("Elastic Fine Tuning")]
    [Tooltip("Semakin tinggi nilainya, pantulannya semakin ekstrem melebihi batas awal.")]
    [SerializeField] private float amplitude = 1.2f; 
    [Tooltip("Semakin kecil nilainya, getaran pegasnya akan semakin cepat/rapat.")]
    [SerializeField] private float period = 0.4f;

    private Sequence _jellySequence;

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

        if (grounded && !wasGrounded)
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
        UpdateAnimator();
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
        if (GetComponent<Player>().isHit) return;
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
        if (GetComponent<Player>().isHit) return;
        // mode wall running
        if (wallRunning)
        {
            state = MovementState.wallRunning;
            desiredMoveSpeed = wallRunningSpeed;
        }

        // mode running
        if (grounded)
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
        if (isMoving && (grounded || wallRunning))
        {
            if (!dustParticle.isPlaying) dustParticle.Play();
        }
        else
        {
            if (dustParticle.isPlaying) dustParticle.Stop();
        }
    }

    void PlayStompParticle()
    {
        if (stompParticle != null)
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
        if (GetComponent<Player>().isHit) return;
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // gerak di tanah
        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * desiredMoveSpeed * 10f, ForceMode.Force);
        }

        // gerak di udara
        else if (!grounded)
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
        playerAnimator.SetTrigger("Jump");
        {
            Debug.Log("Jump animation triggered.");
        }
        ;

        // efek visual saat lompat
        PlayStompParticle();
    }

    // mencegah force lompatan biasa dengan lompatan dinding menyatu
    public void TryJump(bool isWallJump)
    {
        TriggerBoinkEffect(); // Efek visual saat lompat
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
    void UpdateAnimator()
    {
        if (playerAnimator == null) return;

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        playerAnimator.SetFloat("Mag", flatVel.magnitude);

        playerAnimator.SetBool("Grounded", grounded);
        playerAnimator.SetBool("WallRunning", wallRunning);

        bool isAir = !grounded && !wallRunning;
        playerAnimator.SetBool("MidAir", isAir);
    }

    public void TriggerBoinkEffect()
    {
        // Kerapian kode: Selalu matikan tween yang sedang berjalan sebelum memulai yang baru 
        // agar tidak terjadi tumpang tindih (bug visual) jika tombol/mekanik dipicu berkali-kali.
        if (_jellySequence != null && _jellySequence.IsActive())
        {
            _jellySequence.Kill();
        }

        // Kembalikan ke skala awal (Vector3.one) secara instan sebelum animasi dimulai
        elasticTarget.localScale = Vector3.one;

        // Buat sequence baru
        _jellySequence = DOTween.Sequence();

        // FASE 1: Menekan ke bawah (Squish)
        // Y mengecil ke 0.5, X dan Z melebar ke 1.3 secara cepat dengan Ease.OutQuad
        Vector3 squishScale = new Vector3(1.3f, 0.5f, 1.3f);
        _jellySequence.Append(elasticTarget.DOScale(squishScale, squishDuration).SetEase(Ease.OutQuad));

        // FASE 2: Membal balik ke ukuran semula (Stretch & Bounce)
        // Di sinilah keajaiban Ease.OutElastic bekerja dengan parameter tambahan (amplitude & period)
        _jellySequence.Append(
            elasticTarget.DOScale(Vector3.one, elasticDuration)
            .SetEase(Ease.OutElastic, amplitude, period)
        );
    }

    private void OnDestroy()
    {
        // Pastikan memory leak aman saat objek dihancurkan
        if (_jellySequence != null) _jellySequence.Kill();
    }
}
