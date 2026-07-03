using UnityEngine;
using UnityEngine.InputSystem;

public class WallRunning : MonoBehaviour
{
    [Header("Wall Running")]
    public LayerMask wallLayer;
    public LayerMask groundLayerMask;
    public float wallRunForce;
    public float wallJumpUpForce;
    public float wallJumpSideForce;
    public float maxWallRunEnergy;
    public float wallClimbSpeed;
    float wallRunEnergy;

    public float CurrentEnergy => wallRunEnergy;
    public float MaxEnergy => maxWallRunEnergy;

    [Header("Input")]
    float horizontalInput;
    float verticalInput;
    bool upwardsRunning;
    bool downwardsRunning;
    bool jumpInput;

    [Header("Detection")]
    public float wallCheckDistance;
    public float minJumpHeight;
    public float detectionHeight;
    RaycastHit leftWallHit;
    RaycastHit rightWallHit;
    RaycastHit frontWallHit;
    bool wallLeft;
    bool wallRight;
    bool wallFront;
    Vector3 lastNormal; // untuk menyimpan normal dinding terakhir yang disentuh
    Transform lastWall; // untuk menyimpan transform dinding terakhir yang disentuh

    [Header("References")]
    public Transform orientation;
    public Animator playerAnimator;
    PlayerMovement pm;
    Rigidbody rb;
    public ThirdPersonCam thirdPersonCam;

    [Header("UI")]
    [Tooltip("Bubble peringatan yang muncul di dekat Energy Bar saat wallRunEnergy hampir habis.")]
    public EnergyWarningBubble energyWarningBubble;
    [Tooltip("Energy Bar hanya akan aktif/terlihat selama player sedang wall-running (tanpa efek boink, langsung show/hide).")]
    public GameObject energyBarParent;

    [Header("Exiting")]
    bool exitingWall;
    public float exitWallTime;
    float exitWallTimer;

    [Header("Gravity")]
    public bool useGravity;
    public float gravityCounterForce;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();

        wallRunEnergy = maxWallRunEnergy;

