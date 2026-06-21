using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class UIScript : MonoBehaviour
{
    [Header("ENERGY UI")]
    public Image energyFill;
    public WallRunning wallRunning;

    [Header("Pause Panel")]
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
        HandlePauseInput();
    }

    // ================= ENERGY =================
    void UpdateEnergyUI()
    {
        if (wallRunning == null || energyFill == null) return;

        float ratio = wallRunning.CurrentEnergy / wallRunning.MaxEnergy;
        energyFill.fillAmount = Mathf.Lerp(energyFill.fillAmount, ratio, Time.deltaTime * 10f);
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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        AudioManager.instance.PlayUIClick();
        if (pausePanel == null) return;

        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Restart()
    {
        AudioManager.instance.PlayUIClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        AudioManager.instance.PlayUIClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
