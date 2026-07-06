using UnityEngine;
using System.Collections;

public class ObstacleGlove : MonoBehaviour
{
    [Header("Punch Timing")]
    [Tooltip("Waktu tunggu antar tinjuan (detik)")]
    public float punchInterval = 2f;
    
    [Tooltip("Durasi sarung tinju dianggap berbahaya/melesat maju (detik). Sesuaikan dengan animasimu!")]
    public float activePunchDuration = 0.2f;

    [Header("Impact Settings")]
    [Tooltip("Kekuatan dorongan ke depan")]
    public float knockbackForce = 15f;
    [Tooltip("Dorongan ke atas agar lemparan terasa lebih komikal")]
    public float upwardForce = 5f; 

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Collider triggerCollider;

    private Rigidbody body;

    private bool isPunching = false;

    void Start()
    {
        if (animator == null || triggerCollider == null)
        {
            Debug.LogWarning("ObstacleGlove: Animator atau trigger collider belum di-assign.", this);
            enabled = false;
            return;
        }

        body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.useGravity = false;

        triggerCollider.isTrigger = true;
        
        // Mulai siklus tinju
        StartCoroutine(PunchRoutine());
    }

    IEnumerator PunchRoutine()
    {
        while (true)
        {
            // 1. Fase Idle (Tunggu sebelum meninju)
            yield return new WaitForSeconds(punchInterval);

            // 2. Picu animasi dari Animator
            animator.SetTrigger("Punch");
            
            // Nyalakan flag agar tabrakan bisa memberikan force
            isPunching = true;

            // 3. Tunggu selama durasi tangan bergerak maju (berbahaya)
            yield return new WaitForSeconds(activePunchDuration);

            // 4. Matikan flag saat tangan sudah mencapai ujung dan mulai mundur/diam
            isPunching = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"ObstacleGlove: Triggered by {other.name}. isPunching={isPunching}");
        // Hanya beri force jika sarung tinju sedang dalam fase berbahaya (isPunching) dan mengenai Player
        if (isPunching && other.CompareTag("Player"))
        {
            Rigidbody playerRb = other.GetComponent<Rigidbody>();
            
            if (playerRb != null)
            {
                // Nol-kan dulu kecepatan pemain agar pantulannya selalu konsisten
                playerRb.linearVelocity = Vector3.zero;

                // Hitung arah dorongan (transform.forward adalah arah hadap objek sarung tinju)
                Vector3 pushDirection = transform.forward * knockbackForce;
                
                // Tambahkan gaya angkat
                pushDirection.y += upwardForce;

                // Terapkan gaya lemparan
                playerRb.AddForce(pushDirection, ForceMode.Impulse);
            }
        }
    }
}