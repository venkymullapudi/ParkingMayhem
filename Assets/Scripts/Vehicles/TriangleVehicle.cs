using UnityEngine;

public class TriangleVehicle : BaseVehicle
{
    protected override void Awake()
    {
        base.Awake();
        vehicleName = "Triangle Vehicle";
    }

    protected override void CreateWheels()
    {
        wheelTransforms = new Transform[3];
        float wheelOffset = vehicleSize * 0.4f;

        // Create three wheels at the vertices of the triangle
        for (int i = 0; i < 3; i++)
        {
            GameObject wheel = new GameObject($"Wheel_{i}");
            wheel.transform.SetParent(transform);
            
            // Position wheels at the vertices of an equilateral triangle
            float angle = i * 120f * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * wheelOffset;
            float y = Mathf.Sin(angle) * wheelOffset;
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

        // Set triangle sprite
        if (vehicleSprite != null)
        {
            vehicleSprite.sprite = Resources.Load<Sprite>("Sprites/Triangle");
            vehicleSprite.sortingOrder = 0;
        }
    }
} 