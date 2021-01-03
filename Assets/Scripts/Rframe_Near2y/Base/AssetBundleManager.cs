using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class AssetBundleManager : Singletor<AssetBundleManager>
{
    //资源关系依赖配表，可以根据crc来找到对应资源块
    protected Dictionary<uint, ResourceItem> m_ResourceItemDic = new Dictionary<uint, ResourceItem>();
    //储存已加载的AB包，key为crc
    protected Dictionary<uint, AssetBundleItem> m_AssetBundleItemDic = new Dictionary<uint, AssetBundleItem>();
    //AssetBundleItem类对象池
    protected ClassObjectPool<AssetBundleItem> m_AssetBundleItemPool = ObjectManager.Instance.GetOrCreateClassPool<AssetBundleItem>(500);

    /// <summary>
    /// 加载AB配置表
    /// </summary>
    /// <returns></returns>
    public bool LoadAssetBundleConfig()
    {
        m_ResourceItemDic.Clear();
        m_AssetBundleItemDic.Clear();
        string configPath = GameConfig.ABCONFIGPATH;
        AssetBundle configAB = AssetBundle.LoadFromFile(configPath);
        TextAsset textAsset = configAB.LoadAsset<TextAsset>("AssetbundleConfig");
        if (textAsset == null)
        {
            Debug.LogError("AssetBundleConfig is no exit!");
            return false;
        }


        AssetBundleConfig assetBundleConfig = WriteReadData.ReadBinary<AssetBundleConfig>(textAsset);

        for (int i = 0; i < assetBundleConfig.ABList.Count; i++)
        {
            ABBase temp = assetBundleConfig.ABList[i];
            ResourceItem item = new ResourceItem();
            item.m_Crc = temp.Crc;
            item.m_AssetName = temp.AssetName;
            item.m_ABName = temp.ABName;
            item.m_DependAssetBundle = temp.ABDependce;
            item.m_FullPath = temp.Path;
            if (m_ResourceItemDic.ContainsKey(item.m_Crc))
            {
                Debug.LogError("重复的Crc  资源名：" + item.m_AssetName + "ab包名：" + item.m_ABName);
            }
            else
            {
                m_ResourceItemDic.Add(item.m_Crc, item);
            }
        }
        return true;
    }

    /// <summary>
    /// 根据路径的crc加载中间类ResourceItem
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    public ResourceItem LoadResourceAssetBundle(uint crc)
    {
        ResourceItem item = null;
        if(!m_ResourceItemDic.TryGetValue(crc,out item) || item ==null){
            Debug.LogError(string.Format("LoadResourceAssetBundle error: can not find crc {0} in AssetBundleConfig", crc.ToString()));
            return item;
        }

        if(item.m_AssetBundle != null)
        {
            return item;
        }

        item.m_AssetBundle = LoadAssetBundle(item.m_ABName);

        if(item.m_DependAssetBundle != null)
        {
            for(int i = 0; i< item.m_DependAssetBundle.Count; i++)
            {
                LoadAssetBundle(item.m_DependAssetBundle[i]);
            }
        }
        return item;
    }

    /// <summary>
    /// 加载单个AssetBundle，根据名字
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private AssetBundle LoadAssetBundle(string name)
    {
        AssetBundleItem item = null;
        uint crc = CRC32.GetCRC32(name);

        if(!m_AssetBundleItemDic.TryGetValue(crc,out item))
        {
            AssetBundle assetBundle = null;
            string fullPath = GameConfig.ABPATH + name;
            if (File.Exists(fullPath))
            {
                assetBundle = AssetBundle.LoadFromFile(fullPath);
            }
            if (assetBundle == null)
            {
                Debug.LogError(" load AssetBundle Error:" + fullPath);
            }
            item = m_AssetBundleItemPool.Spwan(true);
            item.assetBundle = assetBundle;
            item.RefCount++;
            m_AssetBundleItemDic.Add(crc, item);
        }
        else
        {
            item.RefCount++;
        }
        return item.assetBundle;

    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="item"></param>
    public void ReleaseAsset(ResourceItem item)
    {
        if(item == null)
        {
            return;
        }
        if(item.m_DependAssetBundle !=null && item.m_DependAssetBundle.Count > 0)
        {
            for(int i = 0; i < item.m_DependAssetBundle.Count; i++)
            {
                UnloadAssetBundle(item.m_DependAssetBundle[i]);
            }
        }
        UnloadAssetBundle(item.m_ABName);
    }

    private void UnloadAssetBundle(string name)
    {
        AssetBundleItem item = null;
        uint crc = CRC32.GetCRC32(name);
        if(m_AssetBundleItemDic.TryGetValue(crc,out item) && item != null)
        {
            item.RefCount--;
            if (item.RefCount <= 0 && item.assetBundle !=null)
            {
                item.assetBundle.Unload(true);
                item.Rest();
                m_AssetBundleItemPool.Recycle(item);
                m_AssetBundleItemDic.Remove(crc);
            }
        }
    }

    /// <summary>
    /// 根据crc查找ResourceItem
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    public ResourceItem FindResourceItem(uint crc)
    {
        ResourceItem item = null;
        m_ResourceItemDic.TryGetValue(crc, out item);
        return item;
    }
}


public class AssetBundleItem
{
    public AssetBundle assetBundle = null;
    public int RefCount = 0;

    public void Rest()
    {
        assetBundle = null;
        RefCount = 0;
    }

}

public class ResourceItem
{
    //资源路径的CRC
    public uint m_Crc = 0;
    //资源的文件名
    public string m_AssetName = string.Empty;
    //资源所在的AssetBundle
    public string m_ABName = string.Empty;
    //资源所依赖的AssetBundle
    public List<string> m_DependAssetBundle = null;
    //---------------------------------------------------------------------------
    //---------------------------------------------------------------------------

    //资源唯一标识
    public int m_Guid = 0;
    //资源加载完的AB包
    public AssetBundle m_AssetBundle = null;
    //路径
    public string m_FullPath = string.Empty;
    //资源对象
    public Object m_Obj = null;
    //资源最后使用的时间
    public float m_LastUseTime = 0.0f;
    //引用计数
    protected int m_RefCount = 0;
    //是否跳场景清理
    public bool m_Clear = true;
    public int RefCount
    {
        get
        {
            return m_RefCount;
        }

        set
        {
            m_RefCount = value;
            if (m_RefCount < 0)
            {
                Debug.LogError("refcount < 0 " + m_RefCount + " ," + (m_Obj != null ? m_Obj.name : "name is null"));
            }
        }
    }
}
