using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // Wichtig: Namespace für das neue Input System

public class BinocularController : MonoBehaviour
{
    [Header("Komponenten")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject overlayCanvas;

    [Header("Zoom Einstellungen")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomFOV = 15f;
    [SerializeField] private float zoomSpeed = 8f;

    [Header("Erkennung")]
    [SerializeField] private LayerMask icebergLayer;
    [SerializeField] private float requiredLookDuration = 1.5f;

    private bool isUsingBinoculars = false;
    private float lookTimer = 0f;
    private bool icebergFound = false;

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        overlayCanvas.SetActive(false);
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (!Inventory.Instance.ContainsItem("binoculars")) return;

            if (!isUsingBinoculars)
            {
                // Startet das Ansetzen
                RaiseBinoculars();
            }
            else
            {
                // Setzt das Fernglas wieder ab
                LowerBinoculars();
            }
        }

        if (isUsingBinoculars && !icebergFound)
        {
            CheckForIceberg();
        }
    }

    private Tween RaiseBinoculars()
    {
        PickupObject binoculars = Inventory.Instance.GetItem("binoculars");
        Sequence seq = DOTween.Sequence();
        if (binoculars == null) return seq;

        // 1. Folge-Skript und vorherige Tweens beenden
        if (binoculars.TryGetComponent<AttachToCameraField>(out var attachScript))
        {
            attachScript.enabled = false;
        }
        binoculars.transform.DOKill();

        // 2. Start- und Wegpunkte relativ zur Kamera
        Vector3 startPos = binoculars.transform.position;
        Quaternion startRot = binoculars.transform.rotation;

        // Wegpunkt 1 (Zwischenschritt): Leicht angehoben, etwas nach vorne/innen geschoben (Bogen)
        Vector3 midViewport = new Vector3(0.55f, 0.35f, 0.45f);

        // Wegpunkt 2 (Ziel am Gesicht): Zentriert vor den Augen
        float eyeDist = Mathf.Max(playerCamera.nearClipPlane + 0.05f, 0.18f);
        Vector3 finalViewport = new Vector3(0.5f, 0.5f, eyeDist);

        // Gesamtdauer z.B. 0.85s für eine ruhige, spürbare Handbewegung
        float phase1Duration = 0.45f;
        float phase2Duration = 0.40f;

        // --- PHASE 1: Hochheben und vor die Brust bringen ---
        seq.Append(DOTween.To(() => 0f, t =>
        {
            Vector3 midPos = playerCamera.ViewportToWorldPoint(midViewport);
            binoculars.transform.position = Vector3.Lerp(startPos, midPos, t);
        }, 1f, phase1Duration).SetEase(Ease.OutQuad));

        // Dabei neigt sich das Fernglas leicht schräg (natürlicher Handgriff)
        seq.Join(DOTween.To(() => 0f, t =>
        {
            Quaternion midRot = playerCamera.transform.rotation * Quaternion.Euler(15f, -8f, 5f);
            binoculars.transform.rotation = Quaternion.Slerp(startRot, midRot, t);
        }, 1f, phase1Duration).SetEase(Ease.OutQuad));

        // --- PHASE 2: Direkt an die Augen ziehen & waagerecht ausrichten ---
        seq.Append(DOTween.To(() => 0f, t =>
        {
            Vector3 midPos = playerCamera.ViewportToWorldPoint(midViewport);
            Vector3 endPos = playerCamera.ViewportToWorldPoint(finalViewport);
            binoculars.transform.position = Vector3.Lerp(midPos, endPos, t);
        }, 1f, phase2Duration).SetEase(Ease.InCubic));

        seq.Join(DOTween.To(() => 0f, t =>
        {
            Quaternion midRot = playerCamera.transform.rotation * Quaternion.Euler(15f, -8f, 5f);
            binoculars.transform.rotation = Quaternion.Slerp(midRot, playerCamera.transform.rotation, t);
        }, 1f, phase2Duration).SetEase(Ease.InCubic));

        // Zoom (FOV) schon während Phase 2 starten lassen für flüssigen Übergang
        seq.Insert(phase1Duration + 0.1f, playerCamera.DOFieldOfView(zoomFOV, phase2Duration + 0.1f).SetEase(Ease.InOutQuad));

        // --- ABSCHLUSS: Overlay einblenden und Modell verstecken ---
        seq.OnComplete(() =>
        {
            SetBinocularVisibility(binoculars.gameObject, false);
            isUsingBinoculars = true;
            if (overlayCanvas != null) overlayCanvas.SetActive(true);
        });

        return seq;
    }

    private void LowerBinoculars(Action OnLowered = null)
    {
        PickupObject binoculars = Inventory.Instance.GetItem("binoculars");
        if (binoculars == null) return;

        binoculars.transform.DOKill();
        playerCamera.DOKill();

        // Overlay sofort ausblenden, Modell wieder sichtbar machen
        isUsingBinoculars = false;
        if (overlayCanvas != null) overlayCanvas.SetActive(false);
        SetBinocularVisibility(binoculars.gameObject, true);

        // FOV wieder auf Standard zurückfahren
        playerCamera.DOFieldOfView(normalFOV, 0.45f).SetEase(Ease.OutQuad);

        // Fernglas startet am Gesicht und senkt sich ab
        Vector3 startFacePos = playerCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.2f));
        binoculars.transform.position = startFacePos;
        binoculars.transform.rotation = playerCamera.transform.rotation;

        Vector3 lowerViewport = new Vector3(0.7f, 0.15f, 0.45f);

        Sequence lowerSeq = DOTween.Sequence();
        lowerSeq.Append(DOTween.To(() => 0f, t =>
        {
            Vector3 targetPos = playerCamera.ViewportToWorldPoint(lowerViewport);
            binoculars.transform.position = Vector3.Lerp(startFacePos, targetPos, t);
        }, 1f, 0.5f).SetEase(Ease.OutQuad));

        lowerSeq.OnComplete(() =>
        {
            if (binoculars.TryGetComponent<AttachToCameraField>(out var attachScript))
            {
                attachScript.enabled = true;
            }
            OnLowered?.Invoke();
        });
    }

    // Hilfsmethode, um wirklich alle Meshes des Fernglases zuverlässig auszublenden
    private void SetBinocularVisibility(GameObject obj, bool visible)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            rend.enabled = visible;
        }
    }

    private void CheckForIceberg()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, icebergLayer))
        {
            lookTimer += Time.deltaTime;

            if (lookTimer >= requiredLookDuration)
            {
                OnIcebergDiscovered(hit.collider.gameObject);
            }
        }
        else
        {
            lookTimer = 0f;
        }
    }

    private void OnIcebergDiscovered(GameObject iceberg)
    {
        icebergFound = true;
        Debug.Log("Iceberg discovered!");
        LowerBinoculars(() =>
        {
            GameSceneManager.Instance.ChangeScene("TitanicScene", "titanic_deck");
        });
    }
}