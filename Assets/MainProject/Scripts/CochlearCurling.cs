using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
// https://answers.unity.com/questions/550262/how-to-follow-curved-path.html
// https://answers.unity.com/questions/392606/line-drawing-how-can-i-interpolate-between-points.html
// https://www.reddit.com/r/gamedev/comments/96f8jl/if_you_are_making_an_rpg_you_need_to_know_the/
// https://danielilett.com/2019-09-08-unity-tips-3-interpolation/

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CochlearCurling : MonoBehaviour
{
    #region variables
    public bool getData = false;
    [Header("Geometry settings")]
    public float scale = 1.0f;
    [Range(3,100)]public int nGon = 3;
    // private float stepSize = 0.1f;
    public float stepSize = 25f;
    public bool evenDistribution = false;
    [Range(0,18)]public int criticalPoint;

    [Header("Insertion Settings")]
    public bool automateInsertion;
    public bool  automateRetraction;
    public bool ThreeDCurl;
    [Range(0f,360f)] public float electrodeRoll = 0f;
    [Range(-0.1f,0.1f)]public float electrodeInsertionSpeed = 0f;
    [Range(10f,100f)]public float timeToCurl = 20f; 
    [Range(-19f,19f)] public float angleOfInsertion = 0f;

    [Range(0f,1f)] public float interp = 1f; //for interpolation of angle and pos
    public float curlingModifier = 0f;
    private float previousInterp, modifiedInterp;

    //For mesh
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private int total,oneSideTri;
    private int numPoints;
    //Hit Detection
    private Quaternion qAngle,insertionAngle;
    private Vector3 hitDirection;
    private float dist = Mathf.Infinity;
   
    //Defined from (Clark et.al, 2011)
    private float R,theta,theta_mod;
    private float s= 3f,A= 3.762f,B= 0.001317f,C= 7.967f,D= 0.1287f,E= 0.003056f;
    private float THETA_0 = 5f,THETA_1 = 10.3f;
    // private float THETA_END = 480f;
    private float THETA_END = 380f;
    // private float THETA_END = 910.3f;
    private float cochlearLength, totalLength;
    //Unrolling Mesh
    private Vector3 basePoint,origin,planeNormal;
    private Vector3[] endPos,varPos;
    private Quaternion[] varRotations,endRotations;
    private Quaternion varRot,startRot,accumulate,currentToPrev;
    private Vector3 pivot,prevDir,currentDir,currentDefinePos,prevDefinePos,currentCurlingPoint,prevCurlingPoint,nGonPoint;
    private float rotatePolygonPlane;
    private GameObject springMassObject, finalMassObject, pivotParent;
    private GameObject[] massList;
    private int layerMask;
    private Collider[] hitColliders;
    private Vector3 scaleVector;
    private float previousAngleOfInsertion;

    #endregion
    void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.MarkDynamic();
        Time.fixedDeltaTime = 0.02f; //Seconds
        layerMask = 1 << 3; //3 is the layer number for the cochlear model
        scaleVector = new Vector3(scale,scale,scale);
        
        numPoints = (int)Mathf.Ceil((THETA_END-THETA_1)/stepSize);
        
        prevDir = Vector3.up;
        origin = new Vector3(0,0,0);
        endRotations = new Quaternion[numPoints-1];
        varRotations = new Quaternion[numPoints-1];

        endPos = new Vector3[numPoints];
        varPos = new Vector3[numPoints];


        massList = new GameObject[numPoints];
        
        total = nGon*numPoints;
        oneSideTri = nGon*(numPoints-1)*6; 

        DefineCochlearPath();   

        previousInterp = interp;  

        this.transform.RotateAround(finalMassObject.transform.position,Vector3.forward,angleOfInsertion - previousAngleOfInsertion);
        previousAngleOfInsertion = angleOfInsertion;

    }



    void DefineCochlearPath(){
        for (int i=0;i<numPoints;i++){
            theta = THETA_1+stepSize*i;
            if (theta<=99.9){
                R = C*(1-D*Mathf.Log(theta-THETA_0));
            }
            else{
                theta_mod = 0.0002f*theta*theta+.98f*theta;
                R = A*Mathf.Exp(-B*theta_mod);
            }

            if (ThreeDCurl){
                currentDefinePos = new Vector3(-s*R*Mathf.Sin(theta*Mathf.PI/180f),-s*R*Mathf.Cos(theta*Mathf.PI/180f),s*E*(theta-THETA_1));

            }
            else{
                currentDefinePos = new Vector3(-s*R*Mathf.Sin(theta*Mathf.PI/180f),-s*R*Mathf.Cos(theta*Mathf.PI/180f),0);
            }

            if (i==0){
                origin = currentDefinePos;
            }
            currentDefinePos -= origin;

            // if (currentDefinePos.x <0){
            //     currentDefinePos.x = 0;
            // }

            endPos[i] = currentDefinePos;

            


            if (i==numPoints-1){
                finalMassObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                finalMassObject.name = "MasterMass";
                finalMassObject.transform.localScale = 1.8f*scaleVector;
                finalMassObject.AddComponent<Rigidbody>();
                finalMassObject.GetComponent<Rigidbody>().drag = 1f;
                finalMassObject.GetComponent<Rigidbody>().useGravity = false;
                finalMassObject.GetComponent<Renderer>().material.SetColor("_Color",Color.red);
                // finalMassObject.GetComponent<Renderer>().material.enableInstancing = true;
                finalMassObject.transform.SetParent(this.transform);
                massList[i] = finalMassObject;
            }
            else{
                springMassObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                springMassObject.name = "Mass" + i.ToString();
                springMassObject.transform.localScale = 1.8f*scaleVector;
                springMassObject.AddComponent<Rigidbody>();
                springMassObject.GetComponent<Rigidbody>().drag = 1f;
                springMassObject.GetComponent<Rigidbody>().useGravity = false;
                springMassObject.GetComponent<Renderer>().material.SetColor("_Color",Color.red);
                // springMassObject.GetComponent<Renderer>().material.enableInstancing = true;
                springMassObject.transform.SetParent(this.transform);
                massList[i] = springMassObject;

            }

            varPos[0] = origin;

            if (i>0){
                totalLength += (currentDefinePos - prevDefinePos).magnitude;
                currentDir = endPos[i] - endPos[i-1];
                float segmentLength = currentDir.magnitude;
                cochlearLength += segmentLength;
                Vector3 path = new Vector3(0,i*segmentLength,0);
                varPos[i] = origin + path;

                currentToPrev = Quaternion.FromToRotation(currentDir,prevDir);
                endRotations[i-1] = currentToPrev;
                prevDir = currentDir; 

                //Create Configurable Joints
                massList[i].AddComponent<ConfigurableJoint>();
                massList[i].GetComponent<ConfigurableJoint>().connectedBody = massList[i-1].GetComponent<Rigidbody>();
                massList[i].GetComponent<ConfigurableJoint>().xMotion = ConfigurableJointMotion.Locked;
                // massList[i].GetComponent<ConfigurableJoint>().yMotion = ConfigurableJointMotion.Locked;
                massList[i].GetComponent<ConfigurableJoint>().zMotion = ConfigurableJointMotion.Locked;
                massList[i].GetComponent<ConfigurableJoint>().angularXMotion = ConfigurableJointMotion.Locked;
                massList[i].GetComponent<ConfigurableJoint>().angularYMotion = ConfigurableJointMotion.Locked;
                massList[i].GetComponent<ConfigurableJoint>().enableCollision = true;
                
            }

            prevDefinePos = currentDefinePos;
        }
        
        if (evenDistribution){
            for (int i=0;i<numPoints;i++){
                Vector3 point = new Vector3(0,(float)i*cochlearLength/numPoints,0);
                massList[i].transform.localPosition = origin + point;
                if (i>0){
                    float distance = Vector3.Distance(massList[i].transform.localPosition,massList[i-1].transform.localPosition);
                    // totalLength += distance;
                    // Debug.Log("Vector: " + massList[i].transform.localPosition.ToString("F5") + "Distance: " + distance.ToString("F5"));
                    // Vector3 anchorVec = new Vector3(0,distance/2,0);
                    // Vector3 anchorVec = new Vector3(0,1.3f,0);
                    // massList[i].GetComponent<ConfigurableJoint>().anchor = -anchorVec;
                    // massList[i].GetComponent<ConfigurableJoint>().autoConfigureConnectedAnchor = false;
                    // massList[i].GetComponent<ConfigurableJoint>().connectedAnchor = anchorVec;
                }
            }
        }

        else{
            for (int i=0;i<numPoints;i++){
                massList[i].transform.localPosition = varPos[i];
                if (i>0){
                    float distance = Vector3.Distance(massList[i].transform.localPosition,massList[i-1].transform.localPosition);
                    // totalLength += distance;
                    // Debug.Log("Vector: " + massList[i].transform.localPosition.ToString("F5") + "Distance: " + distance.ToString("F5"));
                    // Vector3 anchorVec = new Vector3(0,distance/2,0);
                    // Vector3 anchorVec = new Vector3(0,1.3f,0);
                    // massList[i].GetComponent<ConfigurableJoint>().anchor = -anchorVec;
                    // massList[i].GetComponent<ConfigurableJoint>().autoConfigureConnectedAnchor = false;
                    // massList[i].GetComponent<ConfigurableJoint>().connectedAnchor = anchorVec;
                }
            }
        }

        print(cochlearLength);
        print(totalLength);
        this.transform.Translate(new Vector3(0,-42.3f,0)); 

           

    }

    //When values in inspector changed
    void OnValidate(){
        if (finalMassObject){
            this.transform.RotateAround(finalMassObject.transform.position,Vector3.forward,angleOfInsertion - previousAngleOfInsertion);
            previousAngleOfInsertion = angleOfInsertion;
        }
        
        CallUpdate();
    }

    void Start(){
        
        CallUpdate();
    }

    void FixedUpdate(){
        

        if (automateInsertion){
            automateRetraction = false;
            electrodeInsertionSpeed = cochlearLength/(timeToCurl/Time.fixedDeltaTime);
            interp = Mathf.Clamp(interp-Time.fixedDeltaTime/timeToCurl,0f,1f);
            if (interp <= 0.000001f){
                electrodeInsertionSpeed = 0;
                automateInsertion = false;
            }
        }
        if (automateRetraction){
            automateInsertion = false;
            electrodeInsertionSpeed = -cochlearLength/(timeToCurl/Time.fixedDeltaTime);
            interp = Mathf.Clamp(interp+Time.fixedDeltaTime/timeToCurl,0f,1f);
            if (interp >= 0.9999f){
                electrodeInsertionSpeed = 0;
                automateRetraction = false;
            }
        }

        qAngle = Quaternion.AngleAxis(-electrodeRoll,Vector3.up);
        // pivotParent.transform.rotation = Quaternion.Euler(0,0,angleOfInsertion);
        insertionAngle = Quaternion.AngleAxis(angleOfInsertion,this.transform.forward);
        // finalMassObject.transform.Translate(Vector3.up*electrodeInsertionSpeed*Time.fixedDeltaTime);
        
        // this.transform.Translate(this.transform.up*electrodeInsertionSpeed);
        this.transform.Translate(Vector3.up*electrodeInsertionSpeed);

        if (getData){
            print(finalMassObject.transform.position);
            getData = false;
        }

        CallUpdate();
        

        ShortestDistance();
    }

    void CallUpdate(){
        if (mesh != null && Application.isPlaying){
            MoveCochlearVertices();
            UpdateMesh();
        }
    }

    void ShortestDistance(){
        Vector3 start = transform.position+(vertices[total-1]+vertices[total-1-nGon/2])/2;
        
        RaycastHit hitSphere;
        for (int i=0;i<360;i++){
            Vector3 dic = Quaternion.Euler(0,i,0)*Vector3.right;
            if (Physics.SphereCast(start,0.001f,dic,out hitSphere,100,layerMask)){
                if (hitSphere.distance<dist){
                    dist = hitSphere.distance;
                    hitDirection = dic;
                }
            }
        }
        Debug.DrawRay(start,hitDirection*dist,Color.red);
        // if(dist<scale/2){
        //     interp = Mathf.Clamp(interp-Time.fixedDeltaTime/timeToCurl,0f,1f);

        // }
        // dist = Mathf.Infinity;
    }


    void MoveCochlearVertices(){
        vertices = new Vector3[total];
        int v=0;

        startRot = new Quaternion(0,0,0,1);
        varRot = startRot;
        prevCurlingPoint = Vector3.zero;
        
        for (int i=0;i<numPoints;i++){
            if (i>0){
                if (i>criticalPoint){
                    //Why this all get fucked up if I do the automatic insertion procedure??
                    float v1 = (i);
                    float mod = curlingModifier * v1 / (numPoints - 1);
                    if (previousInterp>interp){
                        //Then we are curling
                        modifiedInterp = Mathf.Clamp(interp*(1+mod),0,1);
                    }
                    else{
                        //Then we are uncurling
                        modifiedInterp = Mathf.Clamp(interp*(1-mod),0,1);
                    }
                    varRotations[i-1] = Quaternion.Slerp(startRot,endRotations[i-1],modifiedInterp);
                }
                else {
                    varRotations[i-1] = Quaternion.Slerp(startRot,endRotations[i-1],interp);
                }
                
                
            
                varRot *= varRotations[i-1];

                currentCurlingPoint = endPos[i];
                pivot = endPos[i-1];

                currentCurlingPoint -= pivot;
                currentCurlingPoint = varRot*currentCurlingPoint;
                currentCurlingPoint += prevCurlingPoint;

                varPos[i] = currentCurlingPoint;
                
            }
            
            basePoint = varPos[i];
            
            Vector3 rotatedBasePoint = qAngle * basePoint;
            // basePoint = qAngle * basePoint;

            // massList[i].transform.localPosition = rotatedBasePoint;
            Vector3 forceDir = (rotatedBasePoint - massList[i].transform.localPosition);
            massList[i].GetComponent<Rigidbody>().AddForce(forceDir);

            // massList[i].GetComponent<Rigidbody>().velocity = forceDir;
            planeNormal = (basePoint-prevCurlingPoint);
            rotatePolygonPlane = Vector3.SignedAngle(Vector3.up,planeNormal,Vector3.forward);
            // Vector3 shift = new Vector3(0,massList[i].transform.localScale.y,0);
            for (int n=0;n<nGon;n++){
                nGonPoint = new Vector3(scale*Mathf.Cos(2*Mathf.PI*n/nGon),0,scale*Mathf.Sin(2*Mathf.PI*n/nGon));
                nGonPoint = Quaternion.AngleAxis(rotatePolygonPlane,Vector3.forward)*nGonPoint;

                vertices[v] = massList[i].transform.localPosition + qAngle*nGonPoint;
                // if (i==0){
                //     vertices[v] -= shift; 
                // }
                // if (i==numPoints-1){
                //     vertices[v] += shift;
                // }
                // vertices[v] = rotatedBasePoint + qAngle*nGonPoint;
                v+=1;
            }
            prevCurlingPoint = basePoint;
        }

        previousInterp = interp;

        DefineTriangles();
    }

    void DefineTriangles(){
        triangles = new int[oneSideTri*2];
        int v=0;
        int t=0;
        for (int i=0;i<numPoints-1;i++){
            for (int n=0;n<nGon;n++){

                triangles[t] = v;
                triangles[t+1] = triangles[t+4] = (v+1)%(nGon)+nGon*i;
                triangles[t+2] = triangles[t+3] = (v+nGon);
                triangles[t+5] = (v+1)%(nGon) + nGon*(1+i);

                triangles[t+oneSideTri] = (v+1)%(nGon)+nGon*i;
                triangles[t+1+oneSideTri] = triangles[t+4+oneSideTri] = v;
                triangles[t+2+oneSideTri] = triangles[t+3+oneSideTri] = (v+1)%(nGon) + nGon*(1+i);
                triangles[t+5+oneSideTri] = (v+nGon);


                v+=1;
                t+=6;
            }
        }

    }
    // void MeshCollision() {
    //     // check collisions
    //     for (int i=0;i<numPoints;i++){
    //         hitColliders = Physics.OverlapCapsule()
    //     }
    //     hitColliders = Physics.OverlapSphere(this.transform.position,0.5f,layerMask);
    //     // int numOverlaps = Physics.OverlapSphereNonAlloc(this.transform.position,this.GetComponent<SphereCollider>().radius,hitColliders,layerMask,QueryTriggerInteraction.UseGlobal);
    //     // if (numOverlaps>0){
    //     //     Debug.Log("Here: " + numOverlaps.ToString());
    //     // }
        
    //     // // int numOverlaps = Physics.OverlapBoxNonAlloc(this.transform.position,this.transform.localScale*-0.5f,hitColliders,this.transform.rotation,layerMask,QueryTriggerInteraction.UseGlobal);
    //     for (int i = 0; i < hitColliders.Length; i++) {
    //         var collider = hitColliders[i];
    //         Vector3 otherPosition = collider.gameObject.transform.position;
    //         Quaternion otherRotation = collider.gameObject.transform.rotation;
    //         Vector3 direction;
    //         float distance;

    //         bool overlap = Physics.ComputePenetration(this.GetComponent<SphereCollider>(),this.transform.position,
    //             this.transform.rotation,collider,otherPosition,
    //             otherRotation,out direction,out distance);

    //         if (overlap){
    //             penetrationForce = -10*CollisionForce*(direction*distance);
                
    //             // Vector3 movementDirection = moveDir + penetrationVector;
    //             // Vector3 velocityProjected = Vector3.Project(rb.velocity, -direction);
    //             rb.AddRelativeForce(penetrationForce);

    //             // moveDir = movementDirection.normalized;
    //             // this.transform.position = this.transform.position + penetrationVector;
    //             // velocity -= velocityProjected;
    //             Debug.Log("OnCollisionEnter with " + hitColliders[i].gameObject.name + " penetration vector: " + penetrationForce.ToString("F3"));
    //         }
    //         else
    //         {
    //             Debug.Log("OnCollision Enter with " + hitColliders[i].gameObject.name + " no penetration");
    //         }
    //     }

    // }
    void UpdateMesh(){
        mesh.Clear(); //Clear out the current buffer 
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals(); //Calculate normal based on triangle vertices are part of
    }
}
