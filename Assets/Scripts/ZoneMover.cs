using UnityEngine;

// Attached to ParkingZone at higher levels — gently moves the zone back and forth.
// Speed and pattern scale with level difficulty.
public class ZoneMover : MonoBehaviour
{
    public float speed = 0.6f;
    public float rangeX = 1.8f;
    public float rangeY = 0f;
    public float phaseOffset = 0f;

    private Vector3 origin;

    private void Start()
    {
        origin = transform.position;
    }

    private void Update()
    {
        float t = Time.time + phaseOffset;
        float dx = Mathf.Sin(t * speed) * rangeX;
        float dy = rangeY > 0f ? Mathf.Cos(t * speed * 0.7f) * rangeY : 0f;
        transform.position = new Vector3(origin.x + dx, origin.y + dy, origin.z);
    }
}
