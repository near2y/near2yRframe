﻿using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 游戏开始测试
/// </summary>
public class GameStart : MonoBehaviour
{
    //public AudioClip clip;
    //public AudioSource m_Audio;
    //public List<GameObject> objList = new List<GameObject>();
    //public ClassObjectPool<Enemy> m_EnemyClassPool = ObjectManager.Instance.GetOrCreateClassPool<Enemy>(1000);

    public RectTransform m_UIRoot = null;
    public RectTransform m_WindowRoot = null;
    public Camera m_UICamera= null;
    public EventSystem m_UIEventSystem = null;



    private void Awake()
    {
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
        ResourceManager.Instance.PreloadRes(path);

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



public class Enemy 
{
    public long id = 0;
    public string name = string.Empty;
    public float hp = 0;
    public float attack = 0;


    public void Reset()
    {
        id = 0;
        name = string.Empty;
        hp = 0;
        attack = 0;
    }
}