using UnityEngine;

public class BaseVehicle : MonoBehaviour
{
    [Header("Vehicle Properties")]
    public string vehicleName;
    public VehicleController vehicleController;
    public SpriteRenderer vehicleSprite;
    public Transform[] wheelTransforms;
    public float vehicleSize = 1f;
    public Color vehicleColor = Color.white;

    [Header("Wheel Properties")]
    public float wheelSize = 0.2f;
    public Color wheelColor = Color.black;

    protected virtual void Awake()
    {
        InitializeVehicle();
    }

    protected virtual void InitializeVehicle()
    {
        // Create vehicle sprite
        if (vehicleSprite == null)
        {
            GameObject spriteObj = new GameObject("VehicleSprite");
            spriteObj.transform.SetParent(transform);
            spriteObj.transform.localPosition = Vector3.zero;
            vehicleSprite = spriteObj.AddComponent<SpriteRenderer>();
        }

        // Create wheels
        if (wheelTransforms == null || wheelTransforms.Length == 0)
        {
            CreateWheels();
        }

        // Setup vehicle controller
        if (vehicleController == null)
        {
            vehicleController = gameObject.AddComponent<VehicleController>();
            vehicleController.wheels = wheelTransforms;
        }

        // Set visual properties
        UpdateVisuals();
    }

    protected virtual void CreateWheels()
    {
        // This will be overridden by specific vehicle types
    }

    protected virtual void UpdateVisuals()
    {
        if (vehicleSprite != null)
        {
            vehicleSprite.color = vehicleColor;
            vehicleSprite.transform.localScale = Vector3.one * vehicleSize;
        }

        if (wheelTransforms != null)
        {
            foreach (Transform wheel in wheelTransforms)
            {
                SpriteRenderer wheelRenderer = wheel.GetComponent<SpriteRenderer>();
                if (wheelRenderer != null)
                {
                    wheelRenderer.color = wheelColor;
                    wheel.localScale = Vector3.one * wheelSize;
                }
            }
        }
    }

    public virtual void SetColor(Color newColor)
    {
        vehicleColor = newColor;
        UpdateVisuals();
    }

    public virtual void SetSize(float newSize)
    {
        vehicleSize = newSize;
        UpdateVisuals();
    }
} 