using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.Diagnostics;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CochlearCurling : MonoBehaviour
{
    #region resources
    // https://answers.unity.com/questions/550262/how-to-follow-curved-path.html
    // https://answers.unity.com/questions/392606/line-drawing-how-can-i-interpolate-between-points.html
    // https://www.reddit.com/r/gamedev/comments/96f8jl/if_you_are_making_an_rpg_you_need_to_know_the/
    // https://danielilett.com/2019-09-08-unity-tips-3-interpolation/
    #endregion
   
    #region inspectorVariables
    [Header("Geometry settings")]
    public bool GetData;
    public float scale = 0.25f; //Diameter of 0.6mm
    [Range(3,100)]public int nGon = 3;
    // private float stepSize = 0.1f;
    public float stepSize = 25f;
    public bool evenDistribution = false;
    public bool ThreeDCurl;
   
    [Header("Insertion Type")]
    public bool manualInsertion;
    public bool automateInsertion;

    public bool automateInsertionWithSpatial;
    public bool automateNonlinearInsertion;
    public bool  automateRetraction;

    [Header("Insertion Values")]
    [Range(0f,360f)] public float electrodeRoll = 0f;
    [Range(-1,10)]public float electrodeInsertionSpeed = 0;
    public float fastIncrement = 1f;
    public float slowIncrement = 0.01f;
    private float currentIncrement;
    private bool firstZero = false;
    [Range(10f,120f)]public float timeToCurl = 60f; 
    [Range(-21f,21f)] public float angleOfInsertion = 0f;

    [Range(0f,1f)] public float interp = 1f; //for interpolation of angle and pos
    private float previousInterp, modifiedInterp;

    [Header("Curling Modification")]
    [Range(0,72)]public int criticalPoint = 8;
    public float otherMod;
    [Range(0,7f)]public float curlingModifier;
    
    #endregion

    #region savingVariables

    //Saving Data
    [HideInInspector] public List<float> linearVelOutput,RCMOutput,timeOutput;
    [HideInInspector] public Stopwatch myTimer;

    public bool SaveThisData;
    
    #endregion
   
    #region meshVariables
    //For mesh
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private int total,oneSideTri;
    private int numPoints;
    private Vector3 scaleVector;

    #endregion

    #region curlingTrajectoryVariables
    //Defined from (Clark et.al, 2011)
    private float R,theta,theta_mod;
    private float s= 3f,A= 3.762f,B= 0.001317f,C= 7.967f,D= 0.1287f,E= 0.003056f;
    private float THETA_0 = 5f,THETA_1 = 10.3f;
    // private float THETA_END = 480f;
    private float THETA_END = 400f;
    // private float THETA_END = 910.3f;
    private float cochlearLength, totalLength;

    //Using Animation Curves
    private float curveDeltaTime;
    public AnimationCurve otherModAnimation;
    public AnimationCurve curlModAnimation;
    public AnimationCurve critPointAnimation;

    #endregion

    #region unrollingCochlearVariables
    //Unrolling Mesh
    private Vector3 basePoint,origin,planeNormal;
    private Vector3[] endPos,varPos;
    private Quaternion[] varRotations,endRotations, otherVarRotations;
    private Quaternion varRot,startRot,currentToPrev;
    private Vector3 pivot,prevDir,currentDir,currentDefinePos,prevDefinePos,currentCurlingPoint,prevCurlingPoint,nGonPoint;
    private float rotatePolygonPlane;
    private Quaternion qAngle,insertionAngle;
    private float previousAngleOfInsertion;
    private int numSteps;
    private Vector3 velocityVec;
    private Vector3 previousPositionForVec, startingPosition;
    private Vector3 forceDir;
    private bool startedInsertion;
    private Vector3 entrancePosition;

    #endregion

    #region springMassObjects
    private GameObject springMassObject, finalMassObject, pivotParent;
    private GameObject[] massList;
    #endregion

    #region rayCastDetection
    private float distInner, distOuter;
    private Ray ray;
    private RaycastHit hit;
    private int layerMask, rayStopperMask;
    private Vector3 raycastStartPosition;

    private int numHitPoints;

    #endregion

    #region restOfThem
    private float wallScalar = 2.0f, prevWallScalar = 2.0f, bestInterp = 1.0f;

    // Frame rest test
    
    int nonConstantFrames = 0;
    int fpsQty = 0;
    float currentAvgFPS = 0;



    #endregion

    
    void Awake()
    {

        mesh = GetComponent<MeshFilter>().mesh;
        mesh.MarkDynamic();
        Time.fixedDeltaTime = 0.02f; //0.02 Seconds
        layerMask = 1 << 3; //3 is the layer number for the cochlear model
        rayStopperMask = 1 << 6; //6 is the layer number for the ray stopper
        scaleVector = new Vector3(scale,scale,scale);
        
        numPoints = (int)Mathf.Ceil((THETA_END-THETA_1)/stepSize);
        UnityEngine.Debug.Log("Num Points: " + numPoints.ToString());
        
        prevDir = Vector3.up;
        entrancePosition = GameObject.Find("StartingPoint").transform.position;
        this.transform.position = entrancePosition;
        // UnityEngine.Debug.Log("START " + entrancePosition.x.ToString());
        endRotations = new Quaternion[numPoints-1];
        varRotations = new Quaternion[numPoints-1];
        otherVarRotations = new Quaternion[numPoints-1];

        endPos = new Vector3[numPoints];
        varPos = new Vector3[numPoints];

        massList = new GameObject[numPoints];
        
        total = nGon*numPoints;
        oneSideTri = nGon*(numPoints-1)*6; 

        vertices = new Vector3[total];
        triangles = new int[oneSideTri*2];

        DefineCochlear();  
        Vector3 shift = new Vector3(0,-cochlearLength,0);
        Vector3 shiftedStartPos = entrancePosition + shift; 
        this.transform.position = shiftedStartPos;

        // Move top to bottom so that insertion can start correctly
        // this.transform.Translate(new Vector3(0,-cochlearLength,0));

        previousInterp = interp;  

        numSteps = 0;

        // this.transform.RotateAround(finalMassObject.transform.position,Vector3.forward,angleOfInsertion - previousAngleOfInsertion);
        this.transform.RotateAround(massList[massList.Length-1].transform.position,Vector3.forward,angleOfInsertion - previousAngleOfInsertion);
        previousAngleOfInsertion = angleOfInsertion;

        myTimer = new Stopwatch();




        velocityVec = new Vector3(0,1,0);

        previousPositionForVec = this.transform.position;

        startingPosition = this.transform.position;
        UnityEngine.Debug.Log(startingPosition);

        this.gameObject.AddComponent<Rigidbody>();
        this.gameObject.GetComponent<Rigidbody>().drag = 1f;
        this.gameObject.GetComponent<Rigidbody>().useGravity = false;

        startedInsertion = false;

        //Adding Animation Key Values
        //Crit Point
        // critPointAnimation.AddKey(0,21);
        // critPointAnimation.AddKey(3/12f*timeToCurl,20);

        //Other Mod
        otherModAnimation.AddKey(0,0);
        otherModAnimation.AddKey(1/12f*timeToCurl,0.13f);
        otherModAnimation.AddKey(2/12f*timeToCurl,0.18f);
        otherModAnimation.AddKey(3/12f*timeToCurl,0.18f);
        // otherModAnimation.AddKey(1/12f*timeToCurl,10f);
        // otherModAnimation.AddKey(2/12f*timeToCurl,14.5f);
        // otherModAnimation.AddKey(3/12f*timeToCurl,14.5f);

        
        otherModAnimation.AddKey(4/12f*timeToCurl,0.27f);
        otherModAnimation.AddKey(5/12f*timeToCurl,0.41f);
        otherModAnimation.AddKey(6/12f*timeToCurl,0.4f);
        otherModAnimation.AddKey(7/12f*timeToCurl,0.4f);
        otherModAnimation.AddKey(8/12f*timeToCurl,0.4f);
        otherModAnimation.AddKey(9/12f*timeToCurl,0.48f);
        otherModAnimation.AddKey(10/12f*timeToCurl,0.6f);
        otherModAnimation.AddKey(11/12f*timeToCurl,0.6f);
        otherModAnimation.AddKey(timeToCurl,0.6f);
        //Curl Mod
        curlModAnimation.AddKey(0,0);
        curlModAnimation.AddKey(1/12f*timeToCurl,0.92f);
        curlModAnimation.AddKey(2/12f*timeToCurl,0.92f);
        curlModAnimation.AddKey(3/12f*timeToCurl,1.4f);
        // curlModAnimation.AddKey(1/12f*timeToCurl,0.03f);
        // curlModAnimation.AddKey(2/12f*timeToCurl,0.07f);
        // curlModAnimation.AddKey(3/12f*timeToCurl,0.12f);

        curlModAnimation.AddKey(4/12f*timeToCurl,1.97f);
        curlModAnimation.AddKey(5/12f*timeToCurl,2.09f);
        curlModAnimation.AddKey(6/12f*timeToCurl,2.83f);
        curlModAnimation.AddKey(7/12f*timeToCurl,3.37f);
        curlModAnimation.AddKey(8/12f*timeToCurl,3.84f);
        curlModAnimation.AddKey(9/12f*timeToCurl,3.88f);
        curlModAnimation.AddKey(10/12f*timeToCurl,3.67f);
        curlModAnimation.AddKey(11/12f*timeToCurl,4f);
        curlModAnimation.AddKey(timeToCurl,5f);

        // for (int i=0; i<12;i++){
        //     otherModAnimation.SmoothTangents(i,0);
        //     curlModAnimation.SmoothTangents(i,0);
        // }
        


        currentIncrement = fastIncrement;

    }

    


    void DefineCochlear(){
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
            // endPos[i] += this.transform.position;

            endPos[i] = currentDefinePos;

            if (i == numPoints-1){
                // finalMassObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                finalMassObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                finalMassObject.name = "TopMass";
                finalMassObject.transform.localScale = 2f*scaleVector;
                // finalMassObject.AddComponent<Rigidbody>();
                // finalMassObject.GetComponent<Rigidbody>().drag = 1f;
                // finalMassObject.GetComponent<Rigidbody>().useGravity = false;
                // finalMassObject.GetComponent<Rigidbody>().isKinematic = true;
                // finalMassObject.GetComponent<CapsuleCollider>().isTrigger = true;
                finalMassObject.GetComponent<SphereCollider>().isTrigger = true;
                finalMassObject.GetComponent<Renderer>().material.SetColor("_Color",Color.red);
                // finalMassObject.GetComponent<Renderer>().material.enableInstancing = true;
                finalMassObject.transform.SetParent(this.transform);
                massList[i] = finalMassObject;
            }
            else{
                // springMassObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                springMassObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                springMassObject.name = "Mass" + i.ToString();
                springMassObject.transform.localScale = 2f*scaleVector;
                // springMassObject.AddComponent<Rigidbody>();
                // springMassObject.GetComponent<Rigidbody>().drag = 1f;
                // springMassObject.GetComponent<Rigidbody>().useGravity = false;
                // springMassObject.GetComponent<Rigidbody>().isKinematic = true;
                
                // springMassObject.GetComponent<CapsuleCollider>().isTrigger = true;
                springMassObject.GetComponent<SphereCollider>().isTrigger = true;
                springMassObject.GetComponent<Renderer>().material.SetColor("_Color",Color.red);
                // springMassObject.GetComponent<Renderer>().material.enableInstancing = true;
                springMassObject.transform.SetParent(this.transform);
                massList[i] = springMassObject;

            }

            //At this point all the masses are at the same location
            varPos[0] = endPos[0];

            if (i>0){
                // totalLength += (currentDefinePos - prevDefinePos).magnitude;
                currentDir = endPos[i] - endPos[i-1];
                float segmentLength = currentDir.magnitude;
                cochlearLength += segmentLength;
                Vector3 path = new Vector3(0,i*segmentLength,0);
                varPos[i] = endPos[0] + path;

                currentToPrev = Quaternion.FromToRotation(currentDir,prevDir);
                endRotations[i-1] = currentToPrev;
                prevDir = currentDir; 
            }

            prevDefinePos = currentDefinePos;

        }
        UnityEngine.Debug.Log("Length: " + cochlearLength.ToString());
        // cochlearLength = 60f; //mm, actual value from code is 61.74 but just treat as 60mm for the purpose of matching the intended 60mm insertion depth

            

        for (int i=0;i<numPoints;i++){

            if (evenDistribution){
                Vector3 point = new Vector3(0,(float)i*cochlearLength/numPoints,0);
                massList[i].transform.localPosition = endPos[0] + point;
                if (i>0){
                    float distance = Vector3.Distance(massList[i].transform.localPosition,massList[i-1].transform.localPosition);
                }
            }

            else{
                massList[i].transform.localPosition = varPos[i];
                if (i>0){
                    float distance = Vector3.Distance(massList[i].transform.localPosition,massList[i-1].transform.localPosition);
                }
            }

        } 
    }

    void OnValidate(){
        //When values in inspector changed
        if (finalMassObject){
            this.transform.RotateAround(entrancePosition,Vector3.forward,angleOfInsertion - previousAngleOfInsertion);
            previousAngleOfInsertion = angleOfInsertion;
        }
        
        CallUpdate();
    }

    void Start(){
        
        CallUpdate();
    }

    void Update(){

        if (GetData){
            UnityEngine.Debug.Log("X Pos:" + finalMassObject.transform.localPosition.x.ToString() + " Y Pos: " + finalMassObject.transform.localPosition.y.ToString());
            GetData = false;
        }
        
        if (automateInsertion){
            nonConstantFrames += 1;
            UnityEngine.Debug.Log("Number of Frames: " + nonConstantFrames.ToString());
        }
    }

    void FixedUpdate(){
        MotionController();
        qAngle = Quaternion.AngleAxis(-electrodeRoll,massList[0].transform.up);
        insertionAngle = Quaternion.AngleAxis(angleOfInsertion,this.transform.forward);
        this.GetComponent<Rigidbody>().velocity = this.transform.up*electrodeInsertionSpeed;
        DetectPointsInModel();
        CallUpdate();
    
    }

    void MotionController(){
        if (manualInsertion){
            int zeroHit = Convert.ToInt32( Input.GetKey("0"));
            int increaseInsertionVel = Convert.ToInt32( Input.GetKey("w") );
            int decreaseInsertionVel = Convert.ToInt32( Input.GetKey("s") );

            int increaseInsertionAngle = Convert.ToInt32(  Input.GetKey("d") );
            int decreaseInsertionAngle = Convert.ToInt32( Input.GetKey("a") );
            
            if (!startedInsertion && electrodeInsertionSpeed!=0){
                startedInsertion = true;
                myTimer.Start();
                
            }
            if (startedInsertion){
                curveDeltaTime += Time.fixedDeltaTime;
                linearVelOutput.Add(this.transform.GetComponent<Rigidbody>().velocity.y);
                RCMOutput.Add(angleOfInsertion);
                timeOutput.Add(curveDeltaTime);
            }

            if (myTimer.ElapsedMilliseconds > 6000){
                curlingModifier = curlModAnimation.Evaluate(curveDeltaTime-6);
                otherMod = otherModAnimation.Evaluate(curveDeltaTime-6);
            }

            if (zeroHit==1 && !firstZero){
                currentIncrement = slowIncrement;
                firstZero = true;
            }

            if (increaseInsertionAngle == 1 || decreaseInsertionAngle == 1){
                angleOfInsertion = Mathf.Clamp(angleOfInsertion + 0.1f*increaseInsertionAngle - 0.1f*decreaseInsertionAngle,-19,19);
                this.transform.RotateAround(finalMassObject.transform.position,Vector3.forward,angleOfInsertion - previousAngleOfInsertion);
                previousAngleOfInsertion = angleOfInsertion;
            }
            electrodeInsertionSpeed = Mathf.Clamp(electrodeInsertionSpeed + currentIncrement*increaseInsertionVel - currentIncrement*decreaseInsertionVel,0,20);
            if (zeroHit==1){
                electrodeInsertionSpeed = 0;
            }
            

            // If total insertion distance is greater than 60? Or if all points are inside of the cochlear? I can't really use interp anymore
            // cos I am not controlling that, so only distance or num points can be used

            if (numHitPoints == numPoints){
                manualInsertion = false;
                startedInsertion = false;
                electrodeInsertionSpeed = 0;
            }



            // interp = Mathf.Clamp(interp + 0.001f*increaseInterp - 0.001f*decreaseInterp,0f,1f);
            // interp = Mathf.Clamp(interp-Time.fixedDeltaTime/timeToCurl,0f,1f); //So this should not be controlled manually
            
        }

        if (automateInsertion){
            automateRetraction = false;
            electrodeInsertionSpeed = (int)(cochlearLength/timeToCurl);
            // electrodeInsertionSpeed = cochlearLength/(timeToCurl/Time.fixedDeltaTime);
            // interp = Mathf.Clamp(interp-Time.fixedDeltaTime/timeToCurl,0f,1f);
            previousInterp = interp;
            interp = interp-Time.fixedDeltaTime/timeToCurl;
            numSteps+=1;
            electrodeRoll = 30*(Mathf.Sin(Time.fixedDeltaTime*numSteps));
            angleOfInsertion = 10*(Mathf.Sin(Time.fixedDeltaTime*numSteps));
            // linearPosOutput.Add(this.transform.position.y);


            linearVelOutput.Add(this.transform.GetComponent<Rigidbody>().velocity.y);
            // RCMOutput.Add(electrodeRoll);
            // timeOutput.Add(myTimer.ElapsedMilliseconds);

            if (interp < 0.00001f){
                interp = 0;
                electrodeInsertionSpeed = 0;
                electrodeRoll = 0;
                angleOfInsertion = 0;
                automateInsertion = false;
            }
        }


        if (automateRetraction){
            automateInsertion = false;
            electrodeInsertionSpeed = -(int)(cochlearLength/timeToCurl);
            interp = Mathf.Clamp(interp+Time.fixedDeltaTime/timeToCurl,0f,1f);
            if (interp >= 0.9999f){
                electrodeInsertionSpeed = 0;
                automateRetraction = false;
            }
        }

    }

    void CallUpdate(){
        if (mesh != null && Application.isPlaying){
            MoveCochlearVertices();
            UpdateMesh();
        }
    }

    void DetectPointsInModel(bool detectPointsOnly=true){
        // From the tip of the cochlea downwards, detect which points are inside of the cochlear
        // If detectPointsOnly is false, also returns the distance to the walls from every point that is inside

        if (detectPointsOnly){
            for (int i = massList.Length-1;i>=1;i--){
                Transform massTrasform = massList[i].transform;
                Transform prevMassTransform = massList[i-1].transform; 
                // Vector3 rayDirection = insertionAngle*massTrasform.right;
                Vector3 rayDirection = Quaternion.AngleAxis(90,Vector3.back)*(massTrasform.position - prevMassTransform.position);
                raycastStartPosition = massTrasform.position;

                if (Physics.SphereCast(raycastStartPosition,0.001f,rayDirection,out hit,25,rayStopperMask)){
                    numHitPoints = massList.Length - 1 - i;
                    break;
                }

                else if (Physics.SphereCast(raycastStartPosition,0.001f,rayDirection,out hit,25,layerMask)){
                    UnityEngine.Debug.DrawRay(raycastStartPosition,rayDirection*hit.distance,Color.red);
                }

                else{
                    numHitPoints = massList.Length-1 - i;
                    break;
                }
                // UnityEngine.Debug.Log(numHitPoints.ToString());
            }
        }
        else{
            // Vector3 start = transform.position+(vertices[total-1]+vertices[total-1-nGon/2])/2;
                    Vector3 start = finalMassObject.transform.position;

                    RaycastHit hitSphereInner;
                    RaycastHit hitSphereOuter;
                    //Actually it is not this, we need to follow the direction of the tip. Will change this
                    Vector3 dirInner = finalMassObject.transform.right;
                    Vector3 dirOuter = -dirInner;

                    if (Physics.SphereCast(start,0.001f,dirInner,out hitSphereInner,100,layerMask)){
                        distInner = hitSphereInner.distance;
                        // UnityEngine.Debug.Log("Distance to Inner wall: " + distInner.ToString());
                        UnityEngine.Debug.DrawRay(start,dirInner*distInner,Color.red);
                    }
                    if (Physics.SphereCast(start,0.001f,dirOuter,out hitSphereOuter,100,layerMask)){
                        distOuter = hitSphereOuter.distance;
                        // UnityEngine.Debug.Log("Distance to Outer wall: " + distOuter.ToString());
                        UnityEngine.Debug.DrawRay(start,dirOuter*distOuter,Color.yellow);
            }
        }
    }


    void MoveCochlearVertices(){
        int v=0;
        startRot = new Quaternion(0,0,0,1);
        varRot = startRot;
        prevCurlingPoint = Vector3.zero;
        for (int i=0;i<numPoints;i++){
            if (i>0){
                float v1 = i;
                if (i>4 & i<=criticalPoint){
                    
                    float mod = curlingModifier * v1 / (numPoints - 1);
                    modifiedInterp = Mathf.Clamp(interp*(1-mod),0,1); 
                    varRotations[i-1] = Quaternion.Slerp(startRot,endRotations[i-1],modifiedInterp);
                }
                else if (i>criticalPoint){
                    float mod = curlingModifier * otherMod*v1 / (numPoints - 1);
                    modifiedInterp = Mathf.Clamp(interp*(1-mod),0,1); 
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
            if (i==0){
                forceDir = (basePoint - massList[i].transform.position);
            }

            else{
                forceDir = (rotatedBasePoint - massList[i].transform.position);
            }
        
            // UnityEngine.Debug.DrawRay(massList[i].transform.position,forceDir,Color.green,100);
            // Only Want this force if interp is changing

            // massList[i].GetComponent<Rigidbody>().MovePosition(rotatedBasePoint);
            // massList[i].GetComponent<Rigidbody>().MoveRotation(varRot);
            // if (interp>0.0001f && interp<0.9999f){
            // massList[i].GetComponent<Rigidbody>().AddForce(forceDir);
            // massList[i].transform.GetComponent<Rigidbody>().velocity += forceDir*Time.deltaTime;
            if (i==0){
                massList[i].transform.localPosition = basePoint;
            }
            else{

                massList[i].transform.localPosition = basePoint;
                massList[i].transform.RotateAround(massList[0].transform.position,Vector3.up,electrodeRoll);
                // massList[i].transform.localRotation = varRot;
                
            }
            

            // massList[i].GetComponent<Rigidbody>().velocity = forceDir;
            planeNormal = (basePoint-prevCurlingPoint);
            rotatePolygonPlane = Vector3.SignedAngle(Vector3.up,planeNormal,Vector3.forward);
            for (int n=0;n<nGon;n++){
                nGonPoint = new Vector3(1.2f*scale*Mathf.Cos(2*Mathf.PI*n/nGon),0,1.2f*scale*Mathf.Sin(2*Mathf.PI*n/nGon));
                if (i == 0){
                    Vector3 shiftNGon = new Vector3(0,-scale,0);
                    nGonPoint += shiftNGon;
                }
                else if (i == numPoints-1){
                    Vector3 shiftNGon = new Vector3(0,scale,0);
                    nGonPoint += shiftNGon;
                }
                nGonPoint = Quaternion.AngleAxis(rotatePolygonPlane,Vector3.forward)*nGonPoint;
                vertices[v] = massList[i].transform.localPosition + qAngle*nGonPoint;
                v+=1;
            }
            prevCurlingPoint = basePoint;
        }
        previousInterp = interp;

        DefineTriangles();
    }

    void DefineTriangles(){
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

    private void OnTriggerEnter(Collider other)
    {
        UnityEngine.Debug.Log("Collide Event");
    }
    

    void UpdateMesh(){
        mesh.Clear(); //Clear out the current buffer 
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // mesh.RecalculateNormals(); //Calculate normal based on triangle vertices are part of
    }
}


//Leftover Code, possible for use in future
#region leftoverCode

// if (automateInsertionWithSpatial){
//             //Change the insertionSpeed based on distance form wall in 2D plane while keeping the curling rate fixed
//             //If detect hit with wall, start again and change the parameters and compare distance travelled
//             interp = Mathf.Clamp(interp-Time.fixedDeltaTime/timeToCurl,0f,1f);
//             // ShortestDistance();
//             if (distInner < scale || distOuter< scale){
//                 //reset everything
//                 UnityEngine.Debug.Log("Reset");
//                 if (interp < bestInterp){
//                     //This run is better than a previous run so save the scalar
                
//                     bestInterp = interp;
//                     prevWallScalar = wallScalar;
//                     wallScalar += UnityEngine.Random.Range(0f,1f);
//                     UnityEngine.Debug.Log("Better: " + wallScalar.ToString());
//                 }
//                 else{
//                     //This run is worse than the previous run
//                     wallScalar = prevWallScalar += UnityEngine.Random.Range(0f,3f);
//                 }

//                 this.transform.position = startingPosition;
//                 interp = 1.0f;
//                 electrodeInsertionSpeed = 0;
//                 automateInsertionWithSpatial = false;
//             }
//             // else if (distInner > 0.5f && distInner < 2.0f){
//             //     electrodeInsertionSpeed = wallScalar*cochlearLength/(timeToCurl/Time.fixedDeltaTime);
//             // }
//             // else if (distOuter > 0.5f && distOuter<2.0f){
//             //     electrodeInsertionSpeed = 1/wallScalar*cochlearLength/(timeToCurl/Time.fixedDeltaTime);
//             // }
        
//             // else{
//             //     electrodeInsertionSpeed = cochlearLength/(timeToCurl/Time.fixedDeltaTime);
//             // }

//             if (interp < 0.00001f){
//                 //We made it!!
//                 UnityEngine.Debug.Log("We made it: " + wallScalar.ToString());
//                 electrodeInsertionSpeed = 0;
//                 automateInsertionWithSpatial = false;
//             }

//         }

// // void MeshCollision() {
    // //     // check collisions
    // //     for (int i=0;i<numPoints;i++){
    // //         hitColliders = Physics.OverlapCapsule()
    // //     }
    // //     hitColliders = Physics.OverlapSphere(this.transform.position,0.5f,layerMask);
    // //     // int numOverlaps = Physics.OverlapSphereNonAlloc(this.transform.position,this.GetComponent<SphereCollider>().radius,hitColliders,layerMask,QueryTriggerInteraction.UseGlobal);
    // //     // if (numOverlaps>0){
    // //     //     Debug.Log("Here: " + numOverlaps.ToString());
    // //     // }
        
    // //     // // int numOverlaps = Physics.OverlapBoxNonAlloc(this.transform.position,this.transform.localScale*-0.5f,hitColliders,this.transform.rotation,layerMask,QueryTriggerInteraction.UseGlobal);
    // //     for (int i = 0; i < hitColliders.Length; i++) {
    // //         var collider = hitColliders[i];
    // //         Vector3 otherPosition = collider.gameObject.transform.position;
    // //         Quaternion otherRotation = collider.gameObject.transform.rotation;
    // //         Vector3 direction;
    // //         float distance;

    // //         bool overlap = Physics.ComputePenetration(this.GetComponent<SphereCollider>(),this.transform.position,
    // //             this.transform.rotation,collider,otherPosition,
    // //             otherRotation,out direction,out distance);

    // //         if (overlap){
    // //             penetrationForce = -10*CollisionForce*(direction*distance);
                
    // //             // Vector3 movementDirection = moveDir + penetrationVector;
    // //             // Vector3 velocityProjected = Vector3.Project(rb.velocity, -direction);
    // //             rb.AddRelativeForce(penetrationForce);

    // //             // moveDir = movementDirection.normalized;
    // //             // this.transform.position = this.transform.position + penetrationVector;
    // //             // velocity -= velocityProjected;
    // //             Debug.Log("OnCollisionEnter with " + hitColliders[i].gameObject.name + " penetration vector: " + penetrationForce.ToString("F3"));
    // //         }
    // //         else
    // //         {
    // //             Debug.Log("OnCollision Enter with " + hitColliders[i].gameObject.name + " no penetration");
    // //         }
    // //     }

    // // }

          // RaycastHit hitSphere;
        // for (int i=0;i<360;i++){
        //     Vector3 dic = Quaternion.Euler(0,i,0)*Vector3.right;
        //     if (Physics.SphereCast(start,0.001f,dic,out hitSphere,100,layerMask)){
        //         if (hitSphere.distance<dist){
        //             dist = hitSphere.distance;
        //             hitDirection = dic;
        //         }
        //     }
        // }
        // UnityEngine.Debug.DrawRay(start,hitDirection*dist,Color.red);
        // if(dist<scale/2){
        //     interp = Mathf.Clamp(interp-Time.fixedDeltaTime/timeToCurl,0f,1f);

        // }
        // dist = Mathf.Infinity;


        // for (int i=1; i<numPoints;i++){
        //     massList[i].AddComponent<ConfigurableJoint>();
        //     // massList[i].GetComponent<ConfigurableJoint>().configuredInWorldSpace = true;
        //     massList[i].GetComponent<ConfigurableJoint>().enablePreprocessing = true;
        //     massList[i].GetComponent<ConfigurableJoint>().connectedBody = massList[i-1].GetComponent<Rigidbody>();
        //     // massList[i].GetComponent<ConfigurableJoint>().xMotion = ConfigurableJointMotion.Locked;
        //     // massList[i].GetComponent<ConfigurableJoint>().yMotion = ConfigurableJointMotion.Locked;
        //     massList[i].GetComponent<ConfigurableJoint>().zMotion = ConfigurableJointMotion.Locked;
        //     massList[i].GetComponent<ConfigurableJoint>().angularXMotion = ConfigurableJointMotion.Locked;
        //     massList[i].GetComponent<ConfigurableJoint>().angularYMotion = ConfigurableJointMotion.Locked;
        //     // massList[i].GetComponent<ConfigurableJoint>().angularZMotion = ConfigurableJointMotion.Limited;
        //     massList[i].GetComponent<ConfigurableJoint>().enableCollision = true;
        //     massList[i].GetComponent<ConfigurableJoint>().autoConfigureConnectedAnchor = false;
        //     massList[i].GetComponent<ConfigurableJoint>().connectedAnchor = new Vector3(0f,1.0f,0f);

                     
        // //     // massList[i].AddComponent<FixedJoint>();
        // //     // massList[i].GetComponent<FixedJoint>().connectedBody = massList[i-1].GetComponent<Rigidbody>();
        // //     // massList[i].GetComponent<FixedJoint>().enableCollision = true;

        
        //     SoftJointLimitSpring springJointLimit = new SoftJointLimitSpring();
        //     springJointLimit.damper = 10f;
        //     springJointLimit.spring = 6f;
        //     SoftJointLimit jointLimit = new SoftJointLimit();
        //     jointLimit.limit=2.5f;
        //     massList[i].GetComponent<ConfigurableJoint>().linearLimit = jointLimit;
        //     massList[i].GetComponent<ConfigurableJoint>().linearLimitSpring = springJointLimit;
        // }


            // void OnDrawGizmos()
    // {
    //     Gizmos.color = Color.blue;
    //     for (int i=1;i<numPoints;i++){
    //         Gizmos.DrawLine(endPos[i], varPos[i]);
    //     }
    // }


#endregion