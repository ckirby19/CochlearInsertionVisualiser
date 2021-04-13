using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class ReadingTest : MonoBehaviour
{
    // Need to use interop friendly types at the interop boundary. For instance, null-terminated arrays of characters. 
    // That works well when you allocate and deallocate the memory in the same module.
    private List<double> posList;
    private byte[] byte_array;

    // [DllImport("cpp_example_dll.dll",CallingConvention = CallingConvention.Cdecl)]
    // static extern int copy_array(double[] output, int length);
    private int outLength;

    // Start is called before the first frame update
    void Start()
    {
        posList = new List<double>();
    }

    // Update is called once per frame
    void Update()
    {
        if (this.GetComponent<CochlearCurling>().automateInsertion || this.GetComponent<CochlearCurling>().automateRetraction){
            double outputVal = (double)this.transform.position.y;
            posList.Add(outputVal);
            FooPluginAPI_Auto.sendSingle(outputVal);

        }
    }

    void OnApplicationQuit(){
        outLength = posList.Count;
        double[] posOutput = new double[posList.Count];
        for (int i=0; i < posList.Count; i++){
            posOutput[i] = posList[i];
        };
        var val = FooPluginAPI_Auto.simpleFunc();
        Debug.Log(string.Format("simple_func: {0}", val));

        
        FooPluginAPI_Auto.copyArray(posOutput,outLength);

        
    }
}
