using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuUI : Window
{
    private MenuPanel m_MainPanel;
    private AudioSource m_Audio = null;

    public override void OnAwake(params object[] paraList)
    {
        m_MainPanel = GameObject.GetComponent<MenuPanel>();
        AddBtnClickListener(m_MainPanel.m_StartBtn, OnClickStart);
        AddBtnClickListener(m_MainPanel.m_BgmBtn, OnClickBGM);
    }

    void OnClickStart()
    {
        UIManager.Instance.CloseWindow(Name);
        UIManager.Instance.PopUpWindow("GamePanel.prefab");
    }

    void OnClickBGM()
    {
        if (m_Audio == null)
        {
            m_Audio = GameObject.AddComponent<AudioSource>();
        }

        if (m_Audio.clip != null)
        {
            m_Audio.Stop();
            ResourceManager.Instance.ReleaseResource(m_Audio.clip);
            m_Audio.clip = null;
            m_MainPanel.m_BgmText.text = "Play BGM";
        }
        else
        {
            AudioClip clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/GameData/Sounds/menusound.mp3");
            m_Audio.clip = clip;
            m_Audio.Play();
            m_MainPanel.m_BgmText.text = "Stop BGM";
        }

    }

}
