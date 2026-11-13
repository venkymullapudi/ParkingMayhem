using UnityEngine;

public class SquareVehicle : BaseVehicle
{
    protected override void Awake()
    {
        base.Awake();
        vehicleName = "Square Vehicle";
    }

    protected override void CreateWheels()
    {
        wheelTransforms = new Transform[4];
        float wheelOffset = vehicleSize * 0.4f;

        // Create four wheels at the corners
        for (int i = 0; i < 4; i++)
        {
            GameObject wheel = new GameObject($"Wheel_{i}");
            wheel.transform.SetParent(transform);
            
            // Position wheels at the corners
            float x = (i % 2 == 0) ? -wheelOffset : wheelOffset;
            float y = (i < 2) ? -wheelOffset : wheelOffset;
            wheel.transform.localPosition = new Vector3(x, y, 0f);

            // Add wheel sprite
            SpriteRenderer wheelRenderer = wheel.AddComponent<SpriteRenderer>();
            wheelRenderer.sprite = Resources.Load<Sprite>("Sprites/Wheel");
            wheelRenderer.color = wheelColor;
            wheelRenderer.sortingOrder = 1;

            wheelTransforms[i] = wheel.transform;
        }
    }

    protected override void UpdateVisuals()
    {
        base.UpdateVisuals();

        // Set square sprite
        if (vehicleSprite != null)
        {
            vehicleSprite.sprite = Resources.Load<Sprite>("Sprites/Square");
            vehicleSprite.sortingOrder = 0;
        }
    }
} 