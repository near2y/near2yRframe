using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 底层资源加载，不针对需要实例化的资源
/// </summary>
public class ResourceManager :Singletor<ResourceManager>
{
    public bool m_LoadFromAssetBundle = true;


    protected long m_Guid = 0;
    /// <summary>
    /// 缓存引用计数为0的资源列表，防止GC自动回收，达到缓存最大的时候释放这个列表里面最早没用的资源
    /// </summary>
    protected CMapList<ResourceItem> m_NoRefrenceAssetMapList = new CMapList<ResourceItem>();

    /// <summary>
    /// 缓存使用的资源列表
    /// </summary>
    public Dictionary<uint, ResourceItem> AssetDic { get; set; } = new Dictionary<uint, ResourceItem>();

    //中间类，回调类的类对象池
    protected ClassObjectPool<AsyncLoadResParam> m_AsyncLoadResParamPool = new ClassObjectPool<AsyncLoadResParam>(50);
    protected ClassObjectPool<AsyncCallBack> m_AsyncCallBackPool = new ClassObjectPool<AsyncCallBack>(100);

    //mono脚本
    protected MonoBehaviour m_Startmono;
    //正在异步加载的资源列表
    protected List<AsyncLoadResParam>[] m_LoadingAssetList = new List<AsyncLoadResParam>[(int)LoadResPriority.RES_NUM];
    //正在异步加载的Dic
    protected Dictionary<uint, AsyncLoadResParam> m_LoadingAssetDic = new Dictionary<uint, AsyncLoadResParam>();
    //最长连续卡着加载资源的时间，单位微秒
    private const long MAXLOADRESTIME = 200000;

    public void Init(MonoBehaviour mono)
    {
        for(int i = 0; i < (int)LoadResPriority.RES_NUM; i++)
        {
            m_LoadingAssetList[i] = new List<AsyncLoadResParam>();
        }
        m_Startmono = mono;
        m_Startmono.StartCoroutine(AsyncLoadCor());
    }

    /// <summary>
    /// 创建唯一的GUID
    /// </summary>
    /// <returns></returns>
    public long CreateGuid()
    {
        return m_Guid++;
    }

    /// <summary>
    /// 跳场景的时候清空缓存
    /// </summary>
    public void ClearCache()
    {
        List<ResourceItem> tempList = new List<ResourceItem>();
        foreach(ResourceItem item in AssetDic.Values)
        {
            if (item.m_Clear)
            {
                tempList.Add(item);
            }
        }

        foreach (ResourceItem item in tempList)
        {
            DestroyResourceItem(item, true);
        }
        tempList.Clear();
    }

