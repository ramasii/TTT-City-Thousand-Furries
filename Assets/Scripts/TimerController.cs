using UnityEngine;
using System;

public class TimerController : MonoBehaviour
{
    [Header("Pengaturan Waktu")]
    public float timeLimit = 60f; // Misalnya 60 detik untuk sampai
    private float currentTime;

    // Event opsional jika kamu ingin UI Jam Weker bergetar saat waktu update
    public static event Action<float> OnTimeUpdated;

    private void Awake()
    {
        currentTime = timeLimit;
    }

    private void Start()
    {
        // currentTime = timeLimit;
    }

    private void Update()
    {
        // Jangan kurangi waktu kalau game sudah selesai
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        currentTime -= Time.deltaTime;
        OnTimeUpdated?.Invoke(currentTime); // Kirim data waktu ke UI

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            GameManager.Instance.LoseGame(); // Memicu kondisi Failed
        }
    }

    public void ResetTimer()
    {
        currentTime = timeLimit;
        OnTimeUpdated?.Invoke(currentTime); // Update UI saat reset
    }
}