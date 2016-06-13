using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class BoatPhysics : MonoBehaviour
{
    //VARIABLES
    //controls
    public bool DebugForce = false;
    public bool Slam = false;
    public bool PressureDrag = false;
    public bool Viscous = false;
    //pressure drag controls - experimental
    public float CPD1 = 10;
    public float CPD2 = 10;
    public float vr = 1f;
    public float fp = 0.5f;
    public float CSD1 = 10;
    public float CSD2 = 10;
    public float fs = 0.5f;

    //References
    public GameObject UnderwaterMeshOBJ;
    public GameObject WaterPlane;
    private DynamicWater water;
    private Vector3 waterSpeed = new Vector3();
    private Mesh BoatMesh;
    private Vector3[] originalVerticesArray; 
    private int[] originalTrianglesArray;
    private Mesh UnderWaterMesh;
    private List<Vector3> underwaterVertices;
    private List<int> underwaterTriangles;
    private Rigidbody boatRB;

    //Needs to be calculated at runtime
    private float resistanceCoefficient = 0f;

    //Grab references and set values at start
    void Start()
    {
        UnderWaterMesh = UnderwaterMeshOBJ.GetComponent<MeshFilter>().mesh;
        BoatMesh = this.GetComponent<MeshFilter>().mesh;
        water = WaterPlane.GetComponent<DynamicWater>();

        originalVerticesArray = BoatMesh.vertices;
        originalTrianglesArray = BoatMesh.triangles;

        boatRB = this.GetComponent<Rigidbody>();

        boatRB.maxAngularVelocity = 0.5f;
        boatRB.centerOfMass = new Vector3(0.7f,0,0);
    }

    //in update we calculate the underwater mesh and other variables that are necessary to be calculated
    void Update()
    {
        GenerateUnderwaterMesh();
        resistanceCoefficient = GetResistanceCoefficient(boatRB.velocity.magnitude, UnderWaterMesh.bounds.size.x, 0.000001f);
        waterSpeed = new Vector3(water.Speed,0,0);
    }

    /// <summary>
    /// Fixed updata adds all calculated forces to the boat
    /// </summary>
    void FixedUpdate()
    {
        if (underwaterTriangles.Count > 0)
        {
            AddForcesToBoat();
        }
    }

    //Calculating the resistance coefficient based on the velocity of the body
    private float GetResistanceCoefficient( float velocity, float length, float nu)
    {
        //calculate reynolds number
        float rn = (velocity*length)/nu;
        //calculate resistance coefficient for each time step
        float cf = 0.075f/Mathf.Pow((Mathf.Log10(rn)) - 2, 2);
        return cf;
    }

    /// <summary>
    /// Main function that calculates the forces necessary based on the underwater triangles
    /// </summary>
    private void AddForcesToBoat()
    {
        int i = 0;
        float totalArea = 0f;

        //iterate through the list of triangles
        while (i < underwaterTriangles.Count)
        {
            //get positions of 3 vertices per triangle
            Vector3 vertice_1_pos = underwaterVertices[underwaterTriangles[i]];
            i++;
            Vector3 vertice_2_pos = underwaterVertices[underwaterTriangles[i]];
            i++;
            Vector3 vertice_3_pos = underwaterVertices[underwaterTriangles[i]];
            i++;

            Vector3 centerPoint = (vertice_1_pos + vertice_2_pos + vertice_3_pos)/3f;
            float distance_to_surface = Mathf.Abs((float) DistanceToWater(centerPoint));

            //convert all points to world coordinates
            centerPoint = transform.TransformPoint(centerPoint);
            vertice_1_pos = transform.TransformPoint(vertice_1_pos);
            vertice_2_pos = transform.TransformPoint(vertice_2_pos);
            vertice_3_pos = transform.TransformPoint(vertice_3_pos);

            //crossproduct is a normal to the triangle here
            Vector3 crossProduct = Vector3.Cross(vertice_2_pos - vertice_1_pos, vertice_3_pos - vertice_1_pos).normalized;
            
            //area of a triangle using Heron's formula
            float a = Vector3.Distance(vertice_1_pos, vertice_2_pos);
            float b = Vector3.Distance(vertice_2_pos, vertice_3_pos);
            float c = Vector3.Distance(vertice_3_pos, vertice_1_pos);
            float s = (a + b + c)/2;
            float area_heron = Mathf.Sqrt(s*(s - a)*(s - b)*(s - c));
            float area = area_heron;

            //calculate the velocity at the center of the triangle
            Vector3 triVelocity = GetTriangleVelocity(boatRB, centerPoint);

            //add buoyant force (basic one, always enabled)
            AddBuoyancy(distance_to_surface,area,crossProduct,centerPoint);

            //controls for other 3 resistance forces, can be toggled on and off
            if (Slam)
            {
                if(triVelocity.y != 0f)
                AddPrimitiveSlamForce(triVelocity, centerPoint,crossProduct);
            }
            if (PressureDrag)
            {
                //calculate CosTheta to see if a triangle is submerging or emerging
                float cosTheta = Vector3.Dot(triVelocity, crossProduct);
                //add speed of water to make the boat jump better on the waves
                AddPressureDragForce(centerPoint,cosTheta,triVelocity.magnitude+water.Speed,area,crossProduct,CPD1,CPD2,CSD1,CSD2,boatRB.velocity.magnitude,fp,fs);
            }
            if (Viscous)
            {
                AddViscousResistance(resistanceCoefficient,crossProduct,triVelocity,1000,area,centerPoint);
            }
        }
    }

    //Pressure drag force  for stabilization and reduced oscilation
    private void AddPressureDragForce(Vector3 centerPoint, float cosTheta, float vi, float area, Vector3 normal, float CPD1, float CPD2, float CSD1, float CSD2, float vr, float fp, float fs)
    {
        //formulas based on costheta
        Vector3 pressureDrag = Vector3.zero;
        if (cosTheta > 0)
        {
            pressureDrag = -(CPD1 * (vi / vr) + CPD2 * ((vi * vi) / (vr * vr))) * area * (Mathf.Pow(cosTheta, fp)) * normal;
            boatRB.AddForceAtPosition(pressureDrag,centerPoint);
            if (DebugForce) Debug.DrawRay(centerPoint, pressureDrag, Color.blue);
        }
        else
        {
            
            pressureDrag = (CSD1 * (vi / vr) + CSD2 * ((vi * vi) / (vr * vr))) * area * (Mathf.Pow(Mathf.Abs(cosTheta), fs)) * normal;
            boatRB.AddForceAtPosition(pressureDrag, centerPoint);
            if (DebugForce) Debug.DrawRay(centerPoint, pressureDrag, Color.blue);
             
        }   
    }

    //Viscous resistance, mostly influential at higher speeds
    private void AddViscousResistance(float Cf, Vector3 normal, Vector3 velocity, float rho, float area, Vector3 centerPoint)
    {
        //projection of the velocity on our triangle
        Vector3 tangentialVelocity = Vector3.Cross(velocity, (Vector3.Cross(normal, velocity) / velocity.magnitude)) / velocity.magnitude;
        //we apply the projection to the speed of the triangle
        Vector3 vfi = velocity.magnitude*(tangentialVelocity*-1f);
        //calculate viscous resistance
        Vector3 viscous = 0.5f*rho*Cf*area*vfi*vfi.magnitude;
        boatRB.AddForceAtPosition(viscous, centerPoint);    
        if (DebugForce) Debug.DrawRay(centerPoint, viscous, Color.yellow);
    }

    //Simple version of a slam force. Fairly accurate on flat planes, less accurate on waves
    private void AddPrimitiveSlamForce(Vector3 triVelocity, Vector3 centerPoint, Vector3 normal)
    {
        Vector3 slam = -triVelocity*50;
        boatRB.AddForceAtPosition(slam,centerPoint);
        if (DebugForce) Debug.DrawRay(centerPoint, slam, Color.green);
    }

    //Buoyant force that makes the boat float
    private void AddBuoyancy(float distance_to_surface, float area, Vector3 crossProduct, Vector3 centerPoint)
    {
        //The hydrostatic force dF = rho * g * z * dS * n
        Vector3 F = 1000f*Physics.gravity.y*distance_to_surface*area*crossProduct;
        //F = new Vector3(0f, (F.y), 0f);
        boatRB.AddForceAtPosition(F,centerPoint);
        if (DebugForce) Debug.DrawRay(centerPoint, F / 100, Color.red);
    }

    //here is where we obtain the new mesh consisting only of triangles under water
    public void GenerateUnderwaterMesh()
    {
        //store data here
        underwaterVertices = new List<Vector3>();
        underwaterTriangles = new List<int>();

        //loop through triangles
        int i = 0;
        while (i < originalTrianglesArray.Length)
        {
            //get 3 vertices and measure their distance to water
            Vector3 vertice_1_pos = originalVerticesArray[originalTrianglesArray[i]];
            float? distance1 = DistanceToWater(vertice_1_pos); //store in nullable float for the case there is no water
            i++;

            Vector3 vertice_2_pos = originalVerticesArray[originalTrianglesArray[i]];
            float? distance2 = DistanceToWater(vertice_2_pos);
            i++;

            Vector3 vertice_3_pos = originalVerticesArray[originalTrianglesArray[i]];
            float? distance3 = DistanceToWater(vertice_3_pos);
            i++;

            //if all are positive, continue to next triangle
            if (distance1 > 0 && distance2 > 0 && distance3 > 0)
            {
                continue;
            }

            //go to next triangle if there is no water
            if (distance1 == null || distance2 == null || distance3 == null)
            {
                continue;
            }

            //Create 3 new instances of distance to compare and sort
            //Distance is a comparable class that we will use to compare distances between points
            Distance distance1OBJ = new Distance();
            Distance distance2OBJ = new Distance();
            Distance distance3OBJ = new Distance();

            distance1OBJ.distance = (float) distance1; //we know that distance floats are not null anymore
            distance1OBJ.name = "one";
            distance1OBJ.verticePos = vertice_1_pos;

            distance2OBJ.distance = (float) distance2;
            distance2OBJ.name = "two";
            distance2OBJ.verticePos = vertice_2_pos;

            distance3OBJ.distance = (float)distance3;
            distance3OBJ.name = "three";
            distance3OBJ.verticePos = vertice_3_pos;

            List<Distance> allDistancesList = new List<Distance>();
            allDistancesList.Add(distance1OBJ);
            allDistancesList.Add(distance2OBJ);
            allDistancesList.Add(distance3OBJ);

            //sort them and reverse them so that the highert point goes first
            allDistancesList.Sort();
            allDistancesList.Reverse();

            //Everything is underwater
            if (allDistancesList[0].distance < 0f && allDistancesList[1].distance < 0f && allDistancesList[2].distance < 0f)
            {
                //add unsorted coordicates to mesh
                AddCoordinateToMesh(distance1OBJ.verticePos);
                AddCoordinateToMesh(distance2OBJ.verticePos);
                AddCoordinateToMesh(distance3OBJ.verticePos);
            }
            //One vertex is above the water, the rest is under water. We can check like this because the vertex positions in distance are sorted
            else if (allDistancesList[0].distance > 0f && allDistancesList[1].distance < 0f && allDistancesList[2].distance < 0f)
            {
                // 3 Vertices H, M, L representing the heights of the 3 points in a triangle
                Vector3 H = allDistancesList[0].verticePos;

                //Left of H is M
                //Right of H is L

                //Find the name of M, we can count them because of clockwise winding order
                string M_name = "temp";
                if (allDistancesList[0].name == "one")
                {
                    M_name = "three";
                }
                else if (allDistancesList[0].name == "two")
                {
                    M_name = "one";
                }
                else
                {
                    M_name = "two";
                }

                //Get water heights for the highest point
                float h_H = allDistancesList[0].distance;
                float h_M = 0f;
                float h_L = 0f;

                Vector3 M = Vector3.zero;
                Vector3 L = Vector3.zero;

                //establish the other two labels based on the name of the M vertex
                if (allDistancesList[1].name == M_name)
                {
                    M = allDistancesList[1].verticePos;
                    L = allDistancesList[2].verticePos;

                    h_M = allDistancesList[1].distance;
                    h_L = allDistancesList[2].distance;
                }
                else
                {
                    M = allDistancesList[2].verticePos;
                    L = allDistancesList[1].verticePos;

                    h_M = allDistancesList[2].distance;
                    h_L = allDistancesList[1].distance;
                }

                //Calculating the triangle cutting, directly from gamasutra article
                //I_M
                Vector3 MH = H - M;
                float t_M = -h_M/(h_H - h_M);
                Vector3 MI_M = t_M*MH;
                Vector3 I_M = MI_M + M;
                //I_L
                Vector3 LH = H - L;
                float t_L = -h_L/(h_H - h_L);
                Vector3 LI_L = t_L*LH;
                Vector3 I_L = LI_L + L;

                //with points M, L, I_M, I_L i can create two new triangles
                AddCoordinateToMesh(M);
                AddCoordinateToMesh(I_M);
                AddCoordinateToMesh(I_L);

                AddCoordinateToMesh(M);
                AddCoordinateToMesh(I_L);
                AddCoordinateToMesh(L);
            }
            else if (allDistancesList[0].distance > 0f && allDistancesList[1].distance > 0f && allDistancesList[2].distance < 0f)
            {
                //H and M are above the water
                //So we know which one is L because it is last in the sorted list
                Vector3 L = allDistancesList[2].verticePos;

                //Find the name of H
                string H_name = "temp";
                if (allDistancesList[2].name == "one")
                {
                    H_name = "two";
                }
                else if (allDistancesList[2].name == "two")
                {
                    H_name = "three";
                }
                else
                {
                    H_name = "one";
                }

                //We also need the heights to water
                float h_L = allDistancesList[2].distance;
                float h_H = 0f;
                float h_M = 0f;

                Vector3 H = Vector3.zero;
                Vector3 M = Vector3.zero;

                //establish the other label based on the name of the L vertex 
                if (allDistancesList[1].name == H_name)
                {
                    H = allDistancesList[1].verticePos;
                    M = allDistancesList[0].verticePos;

                    h_H = allDistancesList[1].distance;
                    h_M = allDistancesList[0].distance;
                }
                else
                {
                    H = allDistancesList[0].verticePos;
                    M = allDistancesList[1].verticePos;

                    h_H = allDistancesList[0].distance;
                    h_M = allDistancesList[1].distance;
                }

                //Cutting triangle now that the positions and heights are known
                //J_M
                Vector3 LM = M - L;
                float t_M = -h_L/(h_M - h_L);
                Vector3 LJ_M = t_M*LM;
                Vector3 J_M = LJ_M + L;

                //J_H
                Vector3 LH = H - L;
                float t_H = -h_L/(h_H - h_L);
                Vector3 LJ_H = t_H*LH;
                Vector3 J_H = LJ_H + L;

                // with three points, we can add a triangle
                AddCoordinateToMesh(L);
                AddCoordinateToMesh(J_H);
                AddCoordinateToMesh(J_M);

            }
        }
        

        //Generate the final underwater mesh
        UnderWaterMesh.Clear();
        UnderWaterMesh.name = "Underwater Mesh";
        UnderWaterMesh.vertices = underwaterVertices.ToArray();
        //UnderWaterMesh.uv = uvs.ToArray();
        UnderWaterMesh.triangles = underwaterTriangles.ToArray();

        //Ensure the bounding volume is correct
        UnderWaterMesh.RecalculateBounds();
        //Update the normals to reflect the change
        UnderWaterMesh.RecalculateNormals();
    }

    //add vertex position and triangle indices to their respective array
    void AddCoordinateToMesh(Vector3 coord)
    {
        underwaterVertices.Add(coord);
        underwaterTriangles.Add(underwaterVertices.Count - 1);
    }

    //Get distance to our dynamic water level from the water script
    public float? DistanceToWater(Vector3 position)
    {
        Vector3 globalVerticePosition = transform.TransformPoint(position);
        float? y_pos = water.WaterArray[(int)globalVerticePosition.x*10].position.y;
        return globalVerticePosition.y - y_pos;
    }

    //get velocity of a triangle center based on the velocities of the whole rigid body.
    public Vector3 GetTriangleVelocity(Rigidbody boatRB, Vector3 triangleCenter)
    {
        //The connection formula for velocities
        // v_A = v_B + omega_B cross r_BA
        // v_A - velocity in point A
        // v_B - velocity in point B
        // omega_B - angular velocity in point B
        // r_BA - vector between A and B

        Vector3 v_B = boatRB.velocity;

        Vector3 omega_B = boatRB.angularVelocity;

        Vector3 r_BA = triangleCenter - boatRB.worldCenterOfMass;

        Vector3 v_A = v_B + Vector3.Cross(omega_B, r_BA);

        return v_A;
    }
}

/// <summary>
/// Class that compares the distances of the values
/// </summary>
public class Distance : IComparable<Distance>
{
    public float distance;
    public string name;
    public Vector3 verticePos;

    public int CompareTo(Distance other)
    {
        return this.distance.CompareTo(other.distance);
    }
}