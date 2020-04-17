using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

public class LieDetector : MonoBehaviour
{
    String basePath;

    Material sphereMaterial;

    AudioSource audioSource;
    List<AudioClip> audioClips = new List<AudioClip>();
    List<Color> colors = new List<Color>();

    float minimumAudioLevel = 10;

    Csv csv;

    // Start is called before the first frame update
    void Start()
    {
        Initialize();
        StartCoroutine(Run());
        Save();
    }

    // Update is called once per frame
    void Update()
    {
    }

    void Initialize()
    {
        String applicationPath = System.Environment.SpecialFolder.ApplicationData.ToString();
        basePath = System.IO.Path.Combine(applicationPath, "LieDetector");
        Debug.Log($"Outputting to: {basePath}");
        System.IO.Directory.CreateDirectory(basePath);

        List<Material> materials = new List<Material>();
        GameObject.Find("Sphere").GetComponent<Renderer>().GetMaterials(materials);
        sphereMaterial = materials[0];

        audioSource = GameObject.Find("Audio Source").GetComponent<AudioSource>();
        colors.Add(Color.black);
        colors.Add(Color.blue);
        colors.Add(Color.red);
        colors.Add(Color.green);
        colors.Add(Color.yellow);
        colors.Add(Color.white);

        InitializeCsv();
    }

    void InitializeCsv()
    {
        List<String> headers = new List<String>();

        headers.Add("Time");

        String[] sensors = { "Camera", "Left Eye", "Right Eye", "Combine Eye" };
        String[] aspects = { "Position", "Direction" };
        String[] xyz = { "X", "Y", "Z" };
        foreach (String sensor in sensors)
        {
            foreach (String aspect in aspects)
            {
                foreach (String columnType in xyz) headers.Add($"{sensor} {aspect} {columnType}");
            }
        }
        headers.Add("Left Pupil Diameter,Right Pupil Diameter,Left Eye Openness,Right Eye Openness,Left Eye Blink,Right Eye Blink");

        csv = new Csv(basePath, headers);
    }

    IEnumerator Run()
    {
        while (audioClips.Count < 15) {
            ChangeColor(RandomNewColor());
            yield return RecordClip();
        }
    }

    Color RandomColor()
    {
        return colors[Random.Range(0, colors.Count - 1)];
    }

    Color RandomNewColor()
    {
        Color existingColor = sphereMaterial.GetColor("_Color");
        Color newColor = RandomColor();
        while (existingColor == newColor)
        {
            newColor = RandomColor();
        }
        return newColor;
    }

    void ChangeColor(Color c)
    {
        sphereMaterial.SetColor("_Color", c);
    }

    IEnumerator RecordClip()
    {
        audioSource.clip = Microphone.Start(null, false, 200, 44100);
        int endPosition = 0;

        bool startedTalking = false;
        bool stoppedTalking = false;

        float colorTime = Time.fixedTime;

        while (!stoppedTalking)
        {
            if (Microphone.GetPosition(null) > 0)
            {
                int sampleCount = 1024;
                float[] clipSampleData = new float[sampleCount];
                float[] samples = new float[audioSource.clip.samples];
                audioSource.clip.GetData(samples, 0);

                float[] clipSamples = new float[Microphone.GetPosition(null)];
                Array.Copy(samples, clipSamples, clipSamples.Length - 1);

                float sum = 0;

                if (clipSamples.Length > sampleCount)
                {
                    for (var i = clipSamples.Length - 1; i > clipSamples.Length - sampleCount; i--)
                    {
                        sum += Mathf.Abs(clipSamples[i]);
                    }
                }

                if (sum > minimumAudioLevel)
                {
                    startedTalking = true;
                }
                else if (startedTalking && sum < minimumAudioLevel)
                {
                    stoppedTalking = true;
                }
            }

            yield return new WaitForEndOfFrame();

        }

        endPosition = Microphone.GetPosition(null);
        Microphone.End(null);

        if (endPosition > 0)
        {
            float[] fullSamples = new float[audioSource.clip.samples];
            audioSource.clip.GetData(fullSamples, 0);
            float[] fullClipSamples = new float[endPosition];
            Array.Copy(fullSamples, fullClipSamples, fullClipSamples.Length - 1);
            AudioClip clip = AudioClip.Create($"Clip{audioClips.Count}", fullClipSamples.Length, 1, 44100, false);
            clip.SetData(fullClipSamples, 0);
            this.audioClips.Add(clip);
        }
    }

