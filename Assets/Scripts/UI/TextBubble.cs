using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class TextBubble : MonoBehaviour
{
    [Header("Bubble Settings")]
    [SerializeField] private float heightOffset = 2.5f;
    [SerializeField] private float delayBetweenSentences = 4f;

    [Header("Typewriter Settings")]
    [Tooltip("Delay (detik) antar kemunculan huruf saat teks diketik.")]
    [SerializeField] private float letterDelay = 0.05f;

    [Header("Boink Effect Settings")]
    [Tooltip("Durasi animasi boink (detik).")]
    [SerializeField] private float boinkDuration = 0.35f;
    [Tooltip("Seberapa besar overshoot scale saat boink (1 = normal, 1.2 = overshoot 20%).")]
    [SerializeField] private float boinkOvershoot = 1.25f;

    [Header("Visual Settings")]
    [SerializeField] private Vector2 bubbleSize = new Vector2(450f, 160f);
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float fontSize = 26f;
    // Tambahkan slot kustom font di sini
    [SerializeField] private TMP_FontAsset customFont; 

    private GameObject bubbleContainer;
    private Canvas bubbleCanvas;
    private TextMeshProUGUI tmpText;
    private Image backgroundImage;
    private RectTransform bubbleRect;
    private Vector3 baseScale;
    private bool isClosing;

    private string[] sentences = new string[]
    {
        "Oh no, I'm late!",
        "It's 5 minutes left until school gate is closed.",
        "I must look for alternative way."
    };

    void Start()
    {
        CreateBubbleUI();
        StartCoroutine(ShowDialogueRoutine());
    }

    void LateUpdate()
    {
        if (bubbleContainer != null)
        {
            bubbleContainer.transform.position = transform.position + Vector3.up * heightOffset;

            if (Camera.main != null)
            {
                bubbleContainer.transform.LookAt(
                    bubbleContainer.transform.position + Camera.main.transform.rotation * Vector3.forward,
                    Camera.main.transform.rotation * Vector3.up
                );
            }
        }
    }

    private void CreateBubbleUI()
    {
        bubbleContainer = new GameObject("TextBubble_Canvas");
        bubbleContainer.transform.position = transform.position + Vector3.up * heightOffset;

        bubbleCanvas = bubbleContainer.AddComponent<Canvas>();
        bubbleCanvas.renderMode = RenderMode.WorldSpace;
        bubbleContainer.AddComponent<CanvasScaler>();
        bubbleContainer.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = bubbleContainer.GetComponent<RectTransform>();
        canvasRect.sizeDelta = bubbleSize;
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        bubbleRect = canvasRect;
        // Skala dasar (final) bubble, dipakai sebagai acuan animasi boink
        baseScale = new Vector3(0.01f, 0.01f, 0.01f);

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(bubbleContainer.transform, false);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.sprite = CreateRoundedSprite((int)bubbleSize.x, (int)bubbleSize.y, 30, backgroundColor);
        backgroundImage.type = Image.Type.Simple;

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        GameObject textObj = new GameObject("TextMeshPro");
        textObj.transform.SetParent(bubbleContainer.transform, false);
        tmpText = textObj.AddComponent<TextMeshProUGUI>();
        
        // --- PROSES PENERAPAN FONT KUSTOM ---
        if (customFont != null)
        {
            tmpText.font = customFont;
        }
        else
        {
            Debug.LogWarning("Custom Font belum diisi di Inspector TextBubble, menggunakan font default TMPro.");
        }

        tmpText.fontSize = fontSize;
        tmpText.color = textColor;
        tmpText.alignment = TextAlignmentOptions.Center;
        // Use textWrappingMode instead of the obsolete enableWordWrapping property
        tmpText.textWrappingMode = TextWrappingModes.Normal;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-40f, -40f); 

        // --- PROSES MENGUBAH LAYER AGAR UI SELALU DI ATAS ---
        int topLayer = LayerMask.NameToLayer("UI Top");
        if (topLayer != -1) // Memastikan layer sudah dibuat di Editor
        {
            bubbleContainer.layer = topLayer;
            bgObj.layer = topLayer;
            textObj.layer = topLayer;
        }

        bubbleContainer.SetActive(false);
    }

    private Sprite CreateRoundedSprite(int width, int height, int radius, Color color)
    {
        try
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color transparent = new Color(color.r, color.g, color.b, 0f);;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool isInside = true;

                    if (x < radius && y < radius)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) > radius)
                            isInside = false;
                    }
                    else if (x < radius && y >= height - radius)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius - 1)) > radius)
                            isInside = false;
                    }
                    else if (x >= width - radius && y < radius)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, radius)) > radius)
                            isInside = false;
                    }
                    else if (x >= width - radius && y >= height - radius)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, height - radius - 1)) > radius)
                            isInside = false;
                    }

                    tex.SetPixel(x, y, isInside ? color : transparent);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Failed to create procedural rounded sprite: " + e.Message);
            return null;
        }
    }

    private IEnumerator ShowDialogueRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        bubbleContainer.SetActive(true);

        // Boink saat bubble pertama kali muncul
        yield return StartCoroutine(BoinkRoutine());

        for (int i = 0; i < sentences.Length; i++)
        {
            // Boink saat ganti kalimat (kecuali kalimat pertama, sudah kena boink munculnya bubble)
            // if (i > 0)
            // {
            //     yield return StartCoroutine(BoinkRoutine());
            // }

            yield return StartCoroutine(TypeSentenceRoutine(sentences[i]));
            yield return new WaitForSeconds(delayBetweenSentences);
        }

        // Boink kecil sebelum bubble hilang
        yield return StartCoroutine(BoinkOutRoutine());

        CloseBubble();
    }

    private void CloseBubble()
    {
        if (isClosing)
        {
            return;
        }

        isClosing = true;

        if (bubbleContainer != null)
        {
            bubbleContainer.SetActive(false);
            Destroy(bubbleContainer);
            bubbleContainer = null;
        }

        if (this != null)
        {
            Destroy(this);
        }
    }
    
    // Menampilkan kalimat huruf demi huruf sesuai letterDelay
    private IEnumerator TypeSentenceRoutine(string sentence)
    {
        tmpText.text = "";
        foreach (char c in sentence)
        {
            tmpText.text += c;
            yield return new WaitForSeconds(letterDelay);
        }
    }

    // Efek "boink": scale dari kecil -> overshoot -> settle ke ukuran normal
    private IEnumerator BoinkRoutine()
    {
        if (bubbleRect == null) yield break;

        float elapsed = 0f;
        Vector3 from = baseScale * 0.4f;
        Vector3 overshoot = baseScale * boinkOvershoot;

        while (elapsed < boinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / boinkDuration);
            // Ease elastic sederhana: cepat naik, overshoot, lalu settle
            float easedT = EaseOutBack(t);
            bubbleRect.localScale = Vector3.LerpUnclamped(from, overshoot, easedT);
            yield return null;
        }

        bubbleRect.localScale = baseScale;
    }

    // Efek boink saat bubble akan hilang: sedikit membesar dulu lalu mengecil ke nol
    private IEnumerator BoinkOutRoutine()
    {
        if (bubbleRect == null) yield break;

        // Fase 1: sedikit overshoot membesar (kesan "boink")
        float elapsed = 0f;
        float popDuration = boinkDuration * 0.4f;
        Vector3 popScale = baseScale * boinkOvershoot;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            bubbleRect.localScale = Vector3.LerpUnclamped(baseScale, popScale, EaseOutBack(t));
            yield return null;
        }

        // Fase 2: mengecil ke nol
        elapsed = 0f;
        float shrinkDuration = boinkDuration * 0.6f;
        Vector3 from = bubbleRect.localScale;

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkDuration);
            bubbleRect.localScale = Vector3.LerpUnclamped(from, Vector3.zero, t * t);
            yield return null;
        }

        bubbleRect.localScale = Vector3.zero;
    }

    // Kurva ease-out-back untuk kesan overshoot khas "boink"
    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    private void OnDestroy()
    {
        if (bubbleContainer != null)
        {
            Destroy(bubbleContainer);
        }

        if (backgroundImage != null)
        {
            Destroy(backgroundImage.sprite);
        }
    }
}
