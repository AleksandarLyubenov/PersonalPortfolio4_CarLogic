using UnityEngine;

public class TrafficLightFixture : MonoBehaviour
{
    public Renderer topCylinder;    // Red
    public Renderer bottomCylinder; // Green

    public Material redMaterial;
    public Material greenMaterial;
    public Material offMaterial;

    public void SetState(bool isGreen)
    {
        if (isGreen)
        {
            topCylinder.material = offMaterial;
            bottomCylinder.material = greenMaterial;
        }
        else
        {
            topCylinder.material = redMaterial;
            bottomCylinder.material = offMaterial;
        }
    }

    public void TurnOff()
    {
        topCylinder.material = offMaterial;
        bottomCylinder.material = offMaterial;
    }
}
