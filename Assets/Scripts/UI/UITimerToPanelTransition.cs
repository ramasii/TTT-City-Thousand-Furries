using UnityEngine;
using DG.Tweening;

public class UITimerToPanelTransition : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform timerRect;          // Objek UI Timer yang ada di HUD
    [SerializeField] private RectTransform targetSlotRect;     // GameObject kosong sebagai penanda posisi di dalam Panel Completed

    [Header("Transition Settings")]
    [SerializeField] private float duration = 0.55f;
    [SerializeField] private Ease moveEase = Ease.OutBack;     // Efek melenting saat mendarat
    [SerializeField] private Vector3 finalScale = new Vector3(1.3f, 1.3f, 1.3f); // Skala timer saat di panel (biasanya lebih besar)

    [Header("Comic Juice Settings")]
    [SerializeField] private float spinAmount = -360f;         // Efek berputar saat terbang melesat
    [SerializeField] private bool punchOnArrival = true;       // Efek hentakan jeli saat sampai

    /// <summary>
    /// Panggil fungsi ini ketika event OnTime / Win dipicu
    /// </summary>
    public void PlayTimerTransition()
    {
        if (timerRect == null || targetSlotRect == null)
        {
            Debug.LogWarning("Referensi UI belum dipasang di Inspector!");
            return;
        }

        // Hentikan tween aktif pada timer agar tidak terjadi bentrokan logika
        timerRect.DOKill();

        // Menggunakan DOTween Sequence untuk menciptakan rangkaian gerakan yang juicy
        Sequence transitionSequence = DOTween.Sequence();

        // 1. ANTICIPATION (Ancang-ancang): Timer mengecil & miring sedikit dengan cepat sebelum melesat
        transitionSequence.Append(timerRect.DOScale(Vector3.one * 0.8f, 0.08f).SetEase(Ease.OutQuad));
        transitionSequence.Join(timerRect.DORotate(new Vector3(0, 0, 15f), 0.08f));

        // 2. THE FLIGHT (Terbang): Melesat menggunakan DOMove (World Position) ke slot target
        // Kita gunakan Join agar pergerakan, putaran, dan perubahan ukuran selesai bersamaan
        transitionSequence.Append(timerRect.DOMove(targetSlotRect.position, duration).SetEase(moveEase));
        transitionSequence.Join(timerRect.DORotate(new Vector3(0, 0, spinAmount), duration, RotateMode.FastBeyond360).SetEase(Ease.InOutQuad));
        transitionSequence.Join(timerRect.DOScale(finalScale, duration).SetEase(moveEase));

        // 3. LANDING (Mendarat): Berikan efek hentakan pegas/jeli pekat khas komik saat tiba
        if (punchOnArrival)
        {
            transitionSequence.OnComplete(() =>
            {
                // Mengembalikan rotasi ke tegak lurus (0) atau sedikit miring estetik
                timerRect.localEulerAngles = Vector3.zero;

                // Efek membal (Boink!)
                timerRect.DOPunchScale(new Vector3(0.25f, 0.25f, 0.25f), 0.35f, 8, 1f)
                    .SetUpdate(true); // Pastikan efek membal mengabaikan pause

                // PENTING: Pindahkan parent si Timer ke dalam slot di Panel Completed.
                // Argumen 'true' menjaga agar posisi visualnya tidak melompat saat berpindah hierarki parent.
                timerRect.SetParent(targetSlotRect, true);
            });
        }

        // SetUpdate(true) wajib dipasang agar transisi UI tetap berjalan mulus
        // meskipun GameManager mematikan sisa jalannya physics dunia (Time.timeScale = 0)
        transitionSequence.SetUpdate(true);
    }
}