using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingUI : Window
{
    private LoadingPanel m_Panel;
    private string m_SceneName;

    public override void OnAwake(params object[] paraList)
    {
        m_Panel = GameObject.GetComponent<LoadingPanel>();
        m_SceneName = (string)paraList[0];
    }

    public override void OnUpdate()
    {
        if (LoadingPanel.ReferenceEquals(m_Panel, null))
            return;
        m_Panel.m_LoadingBar.value = GameMapManager.LoadingProgress / 100.0f;
        m_Panel.m_LoadingValue.text = string.Format("{0}%", GameMapManager.LoadingProgress);
        if (GameMapManager.LoadingProgress >= 100)
        {
            LoadOtherScene();
        }
    }

    /// <summary>
    /// 加载对应场景第一个UI
    /// </summary>
    public void LoadOtherScene()
    {

    }
}
