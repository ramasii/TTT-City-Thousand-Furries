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

    [Header("Distance Ruler")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform finishArea;

    [SerializeField] private RectTransform ruler;
    [SerializeField] private RectTransform playerIcon;
    [SerializeField] private UIDistanceAnim distanceAnim;

    private float startDistance;
    private float maxProgress;

    [Header("UI Panels")]
    public GameObject winPanel;
    public GameObject losePanel; // Panel dengan tulisan raksasa "LATE!"
    public TextMeshProUGUI timerText;
    public GameObject pausePanel;
    private bool isPaused;

    [Header("References")]
    [SerializeField] private UIPanelAnimator pausePanelAnimator;
    [SerializeField] private UIPanelAnimator winPanelAnimator;
    [SerializeField] private UIPanelAnimator losePanelAnimator;
    [SerializeField] private UITimerToPanelTransition timerTransition;

    void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        startDistance = Vector3.Distance(player.position, finishArea.position);
        maxProgress = 0f;
    }

    void Update()
    {
        UpdateEnergyUI();
        UpdateDistanceRuler();

        if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
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

    // Dipanggil saat pemain menekan Tombol "RESUME"
    public void ClosePauseMenu()
    {
        // Kita panggil HidePanel(). Skrip modular akan memutar animasi keluar dulu,
        // BARU setelah selesai dia akan otomatis melakukan SetActive(false).
        pausePanelAnimator.HidePanel(onCompleteCallback: () =>
        {
            // Tempatkan kode kelanjutan game di sini (misal: resume physics, hilangkan kursor)
            Time.timeScale = 1f;
            isPaused = false;
            Debug.Log("Game Resumed!");
        });
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

        ClosePauseMenu(); // Panggil fungsi untuk menutup panel dengan animasi

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
        timerTransition.PlayTimerTransition();
        // Boleh tambahkan DOTween animasi disini
    }
    public void CloseWinPanel()
    {
        // Kita panggil HidePanel(). Skrip modular akan memutar animasi keluar dulu,
        // BARU setelah selesai dia akan otomatis melakukan SetActive(false).
        winPanelAnimator.HidePanel(onCompleteCallback: () =>
        {
            // Tempatkan kode kelanjutan game di sini (misal: resume physics, hilangkan kursor)
            Time.timeScale = 1f;
            Debug.Log("Game Resumed!");
        });
    }

    private void ShowLosePanel()
    {
        ShowCursor();
        losePanel.SetActive(true);
        // Boleh tambahkan DOTween shake effect pada teks "LATE!" disini
    }

    public void CloseLosePanel()
    {
        // Kita panggil HidePanel(). Skrip modular akan memutar animasi keluar dulu,
        // BARU setelah selesai dia akan otomatis melakukan SetActive(false).
        losePanelAnimator.HidePanel(onCompleteCallback: () =>
        {
            // Tempatkan kode kelanjutan game di sini (misal: resume physics, hilangkan kursor)
            Time.timeScale = 1f;
            Debug.Log("Game Resumed!");
        });
    }

    private void ShowCursor(bool show = true)
    {
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show ? true : false;
    }

    private void UpdateDistanceRuler()
    {
        if (player == null ||
            finishArea == null ||
            ruler == null ||
            playerIcon == null)
            return;

        float currentDistance = Vector3.Distance(player.position, finishArea.position);

        float progress = 1f - (currentDistance / startDistance);

        progress = Mathf.Clamp01(progress);

        // Simpan progress tertinggi agar icon tidak mundur
        if (progress > maxProgress)
        {
            maxProgress = progress;

            if (distanceAnim != null)
                distanceAnim.TriggerMoveEffect();
        }

        float width = ruler.rect.width;

        playerIcon.anchoredPosition = new Vector2(
            width * maxProgress - width * 0.5f,
            playerIcon.anchoredPosition.y
        );
    }
}
