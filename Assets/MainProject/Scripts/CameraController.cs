// Converted from UnityScript to C# at http://www.M2H.nl/files/js_to_c.php - by Mike Hergaarden
   
 using UnityEngine;
 using System.Collections;

 
 public class CameraController : MonoBehaviour
 {
     /*
     Have camera focus on a target that is an empty object within the robot heirachy.
     This camera smoothes out rotation around the y-axis and height.
     Horizontal Distance to the target is always fixed.
     
     There are many different ways to smooth the rotation but doing it this way gives you a lot of control over how the camera behaves.
     
     For every one of those smoothed values we calculate the wanted value and the current value.
     Then we smooth it using the Lerp function.
     Then we apply the smoothed values to the transform's position.
     */
 
     public Transform target;
     // The distance in the x-z plane to the target
     public float distance = 10.0f;
     // the height we want the camera to be above the target
     public float height = 5.0f;
     //How much camera moves around as target moves around
     public float heightDamping = 2.0f;
     public float rotationDamping = 3.0f;
 
     void LateUpdate (){
         // Early out if we don't have a target
            if (!target)
                return;
        

            // Damp the rotation around the y-axis
            float currentRotationAngle = Mathf.LerpAngle(this.transform.eulerAngles.y,target.eulerAngles.y, rotationDamping * Time.deltaTime);
        
            float currentHeight = Mathf.Lerp(this.transform.position.y, height+target.position.y, heightDamping * Time.deltaTime);

            // Convert the angle into a rotation
            Quaternion currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);
        
            // Set the position of the camera on the x-z plane to:
            // distance behind the target
            this.transform.position = target.position;
            this.transform.position -= currentRotation * Vector3.forward * distance;

            // Set the height of the camera
            this.transform.position = new Vector3(this.transform.position.x, currentHeight, this.transform.position.z);

            transform.LookAt (target);
     }
 }