    /// <summary>
    /// 取消异步加载资源
    /// </summary>
    /// <returns>true为完全取消了，false表示可能还有其他相同操作的异步在进行，并没有取消</returns>
    public bool CancleLoad(ResourceObj resObj)
    {
        AsyncLoadResParam para = null;
        if(m_LoadingAssetDic.TryGetValue(resObj.m_Crc,out para) &&m_LoadingAssetList[(int)para.m_Priority].Contains(para))
        {
            for(int i = para.m_CallBackList.Count-1; i >=0; i--)
            {
                AsyncCallBack tempCallBack = para.m_CallBackList[i];
                if(tempCallBack!=null && resObj == tempCallBack.m_ResObj)
                {
                    tempCallBack.Reset();
                    m_AsyncCallBackPool.Recycle(tempCallBack);
                    para.m_CallBackList.Remove(tempCallBack);
                }
            }
            if (para.m_CallBackList.Count <= 0)
            {
                m_LoadingAssetList[(int)para.m_Priority].Remove(para);
                para.Reset();
                m_AsyncLoadResParamPool.Recycle(para);
                m_LoadingAssetDic.Remove(resObj.m_Crc);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 根据ResObj增加引用计数
    /// </summary>
    /// <returns></returns>
    public int IncreaseResourceRef(ResourceObj resObj,int count = 1)
    {
        return resObj != null ? IncreaseResourceRef(resObj.m_Crc, count) : 0;
    }
    /// <summary>
    /// 根据crc增加引用计数
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int IncreaseResourceRef(uint crc,int count = 1)
    {
        ResourceItem item = null;
        if (!AssetDic.TryGetValue(crc, out item) || item == null)
            return 0;

        item.RefCount += count;
        item.m_LastUseTime = Time.realtimeSinceStartup;
        return item.RefCount;
    }
    /// <summary>
    /// 根据ResourceObj减少引用计数
    /// </summary>
    /// <param name="resObj"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int DecreaseResourceRes(ResourceObj resObj, int count = 1)
    {
        return resObj != null ? DecreaseResourceRes(resObj.m_Crc, count) : 0;
    }

    /// <summary>
    /// 根据crc减少引用计数
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int DecreaseResourceRes(uint crc,int count = 1)
    {
        ResourceItem item = null;
        if (!AssetDic.TryGetValue(crc, out item) || item == null)
            return 0;
        item.RefCount -= count;
        return item.RefCount;
    }

    /// <summary>
    /// 预加载资源
    /// </summary>
    /// <param name="path"></param>
    public void PreloadRes(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        uint crc = CRC32.GetCRC32(path);
        ResourceItem item = GetCacheResourceItem(crc,0);
        if (item != null)
        {
            return;
        }
        Object obj = null;
#if UNITY_EDITOR
        if (!!m_LoadFromAssetBundle)
        {
            item = AssetBundleManager.Instance.FindResourceItem(crc);
            if (item != null && item.m_Obj != null)
            {
                obj = item.m_Obj as Object;
            }
            else
            {
                if (item == null)
                {
                    item = new ResourceItem();
                    item.m_Crc = crc;
                }
                obj = LoadAssetByEditor<Object>(path);
            }
        }
#endif  

        if (obj == null)
        {
            item = AssetBundleManager.Instance.LoadResourceAssetBundle(crc);
            if (item != null && item.m_AssetBundle != null)
            {
                obj = item.m_AssetBundle.LoadAsset<Object>(item.m_AssetName);
            }
        }
        CacheResource(path, ref item, crc, obj);
        //跳场景不清空缓存
        item.m_Clear = false;
        ReleaseResource(path, false);
    }

    /// <summary>
    /// 同步加载资源，针对给ObjectManager的接口
    /// </summary>
    /// <param name="path"></param>
    /// <param name="resObj"></param>
    /// <returns></returns>
    public ResourceObj LoadResource(string path, ResourceObj resObj)
    {
        if (resObj == null)
            return null;
        uint crc = resObj.m_Crc == 0 ? CRC32.GetCRC32(path) : resObj.m_Crc;
        ResourceItem item = GetCacheResourceItem(crc);
        if (item != null)
        {
            resObj.m_ResItem = item;
            return resObj;
        }

        Object obj = null;

#if UNITY_EDITOR
        if (!m_LoadFromAssetBundle)
        {
            item = AssetBundleManager.Instance.FindResourceItem(crc);
            if (item != null && item.m_Obj != null)
            {
                obj = item.m_Obj as Object;
            }
            else
            {
                if(item == null)
                {
                    item = new ResourceItem();
                    item.m_Crc = crc;
                }
                obj = LoadAssetByEditor<Object>(path);
            }
        }
#endif
        if (obj == null)
        {
            item = AssetBundleManager.Instance.LoadResourceAssetBundle(crc);
            if (item.m_Obj != null)
            {
                obj = item.m_Obj as Object;
            }
            else
            {
                obj = item.m_AssetBundle.LoadAsset<Object>(item.m_AssetName);
            }
        }

        CacheResource(path, ref item, crc, obj);
        resObj.m_ResItem = item;
        item.m_Clear = resObj.m_bClear;

        return resObj;
    }

    /// <summary>
    /// 同步资源加载，外部直接调用，仅加载不需要实例化的资源，例如Texture,Audio...
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T LoadResource<T>(string path) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        uint crc = CRC32.GetCRC32(path);
        ResourceItem item = GetCacheResourceItem(crc);
        if (item != null)
        {
            return item.m_Obj as T;
        }
        T obj = null;
#if UNITY_EDITOR
        if (!m_LoadFromAssetBundle)
        {
            item = AssetBundleManager.Instance.FindResourceItem(crc);
            if(item != null && item.m_Obj != null)
            {
                obj = item.m_Obj as T;
            }
            else
            {
                if (item == null)
                {
                    item = new ResourceItem();
                    item.m_Crc = crc;
                }
                obj = LoadAssetByEditor<T>(path);
            }
        }
#endif  

        if(obj == null)
        {
            item = AssetBundleManager.Instance.LoadResourceAssetBundle(crc);
            if(item!=null && item.m_AssetBundle != null)
            {
                if(item.m_Obj != null)
                {
                    obj = item.m_Obj as T;
                }
                else
                {
                    obj = item.m_AssetBundle.LoadAsset<T>(item.m_AssetName);
                }
            }
        }
        CacheResource(path, ref item, crc, obj);
        return obj;
    }

    /// <summary>
    /// 根据ResourceObj卸载资源
    /// </summary>
    /// <param name="resObj"></param>
    /// <param name="destroyObj"></param>
    /// <returns></returns>
    public bool ReleaseResource(ResourceObj resObj, bool destroyObj = false)
    {
        if (resObj == null)
            return false;
        //return ReleaseResource(resObj.m_CloneObj, destroyObj);
        ResourceItem item = null;
        if (!AssetDic.TryGetValue(resObj.m_Crc, out item) || item == null)
        {
            Debug.LogError("AssetDic里不存在该资源：" + resObj.m_CloneObj.name);
        }
        //?
        GameObject.Destroy(resObj.m_CloneObj);
        item.RefCount--;
        DestroyResourceItem(item, destroyObj);
        return true;
    }

    /// <summary>
    /// 不需要实例化的资源卸载，根据对象
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="destroy"></param>
    /// <returns></returns>
    public bool ReleaseResource(Object obj,bool destroy = false)
    {
        if(obj == null)
        {
            return false;
        }
        ResourceItem item = null;
        foreach(ResourceItem res in AssetDic.Values)
        {
            if(res.m_Guid == obj.GetInstanceID())
            {
                item = res;
                break;
            }
        }
        if(item == null)
        {
#if UNITY_EDITOR
            Debug.LogError("AssetDic里不存在该资源：" + obj.name + "可能释放了多次");
#endif
            return false;
        }
        item.RefCount--;
        DestroyResourceItem(item, destroy);
        return true;
    }

    /// <summary>
    /// 不需要实例化的资源卸载，根据路径
    /// </summary>
    /// <param name="path"></param>
    /// <param name="destroy"></param>
    /// <returns></returns>
    public bool ReleaseResource(string path,bool destroy = false)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }
        uint crc = CRC32.GetCRC32(path);
        ResourceItem item = null;
        if(!AssetDic.TryGetValue(crc,out item)|| item == null)
        {
            Debug.LogError("AssetDic里不存在该资源：" + path);
        }
        item.RefCount--;
        DestroyResourceItem(item, destroy);
        return true;
    }

