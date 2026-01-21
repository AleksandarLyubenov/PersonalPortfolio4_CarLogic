using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    public List<TrafficLightFixture> northSouthLights;
    public List<TrafficLightFixture> eastWestLights;

    public float greenTime = 10f;
    public float redTime = 10f;

    private bool nsGreen = true;

    void Start()
    {
        StartCoroutine(CycleLights());
    }

    IEnumerator CycleLights()
    {
        while (true)
        {
            SetLights(northSouthLights, nsGreen);
            SetLights(eastWestLights, !nsGreen);

            yield return new WaitForSeconds(nsGreen ? greenTime : redTime);
            nsGreen = !nsGreen;
        }
    }

    void SetLights(List<TrafficLightFixture> fixtures, bool isGreen)
    {
        foreach (var fixture in fixtures)
        {
            if (fixture != null)
                fixture.SetState(isGreen);
        }
    }

    public bool IsGreenForDirection(bool isNorthSouth)
    {
        return isNorthSouth == nsGreen;
    }
}
