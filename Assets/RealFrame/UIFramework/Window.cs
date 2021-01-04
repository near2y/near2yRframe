using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Window
{
    /// <summary>
    /// 窗口的名字
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// 引用GameObject
    /// </summary>
    public GameObject GameObject { get; set; }
    /// <summary>
    /// 引用Transform
    /// </summary>
    public Transform Transform { get; set; }
    /// <summary>
    /// 所有的Btn
    /// </summary>
    protected List<Button> m_AllBtns = new List<Button>();
    /// <summary>
    /// 所有的Toggle
    /// </summary>
    protected List<Toggle> m_AllToggles = new List<Toggle>();
    /// <summary>
    /// 创建界面调用到的函数
    /// </summary>
    public virtual void OnAwake(params object[] paraList) { }
    /// <summary>
    /// 界面展示时调用到的函数
    /// </summary>
    public virtual void OnShow(params object[] paraList) { }
    /// <summary>
    /// 界面隐藏时调用到的函数
    /// </summary>
    public virtual void OnDisable() { }
    /// <summary>
    /// 界面Update
    /// </summary>
    public virtual void OnUpdate() { }
    /// <summary>
    /// 界面关闭时调用到的函数
    /// </summary>
    public virtual void OnClose() 
    {
        RemoveAllBtnListener();
        RemoveAllToggleListener();
        m_AllBtns.Clear();
        m_AllToggles.Clear();
    }

    /// <summary>
    /// 同步替换图片
    /// </summary>
    /// <param name="path"></param>
    /// <param name="img"></param>
    /// <param name="setNativeSize"></param>
    /// <returns></returns>
    public bool ChangeImageSprite(string path,Image img,bool setNativeSize = false)
    {
        if (Image.ReferenceEquals(img, null))
            return false;
        Sprite sp = ResourceManager.Instance.LoadResource<Sprite>(path);
        if (!Sprite.ReferenceEquals(sp, null))
        {
            if (!Sprite.ReferenceEquals(img.sprite, null))
                img.sprite = null;
            img.sprite = sp;
            if (setNativeSize)
            {
                img.SetNativeSize();
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 异步替换图片
    /// </summary>
    /// <param name="path"></param>
    /// <param name="img"></param>
    /// <param name="setNativeSize"></param>
    public void ChangeImageSpriteAsync(string path,Image img,bool setNativeSize = false)
    {
        if (Image.ReferenceEquals(img, null))
            return;
        ResourceManager.Instance.AsyncLoadResource(path, OnloadSpriteFinish, LoadResPriority.RES_MIDDLE,true, 0,img, setNativeSize);
    }

    /// <summary>
    /// 图片加载完成回调
    /// </summary>
    /// <param name="path"></param>
    /// <param name="obj"></param>
    /// <param name="param1"></param>
    /// <param name="param2"></param>
    /// <param name="param3"></param>
    protected void OnloadSpriteFinish(string path, Object obj, object param1 = null, object param2 = null, object param3 = null)
    {
        if (!Object.ReferenceEquals(obj, null))
        {
            Sprite sp = obj as Sprite;
            Image img = param1 as Image;
            bool setNativeSize = (bool)param2;
            if (!Sprite.ReferenceEquals(sp, null))
            {
                if (!Sprite.ReferenceEquals(img.sprite, null))
                    img.sprite = null;
                img.sprite = sp;
                if (setNativeSize)
                {
                    img.SetNativeSize();
                }
            }
        }
    }
    /// <summary>
    /// 通过name ,以及UIMsgID执行事件
    /// </summary>
    /// <param name="msgID"></param>
    /// <param name="paraList"></param>
    /// <returns></returns>
    public virtual bool OnMessage(UIMsgID msgID,params object[] paraList) { return true; }
        
    /// <summary>
    /// 移除所有的btn事件
    /// </summary>
    public void RemoveAllBtnListener()
    {
        foreach (Button btn in m_AllBtns)
        {
            btn.onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// 移除所有的toggle事件
    /// </summary>
    public void RemoveAllToggleListener()
    {
        foreach (Toggle toggle in m_AllToggles)
        {
            toggle.onValueChanged.RemoveAllListeners();
        }
    }

    /// <summary>
    /// 添加按钮事件监听
    /// 添加时会移除原来所有的事件监听
    /// </summary>
    /// <param name="btn">对应的btn</param>
    /// <param name="actionList">一般第一个是点击事件，第二个是声音事件</param>
    public void AddBtnClickListener(Button btn,params UnityAction[] actionList)
    {
        if (!Button.ReferenceEquals(btn, null))
        {
            if (!m_AllBtns.Contains(btn))
            {
                m_AllBtns.Add(btn);
            }
            btn.onClick.RemoveAllListeners();
            foreach (UnityAction action in actionList)
            {
                btn.onClick.AddListener(action);
            }
        }
    }

    /// <summary>
    /// 添加Toggle监听事件
    /// 会移除原来所有的监听事件
    /// </summary>
    /// <param name="toggle">对应Toggle</param>
    /// <param name="actionList">一般第一个是事件，第二个作为声音事件</param>
    public void AddToggleClickListener(Toggle toggle, UnityAction<bool>[] actionList)
    {
        if (!Toggle.ReferenceEquals(toggle, null))
        {
            if (!m_AllToggles.Contains(toggle))
            {
                m_AllToggles.Add(toggle);
            }
            toggle.onValueChanged.RemoveAllListeners();
            foreach (UnityAction<bool> action in actionList)
            {
                toggle.onValueChanged.AddListener(action);
            }
        }
    }

}
