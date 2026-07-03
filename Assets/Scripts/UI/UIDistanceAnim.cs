using UnityEngine;
using DG.Tweening;

public class UIDistanceAnim : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform silhouetteContainer;

    [Header("Animation")]
    [SerializeField] private float punchScale = 0.12f;
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private int vibrato = 6;
    [SerializeField] private float elasticity = 0.8f;
    private Vector3 defaultScale;
    private void Awake()
    {
        defaultScale = silhouetteContainer.localScale;
    }

    public void TriggerMoveEffect()
    {
        if (silhouetteContainer == null) return;

        // Hentikan animasi sebelumnya supaya tidak numpuk
        silhouetteContainer.DOKill(true);

        // Pastikan kembali ke ukuran normal
        silhouetteContainer.localScale = defaultScale;

        // Efek popup/boink
        silhouetteContainer.DOPunchScale(
            defaultScale * punchScale,
            duration,
            vibrato,
            elasticity
        );

    }
}
