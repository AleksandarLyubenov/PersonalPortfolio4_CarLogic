using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    public TrafficLightController controller;
    public bool isNorthSouth; // Direction this light governs

    public bool IsGreen()
    {
        return controller != null && controller.IsGreenForDirection(isNorthSouth);
    }
}
