﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ObjectManager :Singletor<ObjectManager> 
{
    /// <summary>
    /// 对象池节点
    /// </summary>
    public Transform RecyclePoolTrs;
    /// <summary>
    /// 场景节点
    /// </summary>
    public Transform SceneTrs;
    /// <summary>
    /// 对象池
    /// </summary>
    protected Dictionary<uint, List<ResourceObj>> m_ObjectPoolDic = new Dictionary<uint, List<ResourceObj>>();
    /// <summary>
    /// ResourceObj类对象池
    /// </summary>
    protected ClassObjectPool<ResourceObj> m_ResourceObjClassPool = null;
    /// <summary>
    /// 暂存ResourceObj
    /// </summary>
    protected Dictionary<int, ResourceObj> m_ResourceObjDic = new Dictionary<int, ResourceObj>();
    /// <summary>
    /// 根据异步的guid储存resourceObj，来判断是否正在异步加载
    /// </summary>
    protected Dictionary<long, ResourceObj> m_AsyncResObjs = new Dictionary<long, ResourceObj>();
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="recyclePool">回收节点</param>
    /// <param name="scene">场景默认节点</param>
    public void Init(Transform recyclePool,Transform scene)
    {
        m_ResourceObjClassPool =  GetOrCreateClassPool<ResourceObj>(1000);
        RecyclePoolTrs = recyclePool;
        SceneTrs = scene;
    }

    /// <summary>
    /// 清空对象池
    /// </summary>
    public void ClearCache()
    {
        List<uint> tempList = new List<uint>();
        foreach(uint key in m_ObjectPoolDic.Keys)
        {
            List<ResourceObj> st = m_ObjectPoolDic[key];
            for(int i = st.Count-1; i>=0; i--)
            {
                ResourceObj resObj = st[i];
                if(!System.Object.ReferenceEquals(resObj.m_CloneObj,null) && resObj.m_bClear)
                {
                    GameObject.Destroy(resObj.m_CloneObj);
                    m_ResourceObjDic.Remove(resObj.m_CloneObj.GetInstanceID());
                    resObj.Reset();
                    m_ResourceObjClassPool.Recycle(resObj);
                    st.Remove(resObj);
                }
            }
            if (st.Count <= 0)
            {
                tempList.Add(key);
            }
        }
        for(int i = 0; i < tempList.Count; i++)
        {
            uint temp = tempList[i];
            if (m_ObjectPoolDic.ContainsKey(temp))
            {
                m_ObjectPoolDic.Remove(temp);
            }
        }
        tempList.Clear();
    }

    /// <summary>
    /// 清除某个资源在对象池中所有的对象
    /// </summary>
    /// <param name="crc"></param>
    public void ClearPoolObject(uint crc)
    {
        List<ResourceObj> st = null;
        if (!m_ObjectPoolDic.TryGetValue(crc, out st) || st == null)
            return;
        for (int i = st.Count - 1; i >= 0; i--)
        {
            ResourceObj resObj = st[i];
            if (resObj.m_bClear)
            {
                st.Remove(resObj);
                int tempID = resObj.m_CloneObj.GetInstanceID();
                GameObject.Destroy(resObj.m_CloneObj);
                resObj.Reset();
                m_ResourceObjDic.Remove(tempID);
                m_ResourceObjClassPool.Recycle(resObj);
            }
        }
        if (st.Count <= 0)
        {
            m_ObjectPoolDic.Remove(crc);
        }
    }

    /// <summary>
    /// 根据实例化对象直接获取离线数据
    /// </summary>
    /// <param name="obj">GameObject</param>
    /// <returns></returns>
    public OfflineData FindOfflineData(GameObject obj)
    {
        OfflineData data = null;
        ResourceObj resObj = null;
        m_ResourceObjDic.TryGetValue(obj.GetInstanceID(), out resObj);
        if(resObj != null)
        {
            data = resObj.m_OfflineData;
        }
        return data;
    }

    /// <summary>
    /// 从对象池取对象
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    protected ResourceObj GetObjectFromPool(uint crc)
    {
        List<ResourceObj> st = null;
        if(m_ObjectPoolDic.TryGetValue(crc,out st) && st != null && st.Count>0)
        {
            ResourceManager.Instance.IncreaseResourceRef(crc);
            ResourceObj resObj = st[0];
            st.RemoveAt(0);
            GameObject obj = resObj.m_CloneObj;
            if (!System.Object.ReferenceEquals(obj, null))
            {
                if (!System.Object.ReferenceEquals(resObj.m_OfflineData, null))
                {
                    resObj.m_OfflineData.ResetProp();
                }
                resObj.m_Already = false;
#if UNITY_EDITOR
                if (obj.name.EndsWith("(Recycle)"))
                {
                    obj.name = obj.name.Replace("(Recycle)","");
                }
#endif
            }
            return resObj;
        }
        return null;
    }

    /// <summary>
    /// 取消异步加载
    /// </summary>
    /// <param name="guid"></param>
    public void CancleLoad(long guid)
    {
        ResourceObj resObj = null;
        if(m_AsyncResObjs.TryGetValue(guid,out resObj) && ResourceManager.Instance.CancleLoad(resObj))
        {
            m_AsyncResObjs.Remove(guid);
            resObj.Reset();
            m_ResourceObjClassPool.Recycle(resObj);
        }
    }

    /// <summary>
    /// 是否正在异步加载
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public bool IsingAsyncLoad(long guid)
    {
        return m_AsyncResObjs[guid] != null;
    }

    /// <summary>
    /// 该对象是否是对象池创建的
    /// </summary>
    /// <returns></returns>
    public bool IsObjectManagerCreate(GameObject obj)
    {
        ResourceObj resObj = m_ResourceObjDic[obj.GetInstanceID()];
        return resObj == null ? false : true;
    }

    /// <summary>
    /// 预加载GameObject
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="count">预加载个数</param>
    /// <param name="clear">跳场景是否清除</param>
    public void PreLoadGameObject(string path,int count =1,bool clear = false)
    {
        List<GameObject> tempGameObjectList = new List<GameObject>();
        for(int i = 0; i < count; i++)
        {
            GameObject obj = InstantiateObject(path, false, clear);
            tempGameObjectList.Add(obj);
        }

        for(int i = 0; i < tempGameObjectList.Count; i++)
        {
            GameObject obj = tempGameObjectList[i];
            ReleaseObject(obj);
            obj = null;
        }
        tempGameObjectList.Clear();
    }

    /// <summary>
    /// 同步加载
    /// </summary>
    /// <param name="path"></param>
    /// <param name="bClear"></param>
    /// <returns></returns>
    public GameObject InstantiateObject(string path,bool setSceneObj = false,bool bClear = true)
    {
        uint crc = CRC32.GetCRC32(path);
        ResourceObj resourceObj = GetObjectFromPool(crc);
        if(resourceObj == null)
        {
            resourceObj = m_ResourceObjClassPool.Spwan(true);
            resourceObj.m_Crc = crc;
            resourceObj.m_bClear = bClear;
            resourceObj = ResourceManager.Instance.LoadResource(path, resourceObj);
            if(!System.Object.ReferenceEquals(resourceObj.m_ResItem.m_Obj,null))
            {
                resourceObj.m_CloneObj = GameObject.Instantiate(resourceObj.m_ResItem.m_Obj) as GameObject;
                resourceObj.m_OfflineData = resourceObj.m_CloneObj.GetComponent<OfflineData>();
            }
        }
        if (setSceneObj)
        {
            resourceObj.m_CloneObj.transform.SetParent(SceneTrs);
        }
        int tempID = resourceObj.m_CloneObj.GetInstanceID();
        if (!m_ResourceObjDic.ContainsKey(tempID))
        {
            m_ResourceObjDic.Add(tempID, resourceObj);
        }
        return resourceObj.m_CloneObj;
    }



    /// <summary>
    /// 异步对象加载
    /// </summary>
    /// <param name="path"></param>
    /// <param name="dealFinish"></param>
    /// <param name="priority"></param>
    /// <param name="setSceneObject"></param>
    /// <param name="param1"></param>
    /// <param name="param2"></param>
    /// <param name="param3"></param>
    /// <param name="bClear"></param>
    public long InstantiateObjectAsync(string path,OnAsyncObjFinish dealFinish,LoadResPriority priority,
        bool setSceneObject = false,object param1 = null,object param2 = null,object param3 = null, bool bClear = true)
    {
        if (string.IsNullOrEmpty(path))
            return 0;
        uint crc = CRC32.GetCRC32(path);
        ResourceObj resObj = GetObjectFromPool(crc);
        if(resObj != null)
        {
            if (setSceneObject)
            {
                resObj.m_CloneObj.transform.SetParent(SceneTrs, false);
            }
            if(dealFinish != null)
            {
                dealFinish(path, resObj.m_CloneObj, param1, param2, param3);
            }
            return resObj.m_Guid;
        }
        long guid = ResourceManager.Instance.CreateGuid();
        resObj = m_ResourceObjClassPool.Spwan(true);
        resObj.m_Crc = crc;
        resObj.m_bClear = bClear;
        resObj.m_SetSceneParent = setSceneObject;
        resObj.m_dealFinish = dealFinish;
        resObj.m_Param1 = param1;
        resObj.m_Param2 = param2;
        resObj.m_Param3 = param3;
        resObj.m_Guid = guid;
        ResourceManager.Instance.AsyncLoadResource(path, resObj, OnLoadResourceObjFinish, priority);
        return guid;
    }

    /// <summary>
    /// 实例化资源加载完成回调
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="resObj">中间类</param>
    /// <param name="param1">参数1</param>
    /// <param name="param2">参数2</param>
    /// <param name="param3">参数3</param>
    void OnLoadResourceObjFinish(string path,ResourceObj resObj,object param1=null, object param2 = null, object param3 = null)
    {
        if (resObj == null)
            return;
        if (resObj.m_ResItem.m_Obj == null)
        {
#if UNITY_EDITOR
            Debug.LogError("异步资源加载的资源为空" + path);
#endif
        }
        else
        {
            resObj.m_CloneObj = GameObject.Instantiate(resObj.m_ResItem.m_Obj) as GameObject;
            resObj.m_OfflineData = resObj.m_CloneObj.GetComponent<OfflineData>();
        }

        //加载完成就从正在加载的异步中移除
        if (m_AsyncResObjs.ContainsKey(resObj.m_Guid))
        {
            m_AsyncResObjs.Remove(resObj.m_Guid);
        }

        if (resObj.m_CloneObj !=null && resObj.m_SetSceneParent)
        {
            resObj.m_CloneObj.transform.SetParent(SceneTrs);
        }
        //?
        //放在resObj.m_dealFinish != null里?
        int tempID = resObj.m_CloneObj.GetInstanceID();
        if (!m_ResourceObjDic.ContainsKey(tempID))
        {
            m_ResourceObjDic.Add(tempID, resObj);
        }
        if (resObj.m_dealFinish != null)
        {
            resObj.m_dealFinish(path, resObj.m_CloneObj, param1, param2, param3);
        }
    }

    /// <summary>
    /// 资源释放
    /// </summary>
    /// <param name="obj">释放对象</param>
    /// <param name="maxCacheCount">最大缓冲数量，默认-1无限数量</param>
    /// <param name="destroyCache">是否清理掉缓存，默认否</param>
    /// <param name="recycleParent">是否重新放回到回收节点下</param>
    public void ReleaseObject(GameObject obj,int maxCacheCount =-1,bool destroyCache = false,bool recycleParent = true)
    {
        if (System.Object.ReferenceEquals(obj, null))
            return;
        ResourceObj resObj = null;
        int tempID = obj.GetInstanceID();
        if (!m_ResourceObjDic.TryGetValue(tempID, out resObj))
        {
            Debug.Log(obj.name + "对象不是ObjectManager创建的!");
            return;
        }
        if (resObj == null)
        {
            Debug.LogError("缓存的Resourceobj为空");
        }
        if (resObj.m_Already)
        {
            Debug.LogError("该对象已经放回对象池了，检查自己是否清空引用！");
            return;
        }
#if UNITY_EDITOR
        obj.name += "(Recycle)";
#endif

        List<ResourceObj> st = null;
        if(maxCacheCount == 0)
        {
            m_ResourceObjDic.Remove(tempID);
            ResourceManager.Instance.ReleaseResource(resObj, destroyCache);
            resObj.Reset();
            m_ResourceObjClassPool.Recycle(resObj);
        }
        else
        {
            if(!m_ObjectPoolDic.TryGetValue(resObj.m_Crc,out st) || st == null)
            {
                st = new List<ResourceObj>();
                m_ObjectPoolDic.Add(resObj.m_Crc, st);
            }
            if (resObj.m_CloneObj)
            {
                if (recycleParent)
                {
                    resObj.m_CloneObj.transform.SetParent(RecyclePoolTrs);
                }
                else
                {
                    resObj.m_CloneObj.SetActive(false);
                }
            }
            if(maxCacheCount<0 || st.Count < maxCacheCount)
            {
                st.Add(resObj);
                resObj.m_Already = true;
                ResourceManager.Instance.DecreaseResourceRes(resObj);
            }
            else
            {
                m_ResourceObjDic.Remove(tempID);
                ResourceManager.Instance.ReleaseResource(resObj, destroyCache);
                resObj.Reset();
                m_ResourceObjClassPool.Recycle(resObj);
            }
        }
    }

    #region 类对象池的使用
    protected Dictionary<Type,object> m_ClassPoolDic = new Dictionary<Type,object>();

    /// <summary>
    /// 创建类对象池，创建完成以后外面可以保存ClassObjectPool<T>
    /// 创建后可以调用Spwan和Recycle来创建和回收类对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="maxCount"></param>
    /// <returns></returns>
    public ClassObjectPool<T> GetOrCreateClassPool<T>(int maxCount)where T : class, new()
    {
        Type type = typeof(T);
        object outObj = null;
        if(!m_ClassPoolDic.TryGetValue(type,out outObj) || outObj == null)
        {
            ClassObjectPool<T> newPool = new ClassObjectPool<T>(maxCount);
            m_ClassPoolDic.Add(type, newPool);
            return newPool;
        }
        return outObj as ClassObjectPool<T>;
    }
    #endregion
}
