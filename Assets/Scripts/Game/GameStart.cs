using System.Collections;
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
        UIManager.Instance.PopUpWindow("MenuPanel.prefab");

        string path = "Assets/GameData/Sounds/menusound.mp3";
        //ResourceManager.Instance.AsyncLoadResource(path, OnloadFinish, LoadResPriority.RES_MIDDLE);
        ResourceManager.Instance.PreloadRes(path);

    }

    private void RegisterUI()
    {
        UIManager.Instance.Register<MenuUI>(UIName.Menu);
        UIManager.Instance.Register<GameUI>(UIName.Game);
        UIManager.Instance.Register<LoadingUI>(UIName.Loading);
    }


}

public class UIName :Singletor<UIName>
{
    public const string Menu = "MenuPanel.prefab";
    public const string Game = "GamePanel.prefab";
    public const string Loading = "LoadingPanel.prefab";
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
