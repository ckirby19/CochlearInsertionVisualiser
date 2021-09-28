using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class SaveData : MonoBehaviour
{
    // public string FolderName;  
    private string current_time;

    private List<float> linearVelData, RCMData,timeData;
    private float timeVal = 0;


    void OnApplicationQuit(){
        if (this.GetComponent<CochlearCurling>().SaveThisData){
            string DirectoryPath = Application.dataPath + "/Data/" + "InsertionExperiment";
            if (!Directory.Exists(DirectoryPath)){
                Directory.CreateDirectory(DirectoryPath);
            }
            timeData = this.transform.GetComponent<CochlearCurling>().timeOutput;
            linearVelData = this.transform.GetComponent<CochlearCurling>().linearVelOutput;
            RCMData = this.transform.GetComponent<CochlearCurling>().RCMOutput;
            
            current_time = System.DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss");
            string raw_path = DirectoryPath + "/Date_" + current_time + ".txt"; 
            StreamWriter raw_data = new StreamWriter(raw_path);
            for (int i=0; i<linearVelData.Count;i++){
                raw_data.Write(timeData[i]);
                raw_data.Write(" ");
                raw_data.Write(linearVelData[i]);
                raw_data.Write(" ");
                raw_data.Write(Mathf.Abs(RCMData[i] - RCMData[0]));
                raw_data.Write(" ");
                raw_data.Write(System.Environment.NewLine);
                // timeVal += 0.02f;
            }

        }
    }
}
   