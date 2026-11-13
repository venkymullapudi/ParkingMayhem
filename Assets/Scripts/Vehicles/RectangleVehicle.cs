using UnityEngine;

public class RectangleVehicle : BaseVehicle
{
    protected override void Awake()
    {
        base.Awake();
        vehicleName = "Rectangle Vehicle";
    }

    protected override void CreateWheels()
    {
        wheelTransforms = new Transform[4];
        float wx = vehicleSize * 0.55f;
        float wy = vehicleSize * 0.3f;
        Vector2[] positions = { new(-wx, -wy), new(wx, -wy), new(-wx, wy), new(wx, wy) };

        for (int i = 0; i < 4; i++)
        {
            GameObject wheel = new GameObject($"Wheel_{i}");
            wheel.transform.SetParent(transform);
            wheel.transform.localPosition = new Vector3(positions[i].x, positions[i].y, 0f);
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
            vehicleSprite.sprite = Resources.Load<Sprite>("Sprites/Rectangle");
            vehicleSprite.transform.localScale = new Vector3(vehicleSize * 1.4f, vehicleSize * 0.8f, 1f);
            vehicleSprite.sortingOrder = 0;
        }
    }
}
