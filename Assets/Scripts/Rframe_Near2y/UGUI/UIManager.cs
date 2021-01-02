using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum UIMsgID 
{
    None = 0,

}

public class UIManager : Singletor<UIManager>
{
    private const string UIPREFABPATH = "Assets/GameData/Prefabs/UGUI/Panel/";
    /// <summary>
    /// UI节点
    /// </summary>
    private RectTransform m_UIRoot;
    /// <summary>
    /// 窗口节点
    /// </summary>
    private RectTransform m_WindowRoot;
    /// <summary>
    /// UI摄像机
    /// </summary>
    private Camera m_UICamera;
    /// <summary>
    /// EventSystem节点
    /// </summary>
    private EventSystem m_EventSystem;
    /// <summary>
    /// 屏幕宽高比
    /// </summary>
    private float m_CanvasRate = 0;
    /// <summary>
    /// 所有打开的窗开
    /// </summary>
    private Dictionary<string, Window> m_WindowDic = new Dictionary<string, Window>();
    /// <summary>
    /// 注册的窗口字典
    /// key:名字 
    /// values:类型
    /// </summary>
    private Dictionary<string, System.Type> m_RegisterDic = new Dictionary<string, System.Type>();
    /// <summary>
    /// List储存所有的打开窗口
    /// </summary>
    private List<Window> m_WindowList = new List<Window>();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="uiRoot"></param>
    /// <param name="windowRoot"></param>
    /// <param name="uiCamera"></param>
    public void Init(RectTransform uiRoot,RectTransform windowRoot,Camera uiCamera,EventSystem eventSystem)
    {
        m_UIRoot = uiRoot;
        m_WindowRoot = windowRoot;
        m_UICamera = uiCamera;
        m_EventSystem = eventSystem;
        m_CanvasRate = Screen.height / (m_UICamera.orthographicSize * 2);
    }

    /// <summary>
    /// 窗口注册方法
    /// </summary>
    /// <typeparam name="T">窗口泛型类</typeparam>
    /// <param name="name">窗口名字</param>
    public void Register<T>(string name)where T:Window
    {
        m_RegisterDic[name] = typeof(T);
    }
    /// <summary>
    /// 根据窗口名查找窗口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public T FindWindowByName<T>(string name) where T:Window
    {
        Window wnd = null;
        if(m_WindowDic.TryGetValue(name,out wnd))
        {
            return (T)wnd;
        }
         return null;
    }

    /// <summary>
    /// 打开指定窗口
    /// </summary>
    /// <param name="wndName">窗口名字</param>
    /// <param name="bTop">是否置顶</param>
    /// <returns></returns>
    public Window PopUpWindow(string wndName,bool bTop = true,params object[] paraList)
    {
        Window wnd = FindWindowByName<Window>(wndName);
        if(wnd == null)
        {
            System.Type tp = null;
            if(m_RegisterDic.TryGetValue(wndName,out tp))
            {
                //有这个注册的类型就通过这个类型创建这个类
                wnd = System.Activator.CreateInstance(tp) as Window;
            }
            else
            {
                Debug.LogError("找不到窗口对应的脚本，窗口名是："+wndName);
                return null;
            }
            GameObject wndObj = ObjectManager.Instance.InstantiateObject(UIPREFABPATH + wndName, false, false);
            if(System.Object.ReferenceEquals(wndObj,null))
            {
                Debug.LogError("创建窗口Prefab失败：" + wndName);
                return null;
            }
            if (!m_WindowDic.ContainsKey(wndName))
            {
                m_WindowDic.Add(wndName, wnd);
                m_WindowList.Add(wnd);
            }
#if UNITY_EDITOR
            wndObj.name = wndName;
#endif
            wnd.GameObject = wndObj;
            wnd.GameObject.SetActive(true);
            wnd.Transform = wndObj.transform;
            wnd.Name = wndName;
            wnd.OnAwake(paraList);
            wndObj.transform.SetParent(m_WindowRoot, false);

            if (bTop)
            {
                wndObj.transform.SetAsLastSibling();
            }
            wnd.OnShow(paraList);
        }
        else
        {
            ShowWindow(wndName, bTop, paraList);
        }
        return wnd;
    }

    /// <summary>
    /// 根据名字关闭窗口
    /// </summary>
    /// <param name="name"></param>
    /// <param name="destroy"></param>
    public void CloseWindow(string name,bool destroy = false)
    {
        Window wnd = FindWindowByName<Window>(name);
        CloseWindow(wnd, destroy);
    }

