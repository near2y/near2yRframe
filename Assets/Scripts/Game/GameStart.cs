using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 游戏开始测试
/// </summary>
public class GameStart : MonoSingletor<GameStart>
{

    public RectTransform m_UIRoot = null;
    public RectTransform m_WindowRoot = null;
    public Camera m_UICamera= null;
    public EventSystem m_UIEventSystem = null;


    protected override void Awake()
    {
        base.Awake();
        GameObject.DontDestroyOnLoad(gameObject);
        AssetBundleManager.Instance.LoadAssetBundleConfig();
        ResourceManager.Instance.Init(this);
        GameMapManager.Instance.Init(this);
        ObjectManager.Instance.Init(transform.Find("RecyclePoolTrs"), transform.Find("SceneTrs"));
        UIManager.Instance.Init(m_UIRoot, m_WindowRoot, m_UICamera, m_UIEventSystem);
        RegisterUI();
    }


    private void Start()
    {

        string path = "Assets/GameData/Sounds/menusound.mp3";
        //ResourceManager.Instance.AsyncLoadResource(path, OnloadFinish, LoadResPriority.RES_MIDDLE);

        //ResourceManager.Instance.PreloadRes(path);
        AudioClip clip =  ResourceManager.Instance.LoadResource<AudioClip>(path);
        ResourceManager.Instance.ReleaseResource(clip);

        ObjectManager.Instance.PreLoadGameObject("Assets/GameData/Prefabs/Attack.prefab", 5);
        GameObject obj = ObjectManager.Instance.InstantiateObject("Assets/GameData/Prefabs/Attack.prefab", true);
        //GameObject obj = ObjectManager.Instance.InstantiateObject("Assets/GameData/Prefabs/Attack.prefab", true,false);
        //ObjectManager.Instance.ReleaseObject(obj);
        //obj = null;

        GameMapManager.Instance.LoadScene(GameConfig.SCENENAME_MENUSCENE);


    }

    private void Update()
    {
        UIManager.Instance.OnUpdate();
    }

    private void RegisterUI()
    {
        UIManager.Instance.Register<MenuUI>(GameConfig.UIPATH_MENU);
        UIManager.Instance.Register<GameUI>(GameConfig.UIPATH_GAME);
        UIManager.Instance.Register<LoadingUI>(GameConfig.UIPATH_LOAD);
    }

}