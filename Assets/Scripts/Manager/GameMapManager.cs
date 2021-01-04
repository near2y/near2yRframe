using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMapManager : Singletor<GameMapManager>
{
    /// <summary>
    /// 加载场景完成回调
    /// </summary>
    public Action LoadSceneOverCallBack;

    /// <summary>
    /// 加载场景开始回调
    /// </summary>
    public Action LoadSceneEnterCallBack;

    /// <summary>
    /// 当前场景的名字
    /// </summary>
    public string CurrentMapName { get; set; }

    /// <summary>
    /// 切换场景进度条
    /// </summary>
    public static int LoadingProgress = 0;

    private MonoBehaviour m_Mono;

    /// <summary>
    /// 场景管理初始化函数
    /// </summary>
    /// <param name="mono"></param>
    public void Init(MonoBehaviour mono)
    {
        m_Mono = mono;
    }

    /// <summary>
    /// 场景加载完毕
    /// </summary>
    public bool AlreadyLoadScene { get; set; } = false;

    /// <summary>
    /// 加载场景
    /// </summary>
    /// <param name="name">场景名</param>
    public void LoadScene(string name,int index = 1)
    {
        LoadingProgress = 0;
        m_Mono.StartCoroutine(LoadSceneAsync(name));

        //Loading 界面之一
        switch (index)
        {
            case 1:
                UIManager.Instance.PopUpWindow(GameConfig.UIPATH_LOAD, true, name);
                break;
        }
    }

    /// <summary>
    /// 设置场景环境
    /// </summary>
    /// <param name="name"></param>
    void SetSceneSetting(string name)
    {
        //TODO
        //设置各种场景环境，可以根据配表来
    }


    IEnumerator LoadSceneAsync(string name)
    {
        if (!Action.ReferenceEquals(LoadSceneEnterCallBack,null))
        {
            LoadSceneEnterCallBack();
        }
        ClearCache();
        AlreadyLoadScene = false;
        AsyncOperation unloadScene = SceneManager.LoadSceneAsync(GameConfig.SCENENAME_EMPTY, LoadSceneMode.Single);
        while(!AsyncOperation.ReferenceEquals(unloadScene,null) && !unloadScene.isDone)
        {
            yield return new WaitForEndOfFrame();
        }
        LoadingProgress = 0;
        int targetProgress = 0;
        AsyncOperation asyncScene = SceneManager.LoadSceneAsync(name);
        if(!AsyncOperation.ReferenceEquals(asyncScene,null) && !asyncScene.isDone)
        {
            asyncScene.allowSceneActivation = false;
            while (asyncScene.progress<0.9f)
            {
                targetProgress = (int)asyncScene.progress * 100;
                yield return new WaitForEndOfFrame();
                //smooth
                while (LoadingProgress < targetProgress)
                {
                    Debug.Log("near2y " + LoadingProgress);
                    ++LoadingProgress;
                    yield return new WaitForEndOfFrame();
                }
            }
            CurrentMapName = name;
            SetSceneSetting(name);
            targetProgress = 100;
            while (LoadingProgress <targetProgress - 2)
            {
                ++LoadingProgress;
                yield return new WaitForEndOfFrame();
            }
            LoadingProgress = 100;
            asyncScene.allowSceneActivation = true;
            AlreadyLoadScene = true;
            if (!Action.ReferenceEquals(LoadSceneOverCallBack, null))
            {
                LoadSceneOverCallBack();
            }

        }
    }

    /// <summary>
    /// 跳转场景之前清理场景缓存
    /// </summary>
    private void ClearCache()
    {
        ObjectManager.Instance.ClearCache();
        ResourceManager.Instance.ClearCache();
    }
}
