﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViveSR.anipal.Eye;

using Random = UnityEngine.Random;

public class LieDetector : MonoBehaviour
{
    String basePath;

    Material sphereMaterial;

    AudioSource audioSource;
    List<AudioClip> audioClips = new List<AudioClip>();
    List<System.Object[]> colors = new List<System.Object[]>();

    float talkingAudioLevel = 20;
    float notTalkingAudioLevel = 2;

    Csv csv;
    int currentIndex;
    String currentColor;
    bool startedTalking = false;
    bool stoppedTalking = false;

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
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        basePath = Path.Combine(desktopPath, "LieDetector");
        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        List<Material> materials = new List<Material>();
        GameObject.Find("Sphere").GetComponent<Renderer>().GetMaterials(materials);
        sphereMaterial = materials[0];

        audioSource = GameObject.Find("Audio Source").GetComponent<AudioSource>();
        colors.Add(new System.Object[] { "Black", Color.black });
        colors.Add(new System.Object[] { "Blue", Color.blue });
        colors.Add(new System.Object[] { "Red", Color.red });
        colors.Add(new System.Object[] { "Green", Color.green });
        colors.Add(new System.Object[] { "Yellow", Color.yellow });
        colors.Add(new System.Object[] { "White", Color.white });

        InitializeCsv();
    }

    void InitializeCsv()
    {
        List<String> headers = new List<String>();

        headers.Add("Index");
        headers.Add("Color");
        headers.Add("Talking");

        String[] sensors = { "Camera", "Combine Gaze" };
        String[] xyz = { "X", "Y", "Z" };
        foreach (String sensor in sensors)
        {
            foreach (String columnType in xyz) headers.Add($"{sensor} {columnType}");
        }
        headers.Add("Left Pupil Diameter,Right Pupil Diameter,Left Eye Openness,Right Eye Openness");

        
        csv = new Csv(basePath, headers);
    }

    IEnumerator Run()
    {
        while (audioClips.Count < 15)
        {
            System.Object[] newColorInfo = RandomNewColor();
            currentColor = (String) newColorInfo[0];
            ChangeColor((Color) newColorInfo[1]);
            yield return RecordClip();
        }

        Save();

        csv.Close();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
    }

    System.Object[] RandomColor()
    {
        return colors[Random.Range(0, colors.Count - 1)];
    }

    System.Object[] RandomNewColor()
    {
        Color existingColor = sphereMaterial.GetColor("_Color");
        System.Object[] newColorInfo = RandomColor();
        while (existingColor == (Color) newColorInfo[1])
        {
            newColorInfo = RandomColor();
        }
        return newColorInfo;
    }

    void ChangeColor(Color c)
    {
        sphereMaterial.SetColor("_Color", c);
    }

    IEnumerator RecordClip()
    {
        audioSource.clip = Microphone.Start(null, false, 20, 44100);
        int endPosition = 0;

        startedTalking = false;
        stoppedTalking = false;

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

                if (sum > talkingAudioLevel)
                {
                    startedTalking = true;
                }
                else if (startedTalking && sum < notTalkingAudioLevel)
                {
                    stoppedTalking = true;
                }
            }

            recordCsv();

            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(0.5f);

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

    void recordCsv()
    {
        List<String> data = new List<String>();

        data.Add(currentIndex.ToString());

        data.Add(currentColor);

        data.Add((startedTalking && !stoppedTalking).ToString());

        Action<Vector3> addVector3ToCsv = (Vector3 vector3) =>
        {
            data.Add(vector3.x.ToString());
            data.Add(vector3.y.ToString());
            data.Add(vector3.z.ToString());
        };

        Ray cameraRay = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
        addVector3ToCsv(cameraRay.direction);

        Ray gazeRay = new Ray();
        SRanipal_Eye.GetGazeRay(GazeIndex.COMBINE, out gazeRay);
        addVector3ToCsv(gazeRay.direction);

        //EyeData eyeData = new EyeData();

        ViveSR.anipal.Eye.EyeData eyeData = new ViveSR.anipal.Eye.EyeData();
        SRanipal_Eye_API.GetEyeData(ref eyeData);

        data.Add(eyeData.verbose_data.left.pupil_diameter_mm.ToString());
        data.Add(eyeData.verbose_data.left.pupil_diameter_mm.ToString());

        data.Add(eyeData.verbose_data.left.eye_openness.ToString());
        data.Add(eyeData.verbose_data.right.eye_openness.ToString());

        csv.Row(data);
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


