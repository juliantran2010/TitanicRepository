using UnityEngine;
using DG.Tweening;
using System;

public class PlayerLookController : MonoBehaviour
{
    [SerializeField] private Transform cinemachineCameraTarget; // Falls StarterAssets genutzt wird

    private Tween currentLookTween;

    /// <summary>
    /// Rotiert den Spieler weich zu einem Ziel-Transform
    /// </summary>
    public void LookAtTarget(Transform target, float duration = 0.5f, Action onComplete = null)
    {
        if (target == null) return;
        LookAtPosition(target.position, duration, onComplete);
    }

    public void LookAtTarget(Vector3 position, float duration = 0.5f, Action onComplete = null)
    {
        LookAtPosition(position, duration, onComplete);
    }

    /// <summary>
    /// Rotiert den Spieler weich zu einer Weltposition
    /// </summary>
    public void LookAtPosition(Vector3 targetWorldPos, float duration = 0.5f, Action onComplete = null)
    {
        currentLookTween?.Kill(); // Laufende Tweens stoppen

        // 1. Horizontale Richtung (Y-Achse / Yaw) berechnen:
        Vector3 dirToTarget = targetWorldPos - transform.position;
        dirToTarget.y = 0f; // Flach auf dem Boden halten, kein Rollen/Kippen

        if (dirToTarget.sqrMagnitude < 0.001f) return;

        Quaternion targetBodyRotation = Quaternion.LookRotation(dirToTarget);

        // 2. DOTween: Dreht den Spielerkörper sanft um die Y-Achse
        currentLookTween = transform.DORotateQuaternion(targetBodyRotation, duration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => onComplete?.Invoke());

        // 3. Optional: Wenn der Kopf/die Kamera auch vertikal nach oben/unten gucken soll:
        if (cinemachineCameraTarget != null)
        {
            Vector3 camToTarget = targetWorldPos - cinemachineCameraTarget.position;
            float pitchAngle = -Mathf.Atan2(camToTarget.y, new Vector2(camToTarget.x, camToTarget.z).magnitude) * Mathf.Rad2Deg;

            // Begrenzen (z. B. zwischen -40 und 40 Grad)
            pitchAngle = Mathf.Clamp(pitchAngle, -40f, 40f);

            cinemachineCameraTarget.DOLocalRotate(new Vector3(pitchAngle, 0f, 0f), duration)
                .SetEase(Ease.OutCubic);
        }
    }
}