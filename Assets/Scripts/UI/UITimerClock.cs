using UnityEngine;
using UnityEngine.UI; // Wajib untuk mengakses komponen Image
using DG.Tweening;    // Wajib untuk animasi membal (Jelly/Boink)

public class UITimerClock : MonoBehaviour
{
    [Header("Referensi Skrip Pusat Waktu")]
    [SerializeField] private TimerController timerController;

    [Header("Referensi Aset UI")]
    [SerializeField] private RectTransform hourHand;
    [SerializeField] private RectTransform minuteHand;
    [SerializeField] private Image progressCircle;     // Drag UI Lingkaran Kuning ke sini
    [SerializeField] private RectTransform clockContainer; // Objek induk seluruh Jam (yang akan bergetar)

    [Header("Pengaturan Efek Detak")]
    [SerializeField] private int totalTicks = 12;      // Dibagi menjadi 12 bagian progres
    private int lastTickStep = -1;                     // Variabel pelacak step detak

    // Nilai sudut awal (06:55) dan akhir (07:00)
    private readonly float minHandStartAngle = -330f;
    private readonly float minHandEndAngle = -360f;
    private readonly float hourHandStartAngle = -207.5f;
    private readonly float hourHandEndAngle = -210f;

    private void OnEnable()
    {
        TimerController.OnTimeUpdated += UpdateClockHands;
    }

    private void OnDisable()
    {
        TimerController.OnTimeUpdated -= UpdateClockHands;
    }

    private void UpdateClockHands(float currentTime)
    {
        if (timerController == null) return;

        float totalTimeLimit = timerController.timeLimit;
        if (totalTimeLimit <= 0) return;

        // Progres permainan: 0 (Mulai) sampai 1 (Waktu Habis)
        float progress = 1f - (Mathf.Clamp(currentTime, 0f, totalTimeLimit) / totalTimeLimit);

        // 1. Update Rotasi Jarum Jam (Visual Mulus)
        if (minuteHand != null) minuteHand.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(minHandStartAngle, minHandEndAngle, progress));
        if (hourHand != null) hourHand.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(hourHandStartAngle, hourHandEndAngle, progress));

        // 2. Update Lingkaran Kuning (Visual Mulus)
        if (progressCircle != null)
        {
            progressCircle.fillAmount = progress;
        }

        // 3. Logika "Trigger Detak" Setiap 1/12 Bagian
        int currentTickStep = Mathf.FloorToInt(progress * totalTicks);

        // Jika currentTickStep lebih besar dari step sebelumnya, berarti kita melewati batas 1/12 yang baru
        if (currentTickStep > lastTickStep && currentTickStep <= totalTicks)
        {
            TriggerTickEffect();
            lastTickStep = currentTickStep;
        }
        // Kondisi pelindung jika timer di-reset saat menekan "Restart Level"
        else if (currentTickStep < lastTickStep)
        {
            lastTickStep = currentTickStep;
        }
    }

    private void TriggerTickEffect()
    {
        if (clockContainer == null) return;

        // Bunuh animasi sebelumnya agar tidak bertumpuk / glitch kalau trigger terlalu cepat
        clockContainer.DOKill(true);
        clockContainer.localScale = Vector3.one;

        // Efek detak jantung / boink yang juicy (memompa ukuran sesaat lalu kembali normal)
        clockContainer.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.3f, 5, 0.5f).SetEase(Ease.OutElastic);
        
        // Opsional: Jika kamu punya AudioManager, kamu bisa panggil suara detak jam di sini!
        // AudioManager.Instance.PlaySFX("ClockTick");
    }
}