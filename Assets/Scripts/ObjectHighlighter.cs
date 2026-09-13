using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ObjectHighlighter : MonoBehaviour
{
    public enum HighlightMode
    {
        Solid,
        Pulse,
        FlashOnce
    }

    [Header("Color Settings")]
    [ColorUsage(true, true)]
    [SerializeField] private Color highlightColor = new Color(0.2f, 0.7f, 1f) * 2.5f;

    [Header("Animation Settings")]
    [SerializeField] private HighlightMode mode = HighlightMode.Pulse;
    [SerializeField] private float transitionDuration = 0.8f;
    [SerializeField] private float pulseMinMultiplier = 0.4f;

    [Header("Scope")]
    [Tooltip("Wenn aktiv, werden auch alle Renderer in Kind-Objekten hervorgehoben.")]
    [SerializeField] private bool includeChildren = true;

    private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
    private Renderer[] targetRenderers;
    private MaterialPropertyBlock propBlock;
    private Sequence currentSequence;
    private Color currentColor = Color.black;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();

        if (includeChildren)
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        else
        {
            Renderer r = GetComponent<Renderer>();
            targetRenderers = r != null ? new[] { r } : new Renderer[0];
        }

        // Shader-Keyword aktivieren, damit der Shader überhaupt auf Emission reagieren kann
        HashSet<Material> registeredMaterials = new HashSet<Material>();
        foreach (Renderer rend in targetRenderers)
        {
            if (rend == null) continue;
            foreach (Material mat in rend.sharedMaterials)
            {
                if (mat != null && registeredMaterials.Add(mat))
                {
                    mat.EnableKeyword("_EMISSION");
                }
            }
        }

        // ZUM START: Emission sofort auf komplett Schwarz setzen (Licht aus)
        ApplyColor(Color.black);
    }

    [ContextMenu("Highlight On")]
    public void HighlightOn()
    {
        currentSequence?.Kill();
        currentSequence = DOTween.Sequence();

        switch (mode)
        {
            case HighlightMode.Solid:
                currentSequence.Append(
                    DOTween.To(() => currentColor, ApplyColor, highlightColor, transitionDuration)
                           .SetEase(Ease.OutSine)
                );
                break;

            case HighlightMode.Pulse:
                currentSequence.Append(
                    DOTween.To(() => currentColor, ApplyColor, highlightColor, transitionDuration)
                           .SetEase(Ease.InQuad)
                );
                currentSequence.Append(
                    DOTween.To(() => currentColor, ApplyColor, highlightColor * pulseMinMultiplier, transitionDuration * 1.2f)
                           .SetEase(Ease.InOutSine)
                           .SetLoops(-1, LoopType.Yoyo)
                );
                break;

            case HighlightMode.FlashOnce:
                currentSequence.Append(
                    DOTween.To(() => currentColor, ApplyColor, highlightColor, transitionDuration * 0.5f)
                           .SetEase(Ease.OutSine)
                );
                currentSequence.Append(
                    DOTween.To(() => currentColor, ApplyColor, Color.black, transitionDuration * 0.5f)
                           .SetEase(Ease.InSine)
                );
                break;
        }

        currentSequence.SetTarget(gameObject);
    }

    [ContextMenu("Highlight Off")]
    public void HighlightOff(bool instant = false)
    {
        currentSequence?.Kill();

        if (instant)
        {
            ApplyColor(Color.black);
            return;
        }

        currentSequence = DOTween.Sequence();
        currentSequence.Append(
            DOTween.To(() => currentColor, ApplyColor, Color.black, transitionDuration * 0.5f)
                   .SetEase(Ease.OutSine)
        ).SetTarget(gameObject);
    }

    private void ApplyColor(Color color)
    {
        currentColor = color;

        foreach (Renderer rend in targetRenderers)
        {
            if (rend == null) continue;

            rend.GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColorProp, color);
            rend.SetPropertyBlock(propBlock);
        }
    }

    private void OnDisable()
    {
        HighlightOff(true);
    }

    private void OnDestroy()
    {
        currentSequence?.Kill();
    }
}