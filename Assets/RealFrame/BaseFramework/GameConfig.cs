using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GameConfig
{
    //-------------------------------Config --------------------------------------
    /// <summary>
    /// 最大缓存个数
    /// </summary>
    public const int MAXCACHECOUNT = 1000;




    //------------------------------- AB ---------------------------------------
    /// <summary>
    /// AB打包路径
    /// </summary>
    //public const string ABPATH = "Assets/AssetBundles/";
    public static string ABPATH = Application.dataPath + "/../AssetBundle/" + EditorUserBuildSettings.activeBuildTarget.ToString()+"/";
    /// <summary>
    /// AB打包配置表存放路径
    /// </summary>
    public const string ABCONFIGPATH = "Assets/AssetBundles/assetbundleconfig";
    /// <summary>
    /// ABConfig.asset路径
    /// 配置了所有需要打包文件的位置
    /// </summary>
    public const string ABCONFIGASSETPATH = "Assets/RealFrame.Editor/Editor/Resource/ABConfig.asset";
    /// <summary>
    /// ABConfig二进制文件存放路径
    /// </summary>
    public const string ABBINARYPATH= "Assets/GameData/Data/ABData/AssetBundleConfig.bytes";

    //--------------------------------  UI  -------------------------------------
    public const string UIPATH_MENU = "MenuPanel.prefab";
    public const string UIPATH_GAME = "GamePanel.prefab";
    public const string UIPATH_LOAD  = "LoadingPanel.prefab";


    //--------------------------------  Scene ----------------------------------------
    public const string SCENENAME_EMPTY = "Empty";
    public const string SCENENAME_MENUSCENE= "MenuScene";

}
