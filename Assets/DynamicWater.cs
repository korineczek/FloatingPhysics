using UnityEngine;
using System.Collections;

public class DynamicWater : MonoBehaviour {

    //variables
    public Transform WaterPiece;
    public int Frequency = 0;
    public float Speed = 0;
    public float Amplitude = 1;
    public float[] PerlinOffset = new float[1000];
    public Transform[] WaterArray =new Transform[1000];
    private int offset = 0;

	// Use this for initialization
	void Start () {
        //offset = Random.Range(0,6);
	    offset = 0;
        UpdatePerlin(PerlinOffset, offset, Frequency, Amplitude);

	    for (int i = 0; i < 1000; i++)
	    {
	        
	            WaterArray[i] = Instantiate(WaterPiece) as Transform;
	            WaterArray[i].position = new Vector3(i/10f,PerlinOffset[i],0/10f);
                WaterArray[i].SetParent(this.transform); 
	    }
	    StartCoroutine(WaterCycle());
	}
	
	// Update is called once per frame
	public IEnumerator WaterCycle ()
	{
	    while (true)
	    {
	        if (offset > 3600000)
	        {
	            offset = 0;
	        }
	        offset++;
	        UpdatePerlin(PerlinOffset, offset*Speed, Frequency,Amplitude);
	        UpdateWater(WaterArray);
	        yield return new WaitForEndOfFrame();
	    }
	}

    //first started off as perlin noise, but worked much better with just a simple sinusoid
    public float[] UpdatePerlin(float[] input, float offset, int frequency, float amplitude)
    {
        for (int i = 0; i < 1000; i++)
        {
            input[i] = Mathf.Sin(((frequency * i + offset)) * Mathf.Deg2Rad)* amplitude ;  
        }
        return input;
    }
    //update positionts of the water
    public void UpdateWater(Transform[] water)
    {
        for (int i = 0; i < 1000; i++)
        {
           
                water[i].position = new Vector3(i/10f,PerlinOffset[i],0);
            
        }
    }
}
