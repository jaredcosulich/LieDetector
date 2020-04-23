using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class Csv
{
    string path;
    private static StreamWriter sw;

    float startTime = -1f;
    float lastTime = -1f;

    public Csv(string path, List<String> headers)
    {
        this.path = path;
        Debug.Log(path);
        sw = new StreamWriter(path);
        Header(headers);
    }

    public void Close()
    {
        if (sw == null) return;
        sw.Close();
    }

    public void Header(List<String> headers)
    {
        sw.Write(String.Join(",", headers));
        sw.WriteLine(",Total Time,Delta Time");
        sw.Flush();

    }

    public void Row(List<String> data)
    {
        if (startTime == -1) startTime = Time.time;
        if (lastTime == -1) lastTime = Time.time;

        data.Add((Time.time - startTime).ToString());
        data.Add((Time.time - lastTime).ToString());
        sw.WriteLine(string.Join(",", data));
        //sw.Flush();

        lastTime = Time.time;
    }
}

//using System;
//using System.IO;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using System.Linq;

//namespace ReactNeuro
//{
//    namespace Utilities
//    {
//        public class Csv
//        {
//            VRSystem activeSystem;
//            Objects objects;
//            Audit audit;

//            String basePath;
//            String patientId;

//            String tempPath;
//            String examFileName;
//            private static StreamWriter sw;

//            bool recordData = true;

//            bool initialized;

//            Dictionary<String, List<float>> performanceSamples = new Dictionary<string, List<float>>();

//            Exam exam;

//            float lastTime = Time.time;

//            public Csv(VRSystem activeSystem, Objects objects, Audit audit, String basePath, String patientId = null)
//            {
//                if (!recordData) return;

//                this.activeSystem = activeSystem;
//                this.objects = objects;
//                this.audit = audit;
//                this.basePath = basePath;
//                this.patientId = patientId;
//            }

//            public void Initialize(Exam exam)
//            {
//                if (!recordData) return;

//                this.tempPath = System.IO.Path.Combine(this.basePath, "temp");
//                System.IO.Directory.CreateDirectory(this.tempPath);

//                this.exam = exam;

//                String filename = exam.GetType().Name;

//                filename += $"--v#{Application.version}-{exam.GetVersion()}";

//                if (patientId != null)
//                {
//                    filename += $"--{patientId}";
//                }

//                audit.Log($"Initializing {filename}.csv");

//                examFileName = $"{filename}.csv";
//                String examPath = System.IO.Path.Combine(tempPath, examFileName);
//                sw = new StreamWriter(examPath);

//                List<String> header = new List<String>();

//                header.Add("Instruction Dot Size");

//                Dictionary<String, Func<string>> dataColumns = exam.DataColumns();
//                foreach (String columnName in dataColumns.Keys)
//                {
//                    header.Add(columnName);
//                }

//                String[] sensors = { "Camera", "Left Eye", "Right Eye", "Combine Eye" };
//                String[] aspects = { "Position", "Direction" };
//                String[] xyz = { "X", "Y", "Z" };
//                foreach (String sensor in sensors)
//                {
//                    foreach (String aspect in aspects)
//                    {
//                        foreach (String columnType in xyz) header.Add($"{sensor} {aspect} {columnType}");
//                    }
//                }
//                header.Add("Left Pupil Diameter,Right Pupil Diameter,Left Eye Openness,Right Eye Openness,Left Eye Blink,Right Eye Blink");

//                sw.Write(String.Join(",", header));
//                sw.WriteLine(",Date,Total Time,Delta Time");
//                sw.Flush();

//                lastTime = Time.time;

//                initialized = true;
//            }

//            public void Close()
//            {
//                if (sw == null) return;

//                sw.Close();

//                String tempFilePath = System.IO.Path.Combine(tempPath, examFileName);
//                if (System.IO.File.Exists(tempFilePath))
//                {
//                    String examFilePath = System.IO.Path.Combine(basePath, examFileName);
//                    System.IO.File.Move(tempFilePath, examFilePath);
//                    audit.Log($"Saved csv file: {examFilePath}");
//                }

//                audit.Log($"Removing temp csv file: {tempPath}");
//                System.IO.Directory.Delete(tempPath);
//            }

//            private void recordPerformanceSample(string name, float time)
//            {
//                if (!performanceSamples.ContainsKey(name))
//                {
//                    performanceSamples[name] = new List<float>();
//                }
//                performanceSamples[name].Add(time);
//            }

