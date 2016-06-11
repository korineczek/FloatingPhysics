using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class InterfaceToggle : MonoBehaviour
{

    public Transform Boat;
    public Transform Water;

    private BoatPhysics boatPhysics;
    private DynamicWater dynamicWater;

    private Slider amplitude;
    private Slider frequency;
    private Slider speed;
    private Slider weight;


	// Use this for initialization
	void Start ()
	{
	    boatPhysics = Boat.GetComponent<BoatPhysics>();
	    dynamicWater = Water.GetComponent<DynamicWater>();
	    amplitude = transform.FindChild("Amplitude").GetComponent<Slider>();
	    frequency = transform.FindChild("Frequency").GetComponent<Slider>();
	    speed = transform.FindChild("Speed").GetComponent<Slider>();
        weight = transform.FindChild("Weight").GetComponent<Slider>();
	}

    public void ResetPosition()
    {
        Boat.position = new Vector3(50,2,0);
        Boat.rotation = Quaternion.Euler(0,0,0); 
    }

    public void Drop()
    {
        Boat.position += new Vector3(0,5,0);
    }

    public void Viscous()
    {
        if (boatPhysics.Viscous)
        {
            boatPhysics.Viscous = false;
        }
        else
        {
            boatPhysics.Viscous = true;
        }
    }

    public void Slam()
    {
        if (boatPhysics.Slam)
        {
            boatPhysics.Slam = false;
        }
        else
        {
            boatPhysics.Slam = true;
        }
    }

    public void PressureDrag()
    {
        if (boatPhysics.PressureDrag)
        {
            boatPhysics.PressureDrag = false;
        }
        else
        {
            boatPhysics.PressureDrag = true;
        }
    }

    public void Amplitude()
    {
        dynamicWater.Amplitude = amplitude.value;

    }

    public void Frequency()
    {
        dynamicWater.Frequency = (int)frequency.value;
    }

    public void Speed()
    {
        dynamicWater.Speed = (int) speed.value;
    }
    public void Weight()    
    {
        Boat.GetComponent<Rigidbody>().mass = (int)weight.value;
    }
}
