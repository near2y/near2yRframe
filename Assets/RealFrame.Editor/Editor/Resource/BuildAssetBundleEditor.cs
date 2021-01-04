using UnityEditor;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class BuildAssetBundleEditor 
{


    //key是ab包名，value是路径，所有文件夹ab包dic
    private static Dictionary<string, string> allFileDir = new Dictionary<string, string>();

    //储存所有的ab资源路径，过滤之后的
    private static List<string> allFileAB = new List<string>();

    //单个prefab的ab包
    private static Dictionary<string, List<string>> allPrefabDir = new Dictionary<string, List<string>>();

    //储存所有有效路径，过滤不需要使用的路径
    private static List<string> validFile = new List<string>();
    [MenuItem("Tools/Near2y/打AB包")]
    public static void BuildAllAssetBundles()
    {
        allFileDir.Clear();
        allPrefabDir.Clear();
        allFileAB.Clear();
        validFile.Clear();
        //文件夹类型
        ABConfig aBConfig = AssetDatabase.LoadAssetAtPath<ABConfig>(GameConfig.ABCONFIGASSETPATH);
        foreach(ABConfig.FileDirABName fileDir in aBConfig.allFileDirAB)
        {
            if (allFileDir.ContainsKey(fileDir.ABName))
            {
                Debug.LogError("AB包配置名字重复，请检查！");
            }
            else
            {
                allFileDir.Add(fileDir.ABName,fileDir.ABPath);
                allFileAB.Add(fileDir.ABPath);
                validFile.Add(fileDir.ABPath);
            }
        }


        //单个文件
        string[] allStr = AssetDatabase.FindAssets("t:Prefab", aBConfig.allPrefabPath.ToArray());
        for(int i = 0; i < allStr.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(allStr[i]);
            EditorUtility.DisplayProgressBar("查找prefab", "Prefab:" + path, i * 1.0f / allStr.Length);
            validFile.Add(path);
            if (!ContainAllFileAB(path))
            {
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                string[] allDepend = AssetDatabase.GetDependencies(path);
                List<string> allDependPath = new List<string>();
                //添加的不包含自己
                allDependPath.Add(path);
                for(int j = 0; j < allDepend.Length; j++)
                {
                    if(!ContainAllFileAB(allDepend[j]) && !allDepend[j].EndsWith(".cs"))
                    {
                        allFileAB.Add(allDepend[j]);
                        allDependPath.Add(allDepend[j]);
                    }
                }
                if (allPrefabDir.ContainsKey(obj.name))
                {
                    Debug.LogError("存在相同名字的Prefab! 名字：" + obj.name);
                }
                else
                {
                    allPrefabDir.Add(obj.name, allDependPath);
                }
            }
        }

        foreach (string name in allFileDir.Keys)
        {
            SetABName(name, allFileDir[name]);
        }

        foreach(string name in allPrefabDir.Keys)
        {
            SetABName(name, allPrefabDir[name]);
        }

        BuildAssetBundle();

        //清理掉设置assetbundleName的痕迹，避免mainfest刷新，影响svn不必要的更新
        string[] oldNameList = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < oldNameList.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(oldNameList[i], true);
            EditorUtility.DisplayProgressBar("清除AB包名称", "名字：" + oldNameList[i], i * 1.0f / oldNameList.Length);
        }

        //在此过程之前避免编辑器的刷新，以避免打包过慢问题
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
    }

    static void SetABName(string name,string path)
    {
        AssetImporter assetImporter = AssetImporter.GetAtPath(path);
        if(assetImporter == null)
        {
            Debug.LogError("不存在此路径文件：" + path);
        }
        else
        {
            assetImporter.assetBundleName = name;
        }
    }

    static void SetABName(string name, List<string> pathes)
    {
        for(int i = 0; i < pathes.Count; i++)
        {
            SetABName(name, pathes[i]);
        }
    }

    static void BuildAssetBundle()
    {
        string[] allNames = AssetDatabase.GetAllAssetBundleNames();
        //key为全路径，value为包名
        Dictionary<string, string> pathDic = new Dictionary<string, string>();
        for(int i = 0; i < allNames.Length; i++)
        {
            string[] allBundlePath = AssetDatabase.GetAssetPathsFromAssetBundle(allNames[i]);
            for(int j = 0; j < allBundlePath.Length; j++)
            {
                if (allBundlePath[j].EndsWith(".cs") || !ValidPath(allBundlePath[j])) continue;
                pathDic.Add(allBundlePath[j], allNames[i]);
            }
        }
        //删除多余已不存在的包或者更名的包
        DeleteAB();


        //生成自己的配置表
        WriteData(pathDic);

        //打包
        string assetBundleDirectory = GameConfig.ABPATH;
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows);
    }

    /// <summary>
    /// 写入数据
    /// </summary>
    /// <param name="resPathDic">key 为全路径，value为包名</param>
    static void WriteData(Dictionary<string,string> resPathDic)
    {
        //创建数据
        AssetBundleConfig assetBundleConfig= new AssetBundleConfig();
        assetBundleConfig.ABList = new List<ABBase>();
        foreach (string path in resPathDic.Keys)
        {
            ABBase aBBase = new ABBase();
            aBBase.Path = path;
            aBBase.Crc = CRC32.GetCRC32(path);
            aBBase.ABName = resPathDic[path];
            aBBase.AssetName = path.Remove(0, path.LastIndexOf("/") + 1);
            aBBase.ABDependce = new List<string>();
            string[] resDependce = AssetDatabase.GetDependencies(path);
            for(int i = 0; i < resDependce.Length; i++)
            {
                string pathTemp = resDependce[i];
                if(pathTemp == path || path.EndsWith(".cs"))
                {
                    continue;
                }
                string abName = "";
                if(resPathDic.TryGetValue(pathTemp,out abName))
                {
                    if (abName == resPathDic[path]) continue;
                    if (!aBBase.ABDependce.Contains(abName))
                    {
                        aBBase.ABDependce.Add(abName);
                    }
                }
            }
            assetBundleConfig.ABList.Add(aBBase);
        }

        //写入XML
        string xmlPath = Application.dataPath + "/AssetbundleConfig.xml";
        WriteReadData.CreateXML<AssetBundleConfig>(xmlPath, assetBundleConfig);

        //写入二进制
        //二进制格式不需要Path数据
        foreach(ABBase ab in assetBundleConfig.ABList)
        {
            ab.Path = "";
        }
        //string binaryPath = BUNDLETARGETPATH + "/AssetbundleConfig.bytes";
        string binaryPath = "Assets/GameData/Data/ABData/AssetBundleConfig.bytes";
        WriteReadData.CreateBinary<AssetBundleConfig>(binaryPath,assetBundleConfig);
    }

    /// <summary>
    /// 删除无用AB包
    /// </summary>
    static void DeleteAB()
    {
        if (!Directory.Exists(GameConfig.ABPATH))
            return;
        string[] allBundlesName = AssetDatabase.GetAllAssetBundleNames();
        DirectoryInfo directoryInfo = new DirectoryInfo(GameConfig.ABPATH);
        FileInfo[] files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
        for(int i = 0; i < files.Length; i++)
        {
            if (ContainABName(files[i].Name, allBundlesName))
            {
                continue;
            }
            else
            {
                Debug.Log("此AB包已经删除或者更名");
                if (File.Exists(files[i].FullName))
                {
                    File.Delete(files[i].FullName);
                }
            }
        }
    }


    /// <summary>
    /// 遍历文件夹里的文件名与设置的所有AB包名判断
    /// </summary>
    /// <param name="name"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    static bool ContainABName(string name,string[] list)
    {
        for(int i = 0; i < list.Length; i++)
        {
            if(name == list[i])
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 是否包含在已经有的AB包里，用来剔除重复AB包
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static bool ContainAllFileAB(string path)
    {
        
        for (int i = 0; i < allFileAB.Count; i++)
        {
            if(path == allFileAB[i]|| (path.Contains(allFileAB[i]) && (path.Replace(allFileAB[i], "")[0]=='/')))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 判断是否为有效路径
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static bool ValidPath(string path)
    {
        for(int i = 0;  i < validFile.Count; i++)
        {
            if (path.Contains(validFile[i]))
            {
                return true;
            }
        }
        return false;
    }
}
