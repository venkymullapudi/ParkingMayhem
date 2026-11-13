using UnityEngine;

public class CircleVehicle : BaseVehicle
{
    protected override void InitializeVehicle()
    {
        vehicleName = "Circle";
        vehicleSize = 1f;
        vehicleColor = new Color(0.2f, 0.6f, 1f);
        base.InitializeVehicle();
    }
}