        // Pastikan Energy Bar tersembunyi di awal karena player belum wall-running
        if (energyBarParent != null)
        {
            energyBarParent.SetActive(false);
        }
    }

    void Update()
    {
        CheckForWall();
        StateMachine();

        if (!AboveGround())
        {
            lastNormal = Vector3.zero; // reset normal dinding terakhir jika menyentuh tanah
            lastWall = null; // reset transform dinding terakhir jika menyentuh tanah
            wallRunEnergy = maxWallRunEnergy;
        }

        // Energy Bar hanya muncul selama player sedang wall-running, tanpa efek apapun
        if (energyBarParent != null && energyBarParent.activeSelf != pm.wallRunning)
        {
            // Kalau Energy Bar mau dimatikan, paksa sembunyikan & reset Energy Warning Bubble juga.
            // Tanpa ini, bubble yang lagi tampil/animasi ikut ke-nonaktifkan paksa oleh Unity (karena
            // dia child dari Energy Bar Parent) tanpa sempat reset state -> nyangkut dan muncul lagi
            // tiba-tiba saat Energy Bar diaktifkan ulang.
            if (!pm.wallRunning && energyWarningBubble != null)
            {
                energyWarningBubble.ForceHide();
            }

            energyBarParent.SetActive(pm.wallRunning);
        }

        // Update UI peringatan energy HANYA selama player wall-running (Energy Bar sedang aktif).
        // Kalau tidak di-guard dengan pm.wallRunning, SetEnergyPercent() masih bisa terpanggil saat
        // Energy Bar Parent (dan bubble di dalamnya) nonaktif -> ShowWarning() coba StartCoroutine
        // di GameObject yang activeInHierarchy-nya masih false (karena parent-nya mati) -> error.
        if (pm.wallRunning && energyWarningBubble != null && maxWallRunEnergy > 0f)
        {
            energyWarningBubble.SetEnergyPercent(wallRunEnergy / maxWallRunEnergy);
        }
    }

    void FixedUpdate()
    {
        // Cek pm.wallRunning DAN exitingWall agar tidak ada force wall-running tambahan
        // yang "menyelip" di antara WallJump() dipanggil dan StopWallRun() dipanggil
        // pada Update() berikutnya. Tanpa cek exitingWall ini, FixedUpdate bisa sempat
        // menjalankan WallRunningMovement() sekali lagi setelah impulse wall jump
        // sudah diterapkan, sehingga menambahkan gaya ekstra (termasuk menimpa
        // kecepatan Y dengan wallClimbSpeed jika upwardsRunning aktif) di atas hasil
        // wall jump -> inilah penyebab lompatan kadang jadi tiba-tiba lebih tinggi.
        if (pm.wallRunning && !exitingWall)
        {
            WallRunningMovement();
        }
    }

    void CheckForWall()
    {
        Vector3 rayOffset = orientation.up * detectionHeight;
        Vector3 rayOrigin = transform.position + rayOffset;
        wallRight = Physics.Raycast(rayOrigin, orientation.right + rayOffset, out rightWallHit, wallCheckDistance, wallLayer);
        wallLeft = Physics.Raycast(rayOrigin, -orientation.right + rayOffset, out leftWallHit, wallCheckDistance, wallLayer);
        wallFront = Physics.Raycast(rayOrigin, (orientation.forward * 0.75f) + rayOffset, out frontWallHit, wallCheckDistance);
    }

    bool AboveGround()
    {
        return !Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, minJumpHeight, groundLayerMask);
    }

    // gizmos raycast aboveground
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 rayOffset = orientation.up * detectionHeight;
        Vector3 rayOrigin = transform.position + rayOffset;
        Gizmos.DrawRay(rayOrigin, orientation.right * wallCheckDistance);
        Gizmos.DrawRay(rayOrigin, -orientation.right * wallCheckDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, Vector3.down * minJumpHeight);
    }

    void StateMachine()
    {
        // ambil normal dinding yang sedang disentuh
        Vector3 currentNormal = Vector3.zero;
        Transform currentWall = null;
        if (wallRight)
        {
            currentNormal = rightWallHit.normal;
            currentWall = rightWallHit.transform;
        }
        else if (wallLeft)
        {
            currentNormal = leftWallHit.normal;
            currentWall = leftWallHit.transform;
        }

        bool isNewWall = currentWall != null && currentWall != lastWall;

        // state 1 -  wall running
        if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround() && !exitingWall)
        {
            if (!pm.wallRunning && isNewWall) StartWallRun();

            if (pm.wallRunning)
            {
                // Terus perbarui memori dinding selama karakter masih menempel. 
                // Ini mencegah bug jika karakter meluncur ke objek dinding lain di tengah wallrun.
                lastNormal = currentNormal;
                lastWall = currentWall;

                // wallrun timer
                if (wallRunEnergy > 0) wallRunEnergy -= Time.deltaTime;

                if (wallRunEnergy <= 0 && pm.wallRunning)
                {
                    exitingWall = true;
                    exitWallTimer = exitWallTime;
                }

                // wall jump
                if (jumpInput && !exitingWall) pm.TryJump(true);
            }

        }

        // state 2 - exiting wall
        else if (exitingWall)
        {
            if (pm.wallRunning) StopWallRun();

            if (exitWallTimer > 0)
            {
                exitWallTimer -= Time.deltaTime;
            }

            if (exitWallTimer <= 0)
            {
                exitingWall = false;
                // exitWallTimer = exitWallTime;
            }
        }

        // state 3 - not wall running
        else
        {
            if (pm.wallRunning) StopWallRun();
        }
    }

    void StartWallRun()
    {
        playerAnimator.SetBool("WallRunning", true);
        pm.wallRunning = true;
        wallRunEnergy = maxWallRunEnergy;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // terapkan efek kamera
        thirdPersonCam.DoFov(60f);
        if (wallRight) thirdPersonCam.DoDutch(6f);
        else if (wallLeft) thirdPersonCam.DoDutch(-6f);

        // terapkan player obj tilt
        if (wallRight) pm.DoTiltPlayerObj(15f);
        else if (wallLeft) pm.DoTiltPlayerObj(-15f);

        // simpan normal dinding terakhir yang disentuh
        // lastNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
    }

    void WallRunningMovement()
    {
        rb.useGravity = useGravity;

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        // memastikan wallForward mengarah ke arah yang benar
        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
        {
            wallForward = -wallForward;
        }

        // force ke depan
        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        // deteksi apakah karakter tersangkut di sudut antara dua dinding (misalnya, di dalam ceruk)
        bool isStruckInCorner = wallFront || (wallLeft && wallRight);

        // force ke atas/bawah
        if (upwardsRunning && !isStruckInCorner)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, wallClimbSpeed, rb.linearVelocity.z);
        }
        if (downwardsRunning && !isStruckInCorner)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallClimbSpeed, rb.linearVelocity.z);
        }

        // force ke dinding
        if (!(wallLeft && horizontalInput > 0) && !(wallRight && horizontalInput < 0))
        {
            rb.AddForce(-wallNormal * 100, ForceMode.Force);
        }

        // kurangi gravitasi
        if (useGravity)
        {
            rb.AddForce(transform.up * gravityCounterForce, ForceMode.Force);
        }
    }

    void StopWallRun()
    {
        playerAnimator.SetBool("WallRunning", false);
        pm.wallRunning = false;

        // reset efek kamera
        thirdPersonCam.DoFov(50f);
        thirdPersonCam.DoDutch(0f);
        // reset player obj tilt
        pm.DoTiltPlayerObj(0f);
        wallRunEnergy = maxWallRunEnergy;

        // Debug.Log("Stopped wallrunning");
    }

    public void WallJump()
    {
        if(exitingWall) return;
        
        // masuk exiting wall
        exitingWall = true;
        exitWallTimer = exitWallTime;

        // Matikan pm.wallRunning SEGERA (jangan menunggu StateMachine() Update berikutnya).
        // Tanpa baris ini, ada celah satu frame di mana pm.wallRunning masih true,
        // sehingga FixedUpdate() bisa memanggil WallRunningMovement() sekali lagi
        // SETELAH impulse wall jump diterapkan -> menambahkan force tambahan
        // (termasuk menimpa kecepatan Y dengan wallClimbSpeed jika tombol climb
        // sedang ditekan) -> inilah penyebab lompatan kadang tiba-tiba lebih tinggi.
        pm.wallRunning = false;
        playerAnimator.SetBool("WallRunning", false);
        thirdPersonCam.DoFov(50f);
        thirdPersonCam.DoDutch(0f);
        pm.DoTiltPlayerObj(0f);

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        wallNormal.y = 0f; // Matikan gaya ke atas yang bocor/tidak disengaja
        wallNormal.Normalize(); // Kembalikan panjang vektor ke 1
        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        AudioManager.instance.PlayJump();
        // reset velocity y lalu tambahkan force 
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);

        Debug.Log("Wall Jumped!");
        playerAnimator.SetTrigger("Jump");

        // reset jump input
        jumpInput = false;
    }

    // ambil input
    public void OnMove(InputAction.CallbackContext value)
    {
        Vector2 input = value.ReadValue<Vector2>();
        horizontalInput = input.x;
        verticalInput = input.y;
    }

    public void OnSprint(InputAction.CallbackContext value)
    {
        if (value.started) upwardsRunning = true;
        else if (value.canceled) upwardsRunning = false;
    }

    public void OnCrouch(InputAction.CallbackContext value)
    {
        if (value.started) downwardsRunning = true;
        else if (value.canceled) downwardsRunning = false;
    }

    public void OnJump(InputAction.CallbackContext value)
    {
        if (pm.wallRunning)
        {
            if (value.started) jumpInput = true;
        }
    }
}
