using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class UIScript : MonoBehaviour
{
    [Header("ENERGY UI")]
    public Image energyFill;
    public WallRunning wallRunning;

    [Header("UI Panels")]
    public GameObject winPanel;
    public GameObject losePanel; // Panel dengan tulisan raksasa "LATE!"
    public TextMeshProUGUI timerText;
    public GameObject pausePanel;
    private bool isPaused;

    void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    void Update()
    {
        UpdateEnergyUI();
        if(GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            HandlePauseInput();
        }
    }

    // ================= ENERGY =================
    void UpdateEnergyUI()
    {
        if (wallRunning == null || energyFill == null) return;

        float ratio = wallRunning.CurrentEnergy / wallRunning.MaxEnergy;
        energyFill.fillAmount = Mathf.Lerp(energyFill.fillAmount, ratio, Time.deltaTime * 100f);
    }

    // ================= PAUSE =================

    void HandlePauseInput()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        AudioManager.instance.PlayUIClick();
        if (pausePanel == null) return;

        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        ShowCursor();
    }

    public void Resume()
    {
        AudioManager.instance.PlayUIClick();
        if (pausePanel == null) return;

        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        ShowCursor(false);
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void Restart()
    {
        AudioManager.instance.PlayUIClick();

        // Reset timer saat restart
        TimerController timerController = FindAnyObjectByType<TimerController>();
        if (timerController) timerController.ResetTimer();

        GameManager.Instance.StartGame(); // Reset state di GameManager

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        AudioManager.instance.PlayUIClick();
        // GameManager.Instance.StartGame(); // Reset state di GameManager
        SceneManager.LoadScene("MainMenu");
    }

    private void OnEnable()
    {
        // Mulai "mendengarkan" event dari GameManager dan Timer
        GameManager.OnGameWin += ShowWinPanel;
        GameManager.OnGameOver += ShowLosePanel;
        TimerController.OnTimeUpdated += UpdateTimerUI;
    }

    private void OnDisable()
    {
        // Berhenti mendengarkan saat objek hancur (Mencegah Memory Leak)
        GameManager.OnGameWin -= ShowWinPanel;
        GameManager.OnGameOver -= ShowLosePanel;
        TimerController.OnTimeUpdated -= UpdateTimerUI;
    }

    private void UpdateTimerUI(float timeRemaining)
    {
        timerText.text = Mathf.CeilToInt(timeRemaining).ToString();
    }

    private void ShowWinPanel()
    {
        ShowCursor();
        winPanel.SetActive(true);
        // Boleh tambahkan DOTween animasi disini
    }

    private void ShowLosePanel()
    {
        ShowCursor();
        losePanel.SetActive(true);
        // Boleh tambahkan DOTween shake effect pada teks "LATE!" disini
    }

    private void ShowCursor(bool show = true)
    {
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show;
    }
}
