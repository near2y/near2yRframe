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

        //Test
        ResourceManager.Instance.AsyncLoadResource("Assets/GameData/Texture/yixiangziqian.min.png",
            OnLoadSpriteTest1, LoadResPriority.RES_SLOW, true);
        ResourceManager.Instance.AsyncLoadResource("Assets/GameData/Texture/zuanshi4.min.png",
            OnLoadSpriteTest2, LoadResPriority.RES_HIGHT, true);
        ResourceManager.Instance.AsyncLoadResource("Assets/GameData/Texture/yidaiqian.min.png",
            OnLoadSpriteTest3, LoadResPriority.RES_HIGHT, true);
    }

    protected void OnLoadSpriteTest1(string path, Object obj, object param1 = null, object param2 = null, object param3 = null)
    {
        if (!Object.ReferenceEquals(obj, null))
        {
            Sprite sp = obj as Sprite;
            m_MainPanel.m_Test1.sprite = sp;
            Debug.Log("图片1加载出来了");
        }
    }

    protected void OnLoadSpriteTest2(string path, Object obj, object param1 = null, object param2 = null, object param3 = null)
    {
        if (!Object.ReferenceEquals(obj, null))
        {
            Sprite sp = obj as Sprite;
            m_MainPanel.m_Test3.sprite = sp;
            Debug.Log("图片2加载出来了");
        }
    }

    protected void OnLoadSpriteTest3(string path, Object obj, object param1 = null, object param2 = null, object param3 = null)
    {
        if (!Object.ReferenceEquals(obj, null))
        {
            Sprite sp = obj as Sprite;
            m_MainPanel.m_Test2.sprite = sp;
            Debug.Log("图片3加载出来了");
        }
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