    /// <summary>
    /// 缓存加载的资源
    /// </summary>
    /// <param name="path"></param>
    /// <param name="item"></param>
    /// <param name="crc"></param>
    /// <param name="obj"></param>
    /// <param name="refcount"></param>
    void CacheResource(string path ,ref ResourceItem item,uint crc,Object obj,int refcount = 1)
    {
        //缓存太多，清除最早没有使用的资源
        WashOut();
        if(item == null)
        {
            Debug.LogError("ResourceItem is null,path: " + path);
        }

        if(obj == null)
        {
            Debug.LogError("ResourceLoad error: " + path);
        }
        item.m_Obj = obj;
        item.m_Guid = obj.GetInstanceID();
        item.m_LastUseTime = Time.realtimeSinceStartup;
        item.RefCount += refcount;
        ResourceItem oldItem = null;
        if(AssetDic.TryGetValue(item.m_Crc,out oldItem))
        {
            AssetDic[item.m_Crc] = item;
        }
        else
        {
            AssetDic.Add(item.m_Crc, item);
        }
    }

    /// <summary>
    /// 缓存太多，清除最早没有使用的资源
    /// </summary>
    protected void WashOut()
    {
        //当当前内存使用大于百分之80的时候，我们来进行清除最早没用的资源
        //TODO
        //{
        //    if (m_NoRefrenceAssetMapList.Size<= 0)
        //        break;

        //    ResourceItem item= m_NoRefrenceAssetMapList.Back();
        //    DestroyResourceItem(item,true);
        //    m_NoRefrenceAssetMapList.Pop();
        //}
    }

