using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Text bubble peringatan yang muncul di dekat Energy Bar ketika energy hampir habis.
/// Panggil SetEnergyPercent(float) dari sistem energy kamu setiap kali energy berubah,
/// atau panggil ShowWarning() langsung kalau mau trigger manual.
/// </summary>
public class EnergyWarningBubble : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Bubble akan muncul otomatis saat energy (0-1) turun di bawah nilai ini.")]
    [Range(0f, 1f)]
    [SerializeField] private float lowEnergyThreshold = 0.2f;
    [Tooltip("Kalau true, peringatan hanya boleh muncul ulang setelah energy naik lagi di atas threshold.")]
    [SerializeField] private bool requireRecoveryBeforeRetrigger = true;

    [Header("Content Settings")]
    [Tooltip("Isi teks yang ditampilkan di bubble.")]
    [TextArea(2, 4)]
    [SerializeField] private string warningText = "Energy hampir habis!";
    [Tooltip("Lama bubble ditampilkan sebelum otomatis hilang (detik).")]
    [SerializeField] private float displayDuration = 3f;

    [Header("Position Settings")]
    [Tooltip("Posisi bubble relatif terhadap Energy Bar (dalam local anchored position).")]
    [SerializeField] private Vector2 offsetFromEnergyBar = new Vector2(0f, 60f);

    [Header("Visual Settings")]
    [SerializeField] private Vector2 bubbleSize = new Vector2(320f, 90f);
    [SerializeField] private Color backgroundColor = new Color(0.6f, 0.05f, 0.05f, 0.9f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float fontSize = 22f;
    [SerializeField] private TMP_FontAsset customFont;

    [Header("Efek Muncul (In)")]
    [SerializeField] private float boinkInDuration = 0.35f;
    [SerializeField] private float boinkInOvershoot = 1.25f;

    [Header("Efek Hilang (Out)")]
    [SerializeField] private float boinkOutDuration = 0.3f;
    [SerializeField] private float boinkOutOvershoot = 1.15f;

    private RectTransform bubbleRect;
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI tmpText;
    private Image backgroundImage;

    private Vector3 baseScale = Vector3.one;
    private bool isShowing = false;
    private bool hasTriggeredBelowThreshold = false;
    private Coroutine activeRoutine;

    void Awake()
    {
        CreateBubbleUI();
    }

    /// <summary>
    /// Panggil ini dari sistem energy kamu, contoh: energyWarningBubble.SetEnergyPercent(currentEnergy / maxEnergy);
    /// </summary>
    public void SetEnergyPercent(float percent01)
    {
        percent01 = Mathf.Clamp01(percent01);

        if (percent01 < lowEnergyThreshold)
        {
            if (!hasTriggeredBelowThreshold)
            {
                hasTriggeredBelowThreshold = true;
                ShowWarning();
            }
        }
        else if (!requireRecoveryBeforeRetrigger || percent01 >= lowEnergyThreshold)
        {
            hasTriggeredBelowThreshold = false;
        }
    }

    /// <summary>
    /// Trigger manual, bisa dipanggil dari UnityEvent/Inspector/kode lain.
    /// </summary>
    public void ShowWarning()
    {
        ShowWarning(warningText);
    }

    /// <summary>
    /// Trigger manual dengan teks kustom (menimpa sementara, tidak mengubah warningText default).
    /// </summary>
    public void ShowWarning(string customText)
    {
        // GameObject harus aktif DULU sebelum StartCoroutine dipanggil,
        // karena coroutine tidak bisa dijalankan pada GameObject yang inactive.
        gameObject.SetActive(true);

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }
        activeRoutine = StartCoroutine(ShowWarningRoutine(customText));
    }

    /// <summary>
    /// Paksa sembunyikan bubble seketika dan reset seluruh state-nya.
    /// Dipanggil dari luar (misalnya saat Energy Bar induk dimatikan) supaya bubble
    /// tidak "nyangkut" dalam kondisi setengah animasi lalu muncul lagi tiba-tiba
    /// saat parent-nya diaktifkan kembali.
    /// </summary>
    public void ForceHide()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        isShowing = false;
        hasTriggeredBelowThreshold = false;

        if (bubbleRect != null)
        {
            bubbleRect.localScale = Vector3.zero;
        }

        gameObject.SetActive(false);
    }

    private IEnumerator ShowWarningRoutine(string text)
    {
        tmpText.text = text;
        isShowing = true;

        yield return StartCoroutine(BoinkScale(Vector3.zero, baseScale * boinkInOvershoot, baseScale, boinkInDuration));

        yield return new WaitForSeconds(displayDuration);

        Vector3 popScale = baseScale * boinkOutOvershoot;
        yield return StartCoroutine(BoinkScale(baseScale, popScale, popScale, boinkOutDuration * 0.4f));
        yield return StartCoroutine(ShrinkToZero(boinkOutDuration * 0.6f));

        isShowing = false;
        gameObject.SetActive(false);
        activeRoutine = null;
    }

    // Animasi scale dari 'from' -> overshoot 'peak' -> settle di 'settle', pakai ease-out-back
    private IEnumerator BoinkScale(Vector3 from, Vector3 peak, Vector3 settle, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            bubbleRect.localScale = Vector3.LerpUnclamped(from, peak, EaseOutBack(t));
            yield return null;
        }
        bubbleRect.localScale = settle;
    }

    private IEnumerator ShrinkToZero(float duration)
    {
        float elapsed = 0f;
        Vector3 from = bubbleRect.localScale;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            bubbleRect.localScale = Vector3.LerpUnclamped(from, Vector3.zero, t * t);
            yield return null;
        }
        bubbleRect.localScale = Vector3.zero;
    }

    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    private void CreateBubbleUI()
    {
        RectTransform selfRect = GetComponent<RectTransform>();
        if (selfRect == null)
        {
            selfRect = gameObject.AddComponent<RectTransform>();
        }
        bubbleRect = selfRect;

        bubbleRect.sizeDelta = bubbleSize;
        bubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
        bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
        bubbleRect.pivot = new Vector2(0.5f, 0f);
        bubbleRect.anchoredPosition = offsetFromEnergyBar;

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(transform, false);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = backgroundColor;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        GameObject textObj = new GameObject("TextMeshPro");
        textObj.transform.SetParent(transform, false);
        tmpText = textObj.AddComponent<TextMeshProUGUI>();
        if (customFont != null)
        {
            tmpText.font = customFont;
        }
        tmpText.fontSize = fontSize;
        tmpText.color = textColor;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.textWrappingMode = TextWrappingModes.Normal;
        tmpText.text = warningText;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-24f, -16f);

        bubbleRect.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }
}
