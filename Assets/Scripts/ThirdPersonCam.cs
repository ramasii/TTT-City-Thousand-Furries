using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using DG.Tweening;

public class ThirdPersonCam : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform player;
    public Transform playerObj;
    public Rigidbody rb;
    public Transform camTarget;
    public CinemachineCamera cinemachineCamera;
    public float rotationSpeed;
    public PlayerMovement pm;
    float horizontalInput;
    float verticalInput;
    CinemachineRecomposer composer;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        composer = cinemachineCamera.GetComponent<CinemachineRecomposer>();
    }

    private void Update()
    {
        // rotasi orientation
        Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
        orientation.forward = viewDir.normalized;
        camTarget.forward = viewDir.normalized;

        // rotasi playerObj mengikuti orientation pada sumbu Y
        Vector3 inputDir = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (inputDir != Vector3.zero)
        {
            // 1. Hitung target rotasi LookRotation secara normal
            Quaternion targetRotation = Quaternion.LookRotation(inputDir.normalized);

            // 2. JIKA karakter sedang WALLRUN, kunci agar Slerp TIDAK mereset sumbu Z ke angka 0
            if (pm != null && pm.wallRunning)
            {
                // Ambil slerp rotasi ke depan yang baru
                Quaternion nextRotation = Quaternion.Slerp(playerObj.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                
                // Pertahankan rotasi Z (kemiringan dari DOTween) yang saat ini sedang aktif pada objek
                playerObj.rotation = Quaternion.Euler(nextRotation.eulerAngles.x, nextRotation.eulerAngles.y, playerObj.rotation.eulerAngles.z);
            }
            else
            {
                // Jika sedang di tanah/udara biasa, jalankan rotasi penuh seperti biasa
                playerObj.forward = Vector3.Slerp(playerObj.forward, inputDir.normalized, Time.deltaTime * rotationSpeed);
            }
        }
    }

    public void OnMove(InputAction.CallbackContext value)
    {
        Vector2 input = value.ReadValue<Vector2>();
        horizontalInput = input.x;
        verticalInput = input.y;
    }

    public void DoFov(float targetFov)
    {
        // Jaga-jaga agar tidak ada tween FOV lain yang bertabrakan
        DOTween.Kill(cinemachineCamera);

        // DOTween.To memerlukan: Getter, Setter, Target Nilai, dan Durasi
        DOTween.To(
            () => cinemachineCamera.Lens.FieldOfView, // 1. Getter: Mengambil FOV saat ini
            x =>
            {
                // 2. Setter: Karena Lens adalah struct, kita ambil keseluruhannya,
                // ubah nilainya, lalu masukkan kembali ke kamera.
                var lens = cinemachineCamera.Lens;
                lens.FieldOfView = x;
                cinemachineCamera.Lens = lens;
            },
            targetFov, // 3. Target nilai FOV yang diinginkan
            0.5f   // 4. Durasi transisi
        )
        .SetEase(Ease.OutCubic) // Menambahkan kelenturan agar efek zoom terasa dinamis
        .SetId(cinemachineCamera); // Memberi ID agar mudah di-Kill jika dibutuhkan
    }

    public void DoDutch(float targetDutch)
    {
        if (composer == null) return;

        // Amankan agar tween tidak bertabrakan
        DOTween.Kill(composer);

        // Lakukan tween pada properti Dutch milik Recomposer
        DOTween.To(
            () => composer.Dutch,          // Getter: mengambil kemiringan saat ini
            x => composer.Dutch = x,       // Setter: mengubah kemiringan secara real-time
            targetDutch,                     // Target derajat kemiringan
            0.25f                           // Durasi transisi (detik)
        )
        .SetEase(Ease.OutCubic)
        .SetId(composer);
    }
}
