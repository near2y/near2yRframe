using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoSingletor<T>: MonoBehaviour where T: MonoSingletor<T>
{
    protected static T m_Instance;
    public static T Instance { get { return m_Instance; } }

    protected virtual void Awake()
    {
        if(m_Instance == null)
        {
            m_Instance = (T)this;
        }
        else
        {
            Debug.LogError("Get a second instance of this class " + this.GetType());
        }
    }

}