    /// <summary>
    /// 回收一个资源
    /// </summary>
    /// <param name="item"></param>
    /// <param name="destroyCache"></param>
    protected void DestroyResourceItem(ResourceItem item,bool destroyCache = false)
    {
        if (item == null || item.RefCount > 0)
            return;
        if (!AssetDic.Remove(item.m_Crc))
            return;
        if (!destroyCache)
        {
            //m_NoRefrenceAssetMapList.InsertToHead(item);
            return;
        }
        AssetBundleManager.Instance.ReleaseAsset(item);
        ObjectManager.Instance.ClearPoolObject(item.m_Crc);
        if (item.m_Obj != null)
        {
#if UNITY_EDITOR
            Resources.UnloadUnusedAssets();
#endif
            item.m_Obj = null;
        }
    }

#if UNITY_EDITOR
    protected T LoadAssetByEditor<T>(string path) where T : UnityEngine.Object
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
    }
#endif

    /// <summary>
    /// 从缓存在拿取资源对象
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="addrefcount"></param>
    /// <returns></returns>
    ResourceItem GetCacheResourceItem(uint crc,int addrefcount = 1)
    {
        ResourceItem item = null;
        if(AssetDic.TryGetValue(crc,out item))
        {
            if(item != null)
            {
                item.RefCount += addrefcount;
                item.m_LastUseTime = Time.realtimeSinceStartup;

                //if (item.RefCount <= 1)
                //{
                //    m_NoRefrenceAssetMapList.Remove(item);
                //}
            }
        }
        return item;
    }

    /// <summary>
    /// 异步加载资源，仅仅是不需要实例化的资源，例如音频，图片等等
    /// </summary>
    public void AsyncLoadResource(string path, OnAsyncObjFinish dealFinish, LoadResPriority resPriority, uint crc = 0, object param1 = null, object param2 = null, object param3= null)
    {
        if (crc == 0)
        {
            crc = CRC32.GetCRC32(path);
        }
        ResourceItem item = GetCacheResourceItem(crc);
        if(item != null)
        {
            if(dealFinish != null)
            {
                dealFinish(path, item.m_Obj, param1, param2, param3);
            }
            return;
        }
        //判断是否在加载中
        AsyncLoadResParam para = null;
        if(!m_LoadingAssetDic.TryGetValue(crc,out para) || para == null)
        {
            para = m_AsyncLoadResParamPool.Spwan(true);
            para.m_Crc = crc;
            para.m_Path = path;
            para.m_Priority = resPriority;
            m_LoadingAssetDic.Add(crc, para);
            m_LoadingAssetList[(int)resPriority].Add(para);
        }

        //往回调列表里面加回调
        AsyncCallBack callBack = m_AsyncCallBackPool.Spwan(true);
        callBack.m_DealObjFinish = dealFinish;
        callBack.m_Param1 = param1;
        callBack.m_Param2 = param2;
        callBack.m_Param3 = param3;
        para.m_CallBackList.Add(callBack);
    }


    public void AsyncLoadResource(string path,ResourceObj resObj,OnAsyncFinish dealfinish,LoadResPriority priority)
    {
        ResourceItem item = GetCacheResourceItem(resObj.m_Crc);
        if(item != null)
        {
            resObj.m_ResItem = item;
            if(dealfinish != null)
            {
                dealfinish(path,resObj,resObj.m_Param1,resObj.m_Param2,resObj.m_Param3);
            }
            return;
        }
        //判断是否在加载中
        AsyncLoadResParam para = null;
        if (!m_LoadingAssetDic.TryGetValue(resObj.m_Crc, out para) || para == null)
        {
            para = m_AsyncLoadResParamPool.Spwan(true);
            para.m_Crc = resObj.m_Crc;
            para.m_Path = path;
            para.m_Priority = priority;
            m_LoadingAssetDic.Add(resObj.m_Crc, para);
            m_LoadingAssetList[(int)priority].Add(para);
        }

        //往回调列表里面加回调
        AsyncCallBack callBack = m_AsyncCallBackPool.Spwan(true);
        callBack.m_DealFinish = dealfinish;
        callBack.m_ResObj = resObj;
        para.m_CallBackList.Add(callBack);
    }

    /// <summary>
    /// 异步加载
    /// ?一直在运行
    /// </summary>
    /// <returns></returns>
    IEnumerator AsyncLoadCor()
    {
        long lastYiledTime = System.DateTime.Now.Ticks;
        List<AsyncCallBack> callBacks = null;
        bool hadYiled = false;
        while (true)
        {
            //上一次yield的时间
            for  (int i = 0; i < (int)LoadResPriority.RES_NUM; i++)
            {
                List<AsyncLoadResParam> loadingList = m_LoadingAssetList[i];
                if (loadingList.Count <= 0)
                    continue;
                AsyncLoadResParam loadingItem = loadingList[0];
                loadingList.RemoveAt(0);
                callBacks = loadingItem.m_CallBackList;

                Object obj = null;
                ResourceItem item = null;
#if UNITY_EDITOR
                if (!m_LoadFromAssetBundle)
                {
                    obj = LoadAssetByEditor<Object>(loadingItem.m_Path);
                    //模拟异步加载
                    yield return new WaitForSeconds(0.5f);
                    item = AssetBundleManager.Instance.FindResourceItem(loadingItem.m_Crc);
                }
#endif
                if(obj == null)
                {
                    item = AssetBundleManager.Instance.LoadResourceAssetBundle(loadingItem.m_Crc);
                    if(item != null && item.m_AssetBundle)
                    {
                        AssetBundleRequest abRequest = null;
                        if (loadingItem.m_Sprite)
                        {
                            abRequest = item.m_AssetBundle.LoadAssetAsync<Sprite>(item.m_AssetName);
                        }
                        else
                        {
                            abRequest = item.m_AssetBundle.LoadAssetAsync(item.m_AssetName);
                        }
                        yield return abRequest;
                        if (abRequest.isDone)
                        {
                            obj = abRequest.asset;
                        }
                        lastYiledTime = System.DateTime.Now.Ticks;
                    }
                }

                CacheResource(loadingItem.m_Path, ref item, loadingItem.m_Crc, obj, callBacks.Count);

                for (int j = 0; j < callBacks.Count; j++)
                {
                    AsyncCallBack callBack = callBacks[j];
                    if(callBack!=null && callBack.m_DealFinish != null && callBack.m_ResObj !=null)
                    {
                        ResourceObj tempResObj = callBack.m_ResObj;
                        tempResObj.m_ResItem = item;
                        callBack.m_DealFinish(loadingItem.m_Path, tempResObj, tempResObj.m_Param1, tempResObj.m_Param2, tempResObj.m_Param3);
                        callBack.m_DealFinish = null;
                        tempResObj = null;
                    }

                    if(callBack != null && callBack.m_DealObjFinish != null)
                    {
                        callBack.m_DealObjFinish(loadingItem.m_Path, obj, callBack.m_Param1, callBack.m_Param2, callBack.m_Param3);
                        callBack.m_DealObjFinish = null;
                    }
                    callBack.Reset();
                    m_AsyncCallBackPool.Recycle(callBack);
                }
                obj = null;
                callBacks.Clear();
                m_LoadingAssetDic.Remove(loadingItem.m_Crc);

                loadingItem.Reset();
                m_AsyncLoadResParamPool.Recycle(loadingItem);

                if (System.DateTime.Now.Ticks - lastYiledTime > MAXLOADRESTIME)
                {
                    yield return null;
                    lastYiledTime = System.DateTime.Now.Ticks;
                    hadYiled = true;
                }
            }

            if (hadYiled || System.DateTime.Now.Ticks - lastYiledTime > MAXLOADRESTIME)
            {
                lastYiledTime = System.DateTime.Now.Ticks;
                yield return null;
            }
        }
    }
}

