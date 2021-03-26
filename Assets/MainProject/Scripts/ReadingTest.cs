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

    [DllImport("cpp_example_dll.dll",CallingConvention = CallingConvention.Cdecl)]
    static extern int copy_array(double[] output, int length);
    private int outLength;

    // Start is called before the first frame update
    void Start()
    {
        // interp = FooPluginAPI_Auto.sum(2.3f, 1.2f);
        // interp = FooPluginAPI_Auto.cochlearCurl(0.1f);
        
    }

    // Update is called once per frame
    void Update()
    {
        if (this.GetComponent<CochlearCurling>().automateInsertion || this.GetComponent<CochlearCurling>().automateRetraction){
            posList.Add((double)this.transform.position.y);
        }
    }

    void OnApplicationQuit(){
        outLength = posList.Count;
        double[] posOutput = new double[posList.Count];
        for (int i=0; i < posList.Count; i++){
            posOutput[i] = posList[i];
        };
        print(posOutput);
        copy_array(posOutput,outLength);
        
    }
}
