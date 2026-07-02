using UnityEngine;
using System;
using DG.Tweening;

public class UIPanelAnimator : MonoBehaviour
{
    public enum AnimationStyle { PopIn, SlideUp, SlideDown }

    [Header("Layer References")]
    [SerializeField] private CanvasGroup darkOverlay;   // Khusus untuk background hitam transparan
    [SerializeField] private RectTransform mainPanel;  // Khusus untuk panel utama (Panel Kuning)

    [Header("Main Panel Settings")]
    [SerializeField] private AnimationStyle panelStyle = AnimationStyle.PopIn;
    [SerializeField] private float panelDuration = 0.35f;
    [SerializeField] private Ease enterEase = Ease.OutBack;
    [SerializeField] private Ease exitEase = Ease.InBack;
    [SerializeField] private bool useWonkyRotation = true;

    [Header("Dark Overlay Settings")]
    [SerializeField] private float overlayDuration = 0.2f;
    [SerializeField] private float maxAlpha = 0.6f; // Transparansi maksimal background hitam

    private Vector3 panelOriginalPos;
    private float screenHeight;
    private Sequence currentSequence;

    private void Awake()
    {
        if (mainPanel != null) panelOriginalPos = mainPanel.localPosition;
        screenHeight = Screen.height;
    }

    private void OnEnable()
    {
        // Matikan sequence yang sedang berjalan jika ada
        KillCurrentSequence();
        
        // Siapkan kondisi awal (Skala 0 atau posisi di luar layar)
        PrepareInitialState();

        // Buat DOTween Sequence baru
        currentSequence = DOTween.Sequence();

        // 1. Animasi Background Hitam (Fade In)
        if (darkOverlay != null)
        {
            currentSequence.Append(darkOverlay.DOFade(maxAlpha, overlayDuration).SetEase(Ease.Linear));
        }

        // 2. Animasi Panel Kuning (Pop/Bounce) - Berjalan SEBELUM/BERSAMAAN menggunakan Join atau Append
        // Kita gunakan Insert(0.05s) agar panel kuning muncul sedikit terlambat setelah background hitam mulai memudar
        Tween panelTween = null;
        switch (panelStyle)
        {
            case AnimationStyle.PopIn:
                panelTween = mainPanel.DOScale(Vector3.one, panelDuration).SetEase(enterEase);
                break;
            case AnimationStyle.SlideUp:
                panelTween = mainPanel.DOLocalMove(panelOriginalPos, panelDuration).SetEase(enterEase);
                break;
        }

        if (panelTween != null)
        {
            currentSequence.Insert(0.05f, panelTween);
        }

        if (useWonkyRotation)
        {
            currentSequence.Insert(0.05f, mainPanel.DOLocalRotate(Vector3.zero, panelDuration).SetEase(enterEase));
        }

        // Pastikan seluruh rangkaian animasi mengabaikan pause game (Time.timeScale = 0)
        currentSequence.SetUpdate(true);
    }

    public void HidePanel(Action onCompleteCallback = null)
    {
        KillCurrentSequence();
        currentSequence = DOTween.Sequence();

        // Saat keluar, Panel Kuning mengecil duluan
        Tween panelTween = null;
        switch (panelStyle)
        {
            case AnimationStyle.PopIn:
                panelTween = mainPanel.DOScale(Vector3.zero, panelDuration).SetEase(exitEase);
                break;
            case AnimationStyle.SlideUp:
                panelTween = mainPanel.DOLocalMoveY(panelOriginalPos.y - screenHeight, panelDuration).SetEase(exitEase);
                break;
        }

        if (panelTween != null) currentSequence.Append(panelTween);
        if (useWonkyRotation) currentSequence.Join(mainPanel.DOLocalRotate(new Vector3(0, 0, UnityEngine.Random.Range(-5f, 5f)), panelDuration));

        // Setelah panel mengecil, baru Background Hitam menghilang (Fade Out)
        if (darkOverlay != null)
        {
            currentSequence.Append(darkOverlay.DOFade(0f, overlayDuration).SetEase(Ease.Linear));
        }

        currentSequence.SetUpdate(true).OnComplete(() =>
        {
            onCompleteCallback?.Invoke();
            gameObject.SetActive(false);
        });
    }

    private void PrepareInitialState()
    {
        if (darkOverlay != null) darkOverlay.alpha = 0f;

        if (mainPanel != null)
        {
            switch (panelStyle)
            {
                case AnimationStyle.PopIn:
                    mainPanel.localScale = Vector3.zero;
                    break;
                case AnimationStyle.SlideUp:
                    mainPanel.localPosition = new Vector3(panelOriginalPos.x, panelOriginalPos.y - screenHeight, panelOriginalPos.z);
                    break;
            }

            if (useWonkyRotation) mainPanel.localEulerAngles = new Vector3(0, 0, UnityEngine.Random.Range(-8f, 8f));
        }
    }

    private void KillCurrentSequence()
    {
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }
    }
}