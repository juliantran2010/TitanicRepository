using UnityEngine;

public class Floater : MonoBehaviour
{
    public Rigidbody rigidbody;
    public float depthBeforeSubmerged = 2f;
    public float displacementAmount = 5f;
    public int floaterCount = 4;

    private void FixedUpdate()
    {
        // Wellenhöhe an der Position des Floaters abfragen (X und Z)
        float waveHeight = WaveManager.instance.GetWaveHeight(transform.position.x, transform.position.z);

        // Wenn der Floater unter der Welle liegt
        if (transform.position.y < waveHeight)
        {
            float displacementMultiplier = (waveHeight - transform.position.y) / depthBeforeSubmerged;
            if (displacementMultiplier < 0f) displacementMultiplier = 0f;
            float buoyancyMagnitude = Mathf.Abs(Physics.gravity.y) * (rigidbody.mass / floaterCount) * displacementMultiplier * displacementAmount;
            Vector3 buoyancyForce = Vector3.up * buoyancyMagnitude;

            rigidbody.AddForceAtPosition(buoyancyForce, transform.position, ForceMode.Force);
        }
    }
}