//            public void RecordData()
//            {
//                if (!recordData || !initialized || !activeSystem.IsReady()) return;

//                List<String> line = new List<String>();
//                Text instructionsDot = objects.Dot("Instructions");
//                line.Add(objects.IsVisible(instructionsDot) ? instructionsDot.fontSize.ToString() : "");

//                Dictionary<String, Func<string>> dataColumns = exam.DataColumns();
//                foreach (String columnName in dataColumns.Keys)
//                {
//                    line.Add(dataColumns[columnName]());
//                }

//                Action<Vector3> addVector3ToCsv = (Vector3 vector3) =>
//                {
//                    line.Add(vector3.x.ToString());
//                    line.Add(vector3.y.ToString());
//                    line.Add(vector3.z.ToString());
//                };

//                Ray cameraRay = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
//                Vector3 cameraPosition = cameraRay.origin;
//                Vector3 cameraDirection = cameraRay.direction;
//                addVector3ToCsv(cameraPosition);
//                addVector3ToCsv(cameraDirection);

//                EyeData eyeData = activeSystem.GetVerboseData();

//                //Vector3 leftEyePosition = activeSystem.GetLeftEyePosition();
//                //Vector3 leftEyeDirection = activeSystem.GetLeftEyeDirection();
//                //addVector3ToCsv(leftEyePosition);
//                //addVector3ToCsv(leftEyeDirection);
//                addVector3ToCsv(eyeData.LeftEyePosition);
//                addVector3ToCsv(eyeData.LeftEyeDirection);

//                //Vector3 rightEyePosition = activeSystem.GetRightEyePosition();
//                //Vector3 rightEyeDirection = activeSystem.GetRightEyeDirection();
//                //addVector3ToCsv(rightEyePosition);
//                //addVector3ToCsv(rightEyeDirection);
//                addVector3ToCsv(eyeData.RightEyePosition);
//                addVector3ToCsv(eyeData.RightEyeDirection);

//                //Vector3 combineEyePosition = new Vector3(0, 0, 0);
//                //Vector3 combineEyeDirection = activeSystem.GetCombinedDirection();
//                //addVector3ToCsv(combineEyePosition);
//                //addVector3ToCsv(combineEyeDirection);
//                addVector3ToCsv(eyeData.CombinedEyePosition);
//                addVector3ToCsv(eyeData.CombinedEyeDirection);

//                //float leftPupilDiameter = activeSystem.GetLeftPupilDiameter();
//                //line.Add(leftPupilDiameter.ToString());
//                line.Add(eyeData.LeftPupilDiameter.ToString());

//                //float rightPupilDiameter = activeSystem.GetRightPupilDiameter();
//                //line.Add(rightPupilDiameter.ToString());
//                line.Add(eyeData.RightPupilDiameter.ToString());

//                //float leftEyeOpenness = activeSystem.GetLeftEyeOpenness();
//                //line.Add(leftEyeOpenness.ToString());
//                line.Add(eyeData.LeftEyeOpenness.ToString());

//                //float rightEyeOpenness = activeSystem.GetRightEyeOpenness();
//                //line.Add(rightEyeOpenness.ToString());
//                line.Add(eyeData.RightEyeOpenness.ToString());

//                //bool leftEyeBlinking = activeSystem.IsLeftEyeBlinking();
//                //line.Add(leftEyeBlinking.ToString());
//                line.Add(eyeData.LeftEyeBlinking.ToString());

//                //bool rightEyeBlinking = activeSystem.IsRightEyeBlinking();
//                //line.Add(rightEyeBlinking.ToString());
//                line.Add(eyeData.RightEyeBlinking.ToString());

//                line.Add(DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss:ffff tt"));
//                line.Add(Time.time.ToString());
//                line.Add((Time.time - lastTime).ToString());
//                sw.WriteLine(string.Join(",", line));
//                //sw.Flush();

//                lastTime = Time.time;
//            }

//            public void PrintPerformance()
//            {
//                foreach (String name in performanceSamples.Keys)
//                {
//                    float total = performanceSamples[name].Sum();
//                    float count = performanceSamples[name].Count;
//                    Debug.Log($"PERFORMANCE {name}: Total: {total} - Count: {count} - Average: {total / count}");
//                }
//            }
//        }
//    }
//}
