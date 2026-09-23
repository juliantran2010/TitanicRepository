using System.Collections.Generic;
using UnityEngine;

public class CameraTourSceneBinding : MonoBehaviour
{
    [Header("Wegpunkte & Touren für diese Szene")]
    [SerializeField] private List<CameraTourManager.CameraTour> sceneTours = new List<CameraTourManager.CameraTour>();

    private void Start()
    {
        if (CameraTourManager.Instance != null)
        {
            CameraTourManager.Instance.RegisterSceneTours(sceneTours);
        }
    }

    private void OnDestroy()
    {
        if (CameraTourManager.Instance != null)
        {
            CameraTourManager.Instance.UnregisterSceneTours();
        }
    }
}