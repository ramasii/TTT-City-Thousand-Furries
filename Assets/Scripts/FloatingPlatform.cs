using UnityEngine;
using DG.Tweening; // Pastikan DOTween sudah di-import

/// <summary>
/// Platform yang miring secara fisik saat diinjak (seperti di atas air).
/// Akan kembali rata dengan animasi mulus setelah player pergi.
/// </summary>
public class FloatingPlatform : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float timeSinceLastJumpToReturn = 1f;
    [SerializeField] private float returnDuration = 1.5f; // Durasi animasi kembali lurus

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    
    // Gunakan Quaternion untuk menyimpan rotasi awal agar lebih presisi menghindari Gimbal Lock
    private Quaternion initialRotation; 
    private Vector3 initialPosition;
    private float returnTimer;

    void Start()
    {
        if(rb == null) rb = GetComponent<Rigidbody>();
        
        // Simpan posisi dan rotasi lurus saat game baru mulai
        initialRotation = transform.rotation;
        initialPosition = transform.localPosition;
        
        // Mulai dalam keadaan diam (beku)
        rb.isKinematic = true;
    }

    void Update()
    {
        if (returnTimer > 0)
        {
            returnTimer -= Time.deltaTime;
            if (returnTimer <= 0)
            {
                BringBackToInitialPosition();
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            // 1. Hentikan animasi DOTween jika platform sedang dalam proses kembali lurus
            DOTween.Kill(transform); 
            
            // 2. Batalkan timer (jika player menginjaknya lagi saat timer sedang berjalan)
            returnTimer = 0; 

            // 3. Lepaskan isKinematic agar papan miring mengikuti berat player
            rb.isKinematic = false; 
            
            Debug.Log("Player menginjak platform air!");
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            // Mulai hitung mundur saat player pergi
            returnTimer = timeSinceLastJumpToReturn;
        }
    }

    void BringBackToInitialPosition()
    {
        // 1. Bekukan fisika lagi agar tidak goyang saat dianimasikan
        rb.isKinematic = true;
        
        // 2. Gunakan DOTween untuk memutar kembali papan ke rotasi awalnya
        // SetEase(Ease.OutBack) memberikan efek pantulan kecil di akhir putaran, 
        // sehingga terasa sangat natural seperti benda mengapung di air yang stabil.
        transform.DOLocalRotate(initialRotation.eulerAngles, returnDuration).SetEase(Ease.OutBack);
        
        // (Opsional) Kembalikan posisi awal jaga-jaga jika ada bug benturan yang menggeser posisi
        // transform.DOMove(initialPosition, returnDuration).SetEase(Ease.OutCubic);
        transform.DOLocalMove(initialPosition, returnDuration).SetEase(Ease.OutCubic);
    }
}