using UnityEngine;
using System.Collections;

public class DangerousArea : MonoBehaviour
{
    [Header("Fall Shake Settings")]
    [Tooltip("Jeda (realtime, tidak terpengaruh timeScale) sebelum UI Failed muncul, supaya getaran kamera sempat terlihat dulu.")]
    [SerializeField] private float shakeDelay = 0.3f;

    private bool hasTriggered;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return; // Cegah trigger dobel selama jeda berjalan

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            Debug.Log("Player has entered the dangerous area!");

            // Efek kamera getar saat player jatuh dari gedung
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.TriggerFallShake();
            }

            // Tunggu sebentar (realtime) supaya getaran sempat terlihat
            // sebelum game di-pause (Time.timeScale = 0) oleh LoseGame()
            StartCoroutine(LoseGameAfterShake());
        }
    }

    private IEnumerator LoseGameAfterShake()
    {
        yield return new WaitForSecondsRealtime(shakeDelay);
        GameManager.Instance.LoseGame(); // Call the LoseGame method from GameManager
    }
}