//优先级
public enum LoadResPriority 
{ 
    RES_HIGHT = 0,  //最高优先级
    RES_MIDDLE,     //一般优先级
    RES_SLOW,         //低优先级
    RES_NUM,            
}

/// <summary>
/// 资源加载完成回调
/// </summary>
/// <param name="path"></param>
/// <param name="obj"></param>
/// <param name="param1"></param>
/// <param name="param2"></param>
/// <param name="param3"></param>
public delegate void OnAsyncObjFinish(string path, Object obj, object param1 = null, object param2 = null, object param3 = null);

public delegate void OnAsyncFinish(string path, ResourceObj resObj, object param1 = null, object param2 = null, object param3 = null);

/// <summary>
/// 异步加载资源的参数
/// </summary>
public class AsyncLoadResParam
{
    public List<AsyncCallBack> m_CallBackList = new List<AsyncCallBack>();
    public uint m_Crc;
    public string m_Path;
    public bool m_Sprite = false;
    public LoadResPriority m_Priority = LoadResPriority.RES_SLOW;

    public void Reset()
    {
        m_CallBackList.Clear();
        m_Crc = 0;
        m_Path = string.Empty;
        m_Sprite = false;
        m_Priority = LoadResPriority.RES_SLOW;
    }
}

public class AsyncCallBack
{
    /// <summary>
    /// 针对ObjectManager加载完成的回调
    /// </summary>
    public OnAsyncFinish m_DealFinish = null;
    /// <summary>
    /// ObjcectManager中间类
    /// </summary>
    public ResourceObj m_ResObj = null;
    //------------------------------------------------------------------

