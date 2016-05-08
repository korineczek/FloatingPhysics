using UnityEngine;
using System.Collections;

public class DynamicWater : MonoBehaviour {

    public Transform WaterPiece;
    public int Multiplier = 0;
    public float Speed = 0;
    public float[,] PerlinOffset = new float[100, 100];
    public Transform[,] WaterArray =new Transform[100,100];
    private int offset = 0;

	// Use this for initialization
	void Start () {
        offset = Random.Range(0,6);
	    UpdatePerlin(PerlinOffset, offset, Multiplier);

	    for (int i = 0; i < 100; i++)
	    {
	        for (int j = 0; j < 100; j++)
	        {
	            WaterArray[i, j] = Instantiate(WaterPiece) as Transform;
	            WaterArray[i, j].position = new Vector3(i/10f,PerlinOffset[i,j],j/10f);
                WaterArray[i,j].SetParent(this.transform);
	        }
	    }
	    StartCoroutine(WaterCycle());
	}
	
	// Update is called once per frame
	public IEnumerator WaterCycle ()
	{
	    while (true)
	    {
	        if (offset > 360)
	        {
	            offset = 0;
	        }
	        offset++;
	        UpdatePerlin(PerlinOffset, offset*Speed, Multiplier);
	        UpdateWater(WaterArray);
	        //yield return new WaitForSeconds(0.1f);
	        yield return new WaitForEndOfFrame();
	    }
	}

    public float[,] UpdatePerlin(float[,] input, float offset, int multiplier)
    {
        for (int i = 0; i < 100; i++)
        {
            for (int j = 0; j < 100; j++)
            {
                //input[i, j] = Mathf.PerlinNoise((float)i/offset, (float)j/offset);
                input[i, j] = Mathf.Sin((multiplier*i+offset)*Mathf.Deg2Rad);
                //Debug.Log(input[i,j]);
            }
        }
        return input;
    }

    public void UpdateWater(Transform[,] water)
    {
        for (int i = 0; i < 100; i++)
        {
            for (int j = 0; j < 100; j++)
            {
                water[i,j].position = new Vector3(i/10f,PerlinOffset[i,j],j/10f);
            }
        }
    }
}
