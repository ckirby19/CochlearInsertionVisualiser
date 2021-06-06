using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class StewartPlatform : MonoBehaviour
{
    #region publicVariables
    public float platformRadius;
    public float baseRadius;
    public float homeHeight; //Starting height
    public float platformThickness;
    [Range(-30f,30f)] public float Theta = 0,Phi = 0,Psi = 0;
    [Range(-2f,2f)] public float moveX = 0,moveY = 0,moveZ = 0;
    public Transform baseTransform, platformTransform;
    #endregion

    #region privateVariables
    private Mesh baseMesh,platformMesh;
    private Vector3[] baseVertices, platformVertices;
    private int[] baseTriangles, platformTriangles;
    private int total,oneSideTri;
    private int numPoints;
    private int nGon;
    private Vector3 thicknessPoint;
    

    //Vectors for controlling the motion of the platform. Each will have a total of nGon vector3's
    private Vector3[] startingLengthMatrices,lengthMatrices, baseAnchorMatrices, platformAnchorMatrices, rotatedPlatformAnchorMatrices, rotationMatrices;
    //Vector3's that will be put inside of Vector3[]
    private Vector3 lengthMatrix, baseAnchorMatrix, platformAnchorMatrix, centreMatrix;
    //These vector3's are common to all nGon points
    private Vector3[] rotX, rotY, rotZ;

    #endregion

    void Start()
    {
        // baseTransform = this.transform.Find("Base");
        baseMesh = baseTransform.GetComponent<MeshFilter>().mesh;
        baseMesh.MarkDynamic();

        // platformTransform = this.transform.Find("Platform");
        platformMesh = platformTransform.GetComponent<MeshFilter>().mesh;
        platformMesh.MarkDynamic();

        
        rotX = new Vector3[3];
        rotY = new Vector3[3];
        rotZ = new Vector3[3];
        
        nGon = 6;
        numPoints = 2;

        lengthMatrices = new Vector3[nGon];
        startingLengthMatrices = new Vector3[nGon];
        baseAnchorMatrices = new Vector3[nGon];
        platformAnchorMatrices = new Vector3[nGon];
        rotationMatrices = new Vector3[nGon];
        rotatedPlatformAnchorMatrices = new Vector3[nGon];

        total = nGon*numPoints; 
        oneSideTri = nGon*(numPoints-1)*6; 

        baseTriangles = new int[oneSideTri*2];
        platformTriangles = new int[oneSideTri*2];

        //Double so that a hard edge can be created
        baseVertices = new Vector3[2*total];
        platformVertices = new Vector3[2*total];

        centreMatrix = platformTransform.position - baseTransform.position;

        thicknessPoint = new Vector3(0,platformThickness,0);

        CallUpdate(); 
        RotateMovePlatform();  

        for (int n=0;n<nGon;n++){
            startingLengthMatrices[n] = centreMatrix + rotatedPlatformAnchorMatrices[n] - baseAnchorMatrices[n];
        }
    }

    void RotateMovePlatform(){
        // https://core.ac.uk/download/pdf/322824733.pdf
        platformTransform.transform.position = new Vector3(moveX,moveY+homeHeight,moveZ);

        rotX = new [] { new Vector3(1f,0f,0f), 
                        new Vector3(0f,Mathf.Cos(Mathf.Deg2Rad*Theta),-Mathf.Sin(Mathf.Deg2Rad*Theta)),
                        new Vector3(0f,Mathf.Sin(Mathf.Deg2Rad*Theta),Mathf.Cos(Mathf.Deg2Rad*Theta))
                        };
        rotY = new [] { new Vector3(Mathf.Cos(Mathf.Deg2Rad*Psi),-Mathf.Sin(Mathf.Deg2Rad*Psi),0f),
                        new Vector3(Mathf.Sin(Mathf.Deg2Rad*Psi),Mathf.Cos(Mathf.Deg2Rad*Psi),0f),
                        new Vector3(0f,0f,1f)
                        };
        rotZ = new [] { new Vector3(Mathf.Cos(Mathf.Deg2Rad*Phi),0f,Mathf.Sin(Mathf.Deg2Rad*Phi)), 
                        new Vector3(0f,1f,0f),
                        new Vector3(-Mathf.Sin(Mathf.Deg2Rad*Phi),0f,Mathf.Cos(Mathf.Deg2Rad*Phi))
                        };
        
        Quaternion QuatX = RotationMatrixToQuaternion(rotX);
        Quaternion QuatY = RotationMatrixToQuaternion(rotY);
        Quaternion QuatZ = RotationMatrixToQuaternion(rotZ);

        Quaternion Total = QuatX*QuatY*QuatZ;

        for (int i=0;i<nGon;i++){
            rotatedPlatformAnchorMatrices[i] = Total * platformAnchorMatrices[i];
        }

        
    }

    Quaternion RotationMatrixToQuaternion(Vector3[] matrix){
        // Mike Day, Insomniac Games https://d3cw3dd2w32x2b.cloudfront.net/wp-content/uploads/2015/01/matrix-to-quat.pdf
        float m00 = matrix[0][0];
        float m01 = matrix[0][1];
        float m02 = matrix[0][2];
        float m10 = matrix[1][0];
        float m11 = matrix[1][1];
        float m12 = matrix[1][2];
        float m20 = matrix[2][0];
        float m21 = matrix[2][1];
        float m22 = matrix[2][2];

        float t;
        Quaternion q;

        if (m22 < 0){
            if (m00 > m11){      
                t = 1 + m00 - m11 - m22;
                q = new Quaternion( t, m20+m02, m01+m10, m12-m21 );
                // q = quat( t, m01+m10, m20+m02, m12-m21 );
            }
            else{
                t = 1 - m00 + m11 - m22;
                q = new Quaternion( m01+m10, m12+m21, t, m20-m02 );
                // q = quat( m01+m10, t, m12+m21, m20-m02 );
            }
        }
        else{
            if (m00 < -m11){
                t = 1 - m00 - m11 + m22;
                q = new Quaternion( m20+m02, t, m12+m21, m01-m10 );
                // q = quat( m20+m02, m12+m21, t, m01-m10 );
            }
            else{
        
                t = 1 + m00 + m11 + m22;
                q = new Quaternion( m12-m21, m01-m10, m20-m02,  t );
                // q = quat( m12-m21, m20-m02, m01-m10, t );
            }
        }
        // q *= 0.5f / Mathf.Sqrt(t);

        return q.normalized;


    }

    //When values in inspector changed
    void OnValidate(){
        RotateMovePlatform();
        CallUpdate();
        thicknessPoint = new Vector3(0,platformThickness,0);
    }

    void FixedUpdate(){
        CallUpdate();

        for (int n=0;n<nGon;n++){
            lengthMatrices[n] = centreMatrix + rotatedPlatformAnchorMatrices[n] - baseAnchorMatrices[n];
            

            
        }
        float relativeLength = (lengthMatrices[1].sqrMagnitude / startingLengthMatrices[1].sqrMagnitude);
        Debug.Log("Relative length of element " + 1.ToString() + " = " + relativeLength.ToString());
        centreMatrix = platformTransform.position - baseTransform.position;
        
    }

    void CallUpdate(){
        if (platformMesh != null && baseMesh != null && Application.isPlaying){
            MovePlatformVertices();
            UpdateMesh();
        }
    }

    void MovePlatformVertices(){
        
        int v=0;

        for (int i=0;i<numPoints;i++){
            
            for (int n=0;n<nGon;n++){
                baseAnchorMatrix = new Vector3(baseRadius*Mathf.Cos(2*Mathf.PI*n/nGon),0,baseRadius*Mathf.Sin(2*Mathf.PI*n/nGon));
                baseAnchorMatrices[n] = baseAnchorMatrix;
                baseVertices[v] = baseVertices[v+nGon*numPoints] = baseAnchorMatrix + i*thicknessPoint;

                platformAnchorMatrix = new Vector3(platformRadius*Mathf.Cos(2*Mathf.PI*n/nGon),0,platformRadius*Mathf.Sin(2*Mathf.PI*n/nGon));
                platformAnchorMatrices[n] = platformAnchorMatrix;
                platformVertices[v] = platformVertices[v+nGon*numPoints] = rotatedPlatformAnchorMatrices[n] + i*thicknessPoint;

                v+=1;
            }
        }
        DefineTriangles();
    }

    void DefineTriangles(){
        int v=0;
        int v_adj = 0;
        int t=0;
        for (int i=0;i<numPoints-1;i++){
            for (int n=0;n<nGon;n++){
                // if (n%2 == 0){
                //     //Then We use the normal vertices
                //     v_adj = v;
                // }
                // else{
                //     //Use increased vertices
                //     v_adj = v + nGon*numPoints;
                // }
                v_adj = v;
                //Defining the sides of the hexagon
                baseTriangles[t] = v_adj;
                baseTriangles[t+1] =  (v_adj+1)%(nGon)+nGon*i;
                baseTriangles[t+2] = (v_adj+nGon);
                baseTriangles[t+3] = baseTriangles[t+2] + nGon*numPoints;
                baseTriangles[t+4] = baseTriangles[t+1] + nGon*numPoints;
                baseTriangles[t+5] = (v_adj+1)%(nGon) + nGon*(1+i);
                baseTriangles[t+oneSideTri] = (v_adj+1)%(nGon)+nGon*i;
                baseTriangles[t+1+oneSideTri] = v_adj;
                baseTriangles[t+2+oneSideTri] = (v_adj+1)%(nGon) + nGon*(1+i);
                baseTriangles[t+3+oneSideTri] = baseTriangles[t+2+oneSideTri] + nGon*numPoints;
                baseTriangles[t+4+oneSideTri] = baseTriangles[t+1+oneSideTri] + nGon*numPoints;
                baseTriangles[t+5+oneSideTri] = (v_adj+nGon);

                // baseTriangles[t] = v;
                // baseTriangles[t+1] = baseTriangles[t+4] = (v+1)%(nGon)+nGon*i;
                // baseTriangles[t+2] = baseTriangles[t+3] = (v+nGon);
                // baseTriangles[t+5] = (v+1)%(nGon) + nGon*(1+i);
                // baseTriangles[t+oneSideTri] = (v+1)%(nGon)+nGon*i;
                // baseTriangles[t+1+oneSideTri] = baseTriangles[t+4+oneSideTri] = v;
                // baseTriangles[t+2+oneSideTri] = baseTriangles[t+3+oneSideTri] = (v+1)%(nGon) + nGon*(1+i);
                // baseTriangles[t+5+oneSideTri] = (v+nGon);

                
                platformTriangles[t] = v;
                platformTriangles[t+1] = platformTriangles[t+4] = (v+1)%(nGon)+nGon*i;
                platformTriangles[t+2] = platformTriangles[t+3] = (v+nGon);
                platformTriangles[t+5] = (v+1)%(nGon) + nGon*(1+i);
                platformTriangles[t+oneSideTri] = (v+1)%(nGon)+nGon*i;
                platformTriangles[t+1+oneSideTri] = platformTriangles[t+4+oneSideTri] = v;
                platformTriangles[t+2+oneSideTri] = platformTriangles[t+3+oneSideTri] = (v+1)%(nGon) + nGon*(1+i);
                platformTriangles[t+5+oneSideTri] = (v+nGon);


                
                v+=1;
                t+=6;
            }
        }

    }

    void UpdateMesh(){
        baseMesh.Clear(); //Clear out the current buffer 
        baseMesh.vertices = baseVertices;
        baseMesh.triangles = baseTriangles;
        baseMesh.RecalculateTangents();
        // baseMesh.RecalculateNormals(); //Calculate normal based on triangle vertices are part of

        platformMesh.Clear(); //Clear out the current buffer 
        platformMesh.vertices = platformVertices;
        platformMesh.triangles = platformTriangles;
        platformMesh.RecalculateTangents();
        // platformMesh.RecalculateNormals(); //Calculate normal based on triangle vertices are part of
    }

    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.blue;
        for (int n=0;n<nGon;n++){
            Gizmos.DrawLine(baseAnchorMatrices[n]+thicknessPoint, baseAnchorMatrices[n] + lengthMatrices[n]);
        }
    }
}