    //加载完成的回调
    public OnAsyncObjFinish m_DealObjFinish = null;
    //回调参数
    public object m_Param1 = null, m_Param2 = null, m_Param3 = null;

    public void Reset()
    {
        m_DealObjFinish = null;
        m_DealFinish = null;
        m_ResObj = null;
        m_Param1 = null;
        m_Param2 = null;
        m_Param3 = null;
    }
}

#region 双向链表底层
//双向链表结构节点
public class DoubleLinkedListNode<T> where T : class, new()
{
    //前一个节点
    public DoubleLinkedListNode<T> prev = null;
    //后一个节点
    public DoubleLinkedListNode<T> next = null;
    //当前节点
    public T t = null;
}

//双向链表结构
public class DoubleLinkedList<T> where T : class, new()
{
    //表头
    public DoubleLinkedListNode<T> Head = null;
    //表尾
    public DoubleLinkedListNode<T> Tail = null;
    //双向链表结构类对象池
    protected ClassObjectPool<DoubleLinkedListNode<T>> m_DoubleLinkedNodePool =
            ObjectManager.Instance.GetOrCreateClassPool<DoubleLinkedListNode<T>>(500);
    //个数
    protected int m_Count = 0;
    public int Count
    {
        get { return m_Count; }
    }

    /// <summary>
    /// 添加一个节点到头部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToHeader(T t)
    {
        DoubleLinkedListNode<T> pNode = m_DoubleLinkedNodePool.Spwan(true);
        pNode.next = null;
        pNode.prev = null;
        pNode.t = t;
        return AddToHeader(pNode);
        
    }

    /// <summary>
    /// 添加一个节点到头部
    /// </summary>
    /// <param name="pNode"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToHeader(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null)
            return null;
        pNode.prev = null;
        if(Head == null)
        {
            Head = Tail = pNode;
        }
        else
        {
            pNode.next = Head;
            Head.prev = pNode;
            Head = pNode;
        }
        m_Count++;
        return Head;
    }

    /// <summary>
    /// 添加节点到尾部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToTail(T t)
    {
        DoubleLinkedListNode<T> pNode = m_DoubleLinkedNodePool.Spwan(true);
        pNode.next = null;
        pNode.prev = null;
        pNode.t = t;
        return AddToTail(pNode);
    }

    /// <summary>
    /// 添加节点到尾部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToTail(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null)
            return null;
        pNode.next = null;
        if(Tail == null)
        {
            Head = Tail = pNode;
        }
        else
        {
            pNode.prev = Tail;
            Tail.next = pNode;
            Tail = pNode;
        }
        m_Count++;
        return Tail;
    }

    /// <summary>
    /// 移除某个节点
    /// </summary>
    /// <param name="pNode"></param>
    public void RemoveNode(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null)
            return;
        if (pNode == Head)
            Head = pNode.next;

        if (pNode == Tail)
            Tail = pNode.prev;

        if (pNode.prev != null)
            pNode.prev.next = pNode.next;

        if (pNode.next != null)
            pNode.next.prev = pNode.prev;

        pNode.next = pNode.prev = null;
        pNode.t = null;
        m_DoubleLinkedNodePool.Recycle(pNode);
        m_Count--;
    }

    /// <summary>
    /// 把某个节点移动到头部
    /// </summary>
    /// <param name="pNode"></param>
    public  void MoveToHead(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null || pNode == Head)
            return;
        if (pNode.prev == null && pNode.next == null)
            return;
        if(pNode == Tail)
            Tail = pNode.prev;
        if (pNode.prev != null)
            pNode.prev.next = pNode.next;
        if (pNode.next != null)
            pNode.next.prev = pNode.prev;

        pNode.next = null;
        pNode.next = Head;
        Head.prev = pNode;
        Head = pNode;
        if(Tail == null)
        {
            Tail = Head;
        }
    }

}
#endregion