    /// <summary>
    /// 根据窗口对象关闭窗口
    /// </summary>
    /// <param name="wnd"></param>
    /// <param name="destroy"></param>
    public void CloseWindow(Window wnd, bool destroy = false)
    {
        if(wnd != null)
        {
            wnd.OnDisable();
            wnd.OnClose();
            if (m_WindowDic.ContainsKey(wnd.Name))
            {
                m_WindowDic.Remove(wnd.Name);
                m_WindowList.Remove(wnd);
            }
            if (destroy)
            {
                ObjectManager.Instance.ReleaseObject(wnd.GameObject, 0, true);
            }
            else
            {
                ObjectManager.Instance.ReleaseObject(wnd.GameObject,recycleParent:false);
            }
            wnd.GameObject = null;
            wnd = null;
        }
    }

    /// <summary>
    /// 关闭所有窗口
    /// </summary>
    public void CloseAllWindow()
    {
        for(int i = m_WindowList.Count -1; i >= 0; i--)
        {
            CloseWindow(m_WindowList[i]);
        }
    }

    /// <summary>
    /// 显示或隐藏所有UI
    /// 不会调用到OnShow和OnDisable里的方法，只是隐藏或显示
    /// </summary>
    /// <param name="show"></param>
    public void ShowOrHideUI(bool show)
    {
        if(!RectTransform.ReferenceEquals(m_UIRoot,null))
        {
            m_UIRoot.gameObject.SetActive(show);
        }
    }

    /// <summary>
    /// 设置默认选择对象
    /// </summary>
    /// <param name="obj"></param>
    public void SetNormalSelectObj(GameObject obj)
    {
        if (EventSystem.ReferenceEquals(m_EventSystem, null))
        {
            m_EventSystem = EventSystem.current;
        }
        m_EventSystem.firstSelectedGameObject = obj;
    }

    /// <summary>
    /// 打开的窗口更新
    /// </summary>
    public void OnUpdate()
    {
        for(int i = 0; i < m_WindowList.Count; i++)
        {
            Window wnd = m_WindowList[i];
            if(wnd != null)
            {
                wnd.OnUpdate();
            }
        }
    }

    /// <summary>
    /// 切换到唯一窗口
    /// </summary>
    public void SwitchWindowByName(string name,bool bTop = true,params object[] paraList)
    {
        CloseAllWindow();
        PopUpWindow(name, bTop, paraList);
    }

    /// <summary>
    /// 发送消息给窗口
    /// </summary>
    /// <param name="name">窗口名</param>
    /// <param name="msgID">消息ID</param>
    /// <param name="paraList">参数数组</param>
    /// <returns></returns>
    public bool SendMessageToWnd(string name,UIMsgID msgID = UIMsgID.None,params object[] paraList)
    {
        Window wnd = FindWindowByName<Window>(name);
        if(wnd!= null)
        {
            return wnd.OnMessage(msgID, paraList);
        }
        return false;
    }

    /// <summary>
    /// 根据窗口名字显示窗口
    /// </summary>
    /// <param name="name"></param>
    /// <param name="paraList"></param>
    public void ShowWindow(string name,bool bTop = true,params object[] paraList)
    {
        Window wnd = FindWindowByName<Window>(name);
        ShowWindow(wnd,bTop, paraList);
    }

    /// <summary>
    /// 根据名字隐藏窗口
    /// </summary>
    /// <param name="name"></param>
    public void HideWindow(string name)
    {
        Window wnd = FindWindowByName<Window>(name);
        HideWindow(wnd);
    }

    /// <summary>
    /// 根据window对象隐藏窗口
    /// </summary>
    /// <param name="wnd"></param>
    public void HideWindow(Window wnd)
    {
        if(wnd != null)
        {
            wnd.GameObject.SetActive(false);
            wnd.OnDisable();
        }
    }

    /// <summary>
    /// 根据窗口对象显示窗口
    /// </summary>
    /// <param name="wnd"></param>
    /// <param name="paraList"></param>
    public void ShowWindow(Window wnd,bool bTop = true, params object[] paraList)
    {
        if(wnd != null)
        {
            if (!System.Object.ReferenceEquals(wnd.GameObject, null) && !wnd.GameObject.activeSelf) wnd.GameObject.SetActive(true);
            if (bTop) wnd.Transform.SetAsLastSibling();
            wnd.OnShow(paraList);
        }
    }
}
