using UnityEngine;

public class PentagonVehicle : BaseVehicle
{
    protected override void Awake()
    {
        base.Awake();
        vehicleName = "Pentagon Vehicle";
    }

    protected override void CreateWheels()
    {
        wheelTransforms = new Transform[5];
        float r = vehicleSize * 0.42f;
        for (int i = 0; i < 5; i++)
        {
            GameObject wheel = new GameObject($"Wheel_{i}");
            wheel.transform.SetParent(transform);
            float angle = (i * 72f - 90f) * Mathf.Deg2Rad;
            wheel.transform.localPosition = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f);
            SpriteRenderer wr = wheel.AddComponent<SpriteRenderer>();
            wr.sprite = Resources.Load<Sprite>("Sprites/Wheel");
            wr.color = wheelColor;
            wr.sortingOrder = 1;
            wheelTransforms[i] = wheel.transform;
        }
    }

    protected override void UpdateVisuals()
    {
        base.UpdateVisuals();
        if (vehicleSprite != null)
        {
            vehicleSprite.sprite = Resources.Load<Sprite>("Sprites/Pentagon");
            vehicleSprite.sortingOrder = 0;
        }
    }
}