#region 双向链表封装
public class CMapList<T> where T :class, new()
{
    DoubleLinkedList<T> m_Dlink = new DoubleLinkedList<T>();
    Dictionary<T, DoubleLinkedListNode<T>> m_FindMap = new Dictionary<T, DoubleLinkedListNode<T>>();

    ~CMapList()
    {
        Clear();
    }

    /// <summary>
    /// 清空列表
    /// </summary>
    public void Clear()
    {
        while(m_Dlink.Tail != null)
        {
            Remove(m_Dlink.Tail.t);
        }
    }

    /// <summary>
    /// 插入一个节点到表头
    /// </summary>
    /// <param name="t"></param>
    public void InsertToHead(T t)
    {
        DoubleLinkedListNode<T> node = null;
        if(m_FindMap.TryGetValue(t,out node) && node != null)
        {
            m_Dlink.AddToHeader(node);
            return;
        }
        m_Dlink.AddToHeader(t);
        m_FindMap.Add(t, m_Dlink.Head);
    }

    /// <summary>
    /// 从表尾弹出一个节点
    /// </summary>
    public void Pop()
    {
        if(m_Dlink.Tail != null)
        {
            Remove(m_Dlink.Tail.t);
        }
    }

    /// <summary>
    /// 移除一个节点
    /// </summary>
    /// <param name="t"></param>
    public void Remove(T t)
    {
        DoubleLinkedListNode<T> node = null;
        if (!m_FindMap.TryGetValue(t, out node) || node == null)
            return;
        m_Dlink.RemoveNode(node);
        m_FindMap.Remove(t);
    }

    /// <summary>
    /// 获取尾部节点
    /// </summary>
    /// <returns></returns>
    public T Back()
    {
        return m_Dlink.Tail == null ? null : m_Dlink.Tail.t;
    }

    /// <summary>
    /// 返回节点个数
    /// </summary>
    public int Size
    {
        get{
            return m_FindMap.Count;
        }
    }

    /// <summary>
    /// 查找是否存在该节点
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public bool Find(T t)
    {
        DoubleLinkedListNode<T> node = null;
        if (!m_FindMap.TryGetValue(t, out node) || node == null)
            return false;
        return true;
    }

    /// <summary>
    /// 刷新某个节点，把节点移动到头部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public bool Refresh(T t)
    {
        DoubleLinkedListNode<T> node = null;
        if (!m_FindMap.TryGetValue(t, out node) || node == null)
            return false;
        m_Dlink.MoveToHead(node);
        return true;
    }
}
#endregion

#region Object与Resource中的桥梁
public class ResourceObj
{
    /// <summary>
    /// 存ResourceItem
    /// </summary>
    public ResourceItem m_ResItem = null;
    /// <summary>
    /// 路径对应crc
    /// </summary>
    public uint m_Crc = 0;
    /// <summary>
    /// 实例化出来的Object
    /// </summary>
    public GameObject m_CloneObj = null;
    /// <summary>
    /// 是否跳场景清楚
    /// </summary>
    public bool m_bClear = true;
    /// <summary>
    /// 储存GUID
    /// </summary>
    public long m_Guid = 0;
    /// <summary>
    /// 是否已经放回对象池
    /// </summary>
    public bool m_Already = false;
    /// <summary>
    /// 离线数据
    /// </summary>
    public OfflineData m_OfflineData = null;

    //---------------------------------异步需要
    /// <summary>
    /// 是否放在场景节点下面
    /// </summary>
    public bool m_SetSceneParent = false;

    /// <summary>
    /// 实例化资源加载完成回调
    /// </summary>
    public OnAsyncObjFinish m_dealFinish = null;

    /// <summary>
    /// 异步参数
    /// </summary>
    public object m_Param1, m_Param2, m_Param3 = null;


    public void Reset()
    {
        m_Crc = 0;
        m_CloneObj = null;
        m_bClear = true;
        m_Guid = 0;
        m_Already = false;
        m_SetSceneParent = false;
        m_dealFinish = null;
        m_OfflineData = null;
    }
}
#endregion  