    void Save()
    {
        int index = 0;
        foreach (AudioClip audioClip in audioClips)
        {
            string wavFilePath = System.IO.Path.Combine(basePath, $"clip{index}.wav");
            SavWav.Save(wavFilePath, audioClip);
            index += 1;
        }
    }
}


//using System;
//using System.IO;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using ReactNeuro.Utilities;
//using Valve.VR;

//using Random = UnityEngine.Random;

//namespace ReactNeuro
//{
//    namespace Exams
//    {

//        struct ColorString
//        {
//            public string text;
//            public string colorName;
//            public Color color;

//            public ColorString(string text, string colorName, Color color)
//            {
//                this.text = text;
//                this.colorName = colorName;
//                this.color = color;
//            }

//            override public string ToString()
//            {
//                return $"{colorName}-{text}";
//            }
//        }

//        public class Stroop : Exam
//        {
//            String version = "1.2";
//            bool requiresCalibration = false;

//            VRSystem activeSystem;
//            Objects objects;

//            AudioSource audioSource;
//            AudioClip audioClip;

//            Dictionary<Color, String> colors = new Dictionary<Color, string>();
//            List<ColorString> colorStrings = new List<ColorString>();

//            int activeIndex = -1;
//            string activeColorString = "-";

//            float minimumLevel = 10;

//            int trials = 20;

//            public Stroop(VRSystem activeSystem)
//            {
//                this.activeSystem = activeSystem;
//                this.objects = activeSystem.SystemObjects();

//                colors.Add(Color.blue, "Blue");
//                //colors.Add(new Color(1.0f, 0.64f, 0.0f), "Orange");
//                colors.Add(Color.green, "Green");
//                colors.Add(Color.red, "Red");
//                //colors.Add(Color.black, "Black");
//                //colors.Add(new Color(0.5f, 0.2f, 0.73f), "Purple");
//                //colors.Add(new Color(0.9f, 0.43f, 0.9f), "Pink");

//                Color[] colorKeys = new Color[colors.Keys.Count];
//                colors.Keys.CopyTo(colorKeys, 0);

//                for (int i = 0; i < trials; ++i)
//                {
//                    string text = colors[colorKeys[Mathf.FloorToInt(Random.Range(0, colorKeys.Length))]];
//                    Color color;
//                    string colorName;
//                    do
//                    {
//                        color = colorKeys[Mathf.FloorToInt(Random.Range(0, colorKeys.Length))];
//                        colorName = colors[color];
//                    } while (i % 2 == (colorName != text ? 0 : 1));

//                    this.colorStrings.Add(new ColorString(text, colorName, color));
//                }

//                for (int t = 0; t < trials; t++)
//                {
//                    ColorString tmp = this.colorStrings[t];
//                    int r = Random.Range(t, trials);
//                    this.colorStrings[t] = this.colorStrings[r];
//                    this.colorStrings[r] = tmp;
//                }
//            }

//            public String GetVersion()
//            {
//                return version;
//            }

//            public bool RequiresCalibration()
//            {
//                return requiresCalibration;
//            }

//            public IEnumerator Run()
//            {
//                audioSource = GameObject.Find("Audio").GetComponent<AudioSource>();

//                objects.UpdateInstructions("Say aloud the color of the\ntext as fast as possible.\nDo not read the word.");
//                yield return Helper.RunInstructionsDot(activeSystem);

//                objects.HideInstructions();

//                objects.ChangeScreenColor(new Color(0.45f, 0.47f, 0.5f));
//                yield return new WaitForSeconds(2);

