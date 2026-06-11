using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager instance;

    public float amplitude = 1f;
    public float length = 2f;
    public float speed = 1f;
    public float offset = 0f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this) 
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    private void Update()
    {
        offset += speed * Time.deltaTime;
    }

    public float GetWaveHeight(float x, float z)
    {
        // X and Z combined create diagonal waves, which makes the ship tilt on both axes
        return amplitude * Mathf.Sin((x + z) / length + offset);
    }
}
