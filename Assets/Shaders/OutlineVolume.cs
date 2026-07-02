using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenu("Custom/Roystan Outline")]
public class OutlineVolume : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Warna dari garis tepi (Alpha untuk ketebalan/opacity)")]
    public ColorParameter outlineColor = new ColorParameter(Color.black);

    [Tooltip("Ketebalan maksimal garis tepi")]
    public ClampedFloatParameter outlineScale = new ClampedFloatParameter(3f, 0f, 10f);

    [Header("Distance Settings")]
    [Tooltip("Seberapa cepat ketebalan menipis saat objek menjauh")]
    public ClampedFloatParameter distanceFalloff = new ClampedFloatParameter(5f, 0f, 50f);

    [Tooltip("Ketebalan minimal agar garis tidak hilang saat objek sangat jauh")]
    public ClampedFloatParameter minOutlineScale = new ClampedFloatParameter(1f, 0f, 5f);

    [Header("Threshold Settings")]
    [Tooltip("Sensitivitas deteksi sudut berdasarkan kedalaman (Depth)")]
    public ClampedFloatParameter depthThreshold = new ClampedFloatParameter(1.5f, 0f, 10f);

    [Tooltip("Sensitivitas deteksi sudut berdasarkan arah permukaan (Normal)")]
    public ClampedFloatParameter normalThreshold = new ClampedFloatParameter(0.4f, 0f, 1f);

    [Tooltip("Toleransi artefak hitam pada sudut miring (Grazing Angle). Naikkan jika ada garis palsu di dinding/lantai.")]
    public ClampedFloatParameter grazingTolerance = new ClampedFloatParameter(5f, 0f, 20f);

    public bool IsActive() => outlineColor.value.a > 0f && active;

    public bool IsTileCompatible() => false;
}