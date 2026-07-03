using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Singleton pattern agar mudah diakses
    public static GameManager Instance { get; private set; }

    // State Pattern Sederhana
    public enum GameState { Playing, GameWin, GameOver }
    [SerializeField] private GameState currentstate;
    public GameState CurrentState { get { return currentstate; } private set { currentstate = value; } }

    // Alasan kekalahan, dipakai UI untuk menentukan panel mana yang tampil
    public enum LoseReason { TimeOut, FellOff }

    // Observer Pattern: Event yang bisa didengarkan oleh skrip lain
    public static event Action OnGameWin;
    public static event Action<LoseReason> OnGameOver;

    private void Awake()
    {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
        
        Time.timeScale = 1f; // Pastikan waktu berjalan normal di awal
        CurrentState = GameState.Playing;
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
    }

    public void WinGame()
    {
        if (CurrentState != GameState.Playing) return; // Cegah terpanggil 2x

        CurrentState = GameState.GameWin;
        OnGameWin?.Invoke(); // Pancarkan event Menang!
        
        Debug.Log("Completed: Sampai Sekolah!");
        Time.timeScale = 0f; // Hentikan game
    }

    public void LoseGame(LoseReason reason = LoseReason.TimeOut)
    {
        if (CurrentState != GameState.Playing) return; // Cegah terpanggil 2x

        CurrentState = GameState.GameOver;
        OnGameOver?.Invoke(reason); // Pancarkan event Kalah, sertakan alasannya

        Debug.Log(reason == LoseReason.FellOff ? "Failed: Jatuh dari gedung!" : "Failed: Telat masuk sekolah!");
        Time.timeScale = 0f; // Hentikan game
    }
}
