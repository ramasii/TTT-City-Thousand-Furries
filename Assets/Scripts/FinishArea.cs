using UnityEngine;

public class FinishArea : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Jika yang menyentuh gerbang adalah Player
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.WinGame(); // Memicu kondisi Completed
        }
    }
}
