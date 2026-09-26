using DG.Tweening;
using UnityEngine;

public class FocusObject : MonoBehaviour
{
    [SerializeField] private bool focusOnStart = false;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    void Start()
    {
        if (focusOnStart)
        {
            Vector3 position = transform.position + positionOffset;
            FocusPlayerToTarget(position, duration);
        }
    }

    public void FocusPlayerToTarget(Vector3 position, float duration = 0.5f)
    {
        PlayerLookController controller = PersistentPlayer.Instance.GetComponent<PlayerLookController>();
        if (controller == null) return;

        controller.LookAtTarget(position, duration);
    }
}
