using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class SaveData : MonoBehaviour
{
    public string FolderName;
    public bool SaveThisData;    
    private string current_time;

    private List<float> linearData,rotaryData,timeData;

    void OnApplicationQuit(){
        if (SaveThisData){
            string DirectoryPath = Application.dataPath + "/Data/" + FolderName;
            if (!Directory.Exists(DirectoryPath)){
                Directory.CreateDirectory(DirectoryPath);
            }
            linearData = this.transform.GetComponent<CochlearCurling>().linearPosOutput;
            rotaryData = this.transform.GetComponent<CochlearCurling>().rotaryPosOutput;
            timeData = this.transform.GetComponent<CochlearCurling>().timeOutput;
            current_time = System.DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss");
            string raw_path = DirectoryPath + "/Date_" + current_time + ".txt"; 
            StreamWriter raw_data = new StreamWriter(raw_path);
            for (int i=0; i<linearData.Count;i++){
                raw_data.Write(timeData[i]-timeData[0]);
                raw_data.Write(" ");
                raw_data.Write(linearData[i]);
                raw_data.Write(" ");
                raw_data.Write(Mathf.Abs(rotaryData[i]-rotaryData[0]));
                raw_data.Write(System.Environment.NewLine);
            }

        }
    }
}
   