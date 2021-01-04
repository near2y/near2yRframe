﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

public class BuildAPP
{
    public static string m_AppName = "Near2y";

    public static string m_AndroidPath = Application.dataPath + "/../BuildTarget/Android/";
    public static string m_IOSPath = Application.dataPath + "/../BuildTarget/IOS/";
    public static string m_WindowsPath = Application.dataPath + "/../BuildTarget/Windows/";


    public static void Build()
    {
        string abPath = Application.dataPath + "/../AssetBundle/" + EditorUserBuildSettings.activeBuildTarget.ToString() + "/";
        Copy(abPath, Application.streamingAssetsPath);
        string savePath = "";
        switch (EditorUserBuildSettings.activeBuildTarget)
        {
            case BuildTarget.Android:
                savePath = m_AndroidPath + m_AppName + "_" + EditorUserBuildSettings.activeBuildTarget +
                    string.Format("_{0:yyyy_MM_dd_HH_mm}", DateTime.Now + ".apk");
                break;
            case BuildTarget.StandaloneWindows:
                savePath = m_WindowsPath + m_AppName + "_" + EditorUserBuildSettings.activeBuildTarget +
                    string.Format("_{0:yyyy_MM_dd_HH_mm}/{1}.exe", DateTime.Now,m_AppName);
                break;
            case BuildTarget.iOS:
                savePath = m_IOSPath + m_AppName + "_" + EditorUserBuildSettings.activeBuildTarget +
                    string.Format("_{0:yyyy_MM_dd_HH_mm}", DateTime.Now);
                break;
            case BuildTarget.StandaloneWindows64:
                savePath = m_WindowsPath + m_AppName + "_" + EditorUserBuildSettings.activeBuildTarget +
                    string.Format("_{0:yyyy_MM_dd_HH_mm}/{1}.exe", DateTime.Now, m_AppName);
                break;
        }
        BuildPipeline.BuildPlayer(FindEnableEditorScenes(), savePath, BuildTarget.Android, BuildOptions.None);
    }

    /// <summary>
    /// 找到已经打开的场景
    /// </summary>
    /// <returns></returns>
    private static string[] FindEnableEditorScenes()
    {
        List<string> editorScenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            editorScenes.Add(scene.path);
        }
        return editorScenes.ToArray();
    }

    /// <summary>
    /// 将AB包拷贝过来
    /// </summary>
    private static void Copy(string srcPath,string targetPath)
    {
        try
        {
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }
            string scrdir = Path.Combine(targetPath, Path.GetFileName(srcPath));
            if (Directory.Exists(srcPath))
                scrdir += Path.DirectorySeparatorChar;
            if (!Directory.Exists(scrdir))
            {
                Directory.CreateDirectory(scrdir);
            }

            string[] files = Directory.GetFileSystemEntries(srcPath);
            foreach (string file in files)
            {
                if (Directory.Exists(file))
                {
                    Copy(file, scrdir);
                }
                else
                {
                    File.Copy(file,scrdir+Path.GetFileName(file),true);
                }
            }
        }
        catch
        {
            Debug.LogError("无法复制：" + srcPath + " 到" + targetPath);
        }
    }

    private static void DeleteDir(string scrPath)
    {
        try
        {
            DirectoryInfo dir = new DirectoryInfo(scrPath);
            //FileSystemInfo[] fileInfo = dir.get
        }
        catch
        {

        }
    }
}