//                audioSource.clip = Microphone.Start(null, false, 200, 44100);
//                int endPosition = 0;

//                int index = 0;
//                foreach (ColorString colorString in this.colorStrings)
//                {
//                    this.activeIndex = index;
//                    this.activeColorString = colorString.ToString();

//                    objects.UpdateInstructions(colorString.text);
//                    Text instructions = objects.FindText("Instructions");
//                    instructions.color = colorString.color;
//                    instructions.fontSize = 30;

//                    bool startedTalking = false;
//                    bool stoppedTalking = false;

//                    float colorTime = Time.fixedTime;

//                    bool triggerPressed = false;
//                    while (triggerPressed || (!stoppedTalking && (Time.fixedTime - colorTime) < 4))
//                    {
//                        SteamVR_Input_Sources RightInputSource = SteamVR_Input_Sources.RightHand;
//                        triggerPressed = SteamVR_Actions._default.Squeeze.GetAxis(RightInputSource) > 0f;

//                        if (Microphone.GetPosition(null) > 0)
//                        {
//                            int sampleCount = 1024;
//                            float[] clipSampleData = new float[sampleCount];
//                            float[] samples = new float[audioSource.clip.samples];
//                            audioSource.clip.GetData(samples, 0);

//                            float[] clipSamples = new float[Microphone.GetPosition(null)];
//                            Array.Copy(samples, clipSamples, clipSamples.Length - 1);

//                            float sum = 0;

//                            if (clipSamples.Length > sampleCount)
//                            {
//                                for (var i = clipSamples.Length - 1; i > clipSamples.Length - sampleCount; i--)
//                                {
//                                    sum += Mathf.Abs(clipSamples[i]);
//                                }
//                            }
//                            float currentAverageVolume = sum / clipSamples.Length;

//                            if (sum > minimumLevel)
//                            {
//                                startedTalking = true;
//                            }
//                            else if (startedTalking && sum < minimumLevel && !triggerPressed)
//                            {
//                                stoppedTalking = true;

//                                this.activeColorString = "-";
//                                this.activeIndex = -1;
//                                objects.UpdateInstructions("");
//                                yield return new WaitForSeconds(0.25f);
//                            }
//                        }

//                        yield return new WaitForEndOfFrame();
//                    }

//                    yield return new WaitForSeconds(Random.Range(0.5f, 1f));

//                    index += 1;
//                }

//                objects.Reset();
//                objects.UpdateInstructions("Great job!");

//                endPosition = Microphone.GetPosition(null);
//                Microphone.End(null);

//                if (endPosition > 0)
//                {
//                    float[] fullSamples = new float[audioSource.clip.samples];
//                    audioSource.clip.GetData(fullSamples, 0);
//                    float[] fullClipSamples = new float[endPosition];
//                    Array.Copy(fullSamples, fullClipSamples, fullClipSamples.Length - 1);
//                    AudioClip clip = AudioClip.Create("Stroop", fullClipSamples.Length, 1, 44100, false);
//                    clip.SetData(fullClipSamples, 0);
//                    this.audioClip = clip;
//                }

//                yield return new WaitForSeconds(2);
//            }

//            public Dictionary<String, Func<string>> DataColumns()
//            {
//                Dictionary<String, Func<string>> dataColumns = new Dictionary<String, Func<string>>();

//                dataColumns["Color Index"] = () => this.activeIndex.ToString();
//                dataColumns["Active Color"] = () => this.activeColorString.Split('-')[0];
//                dataColumns["Active Text"] = () => this.activeColorString.Split('-')[1];

//                return dataColumns;
//            }

//            public void Save(string path)
//            {
//                if (this.audioClip != null)
//                {
//                    string wavFilePath = System.IO.Path.Combine(path, "Stroop/stroop.wav");
//                    SavWav.Save(wavFilePath, this.audioClip);
//                }
//            }

//            public void Finish() { }

//        }
//    }
//}
