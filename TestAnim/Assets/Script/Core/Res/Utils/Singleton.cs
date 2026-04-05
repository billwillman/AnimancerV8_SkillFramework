/*
 * 单例
 */

using UnityEngine;
using System;
using System.Collections;
using Utils;


// 关于资源的类不允许使用单例
public class Singleton<T> where T : class, new()
{
    //
    // Static Fields
    //
    protected static T m_Instance;

    //
    // Static Properties
    //
    public static T Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = new T();
            }
            return m_Instance;
        }
    }

    //
    // Static Methods
    //
    public static T GetInstance()
    {
        return Instance;
    }
}

public class SingetonMono<T> : CachedMonoBehaviour where T : CachedMonoBehaviour
{

	protected virtual void Awake()
	{
        DontDestroyOnLoad(this.gameObject);
		m_Instance = this as T;
	}

    public static T Instance
    {
        get
        {
            if (m_IsDestroy)
                return null;

            if (m_Instance == null)
            {
                m_Instance = GameObject.FindFirstObjectByType<T>(); // 这个要注意在场景切换的时候如果调用会有概率Crash，m_IsDestroy判断了OnDestroy，不要处理
                if (m_Instance == null) {
                    GameObject gameObj = new GameObject();
                    m_Instance = gameObj.AddComponent<T>();
                }
            }

            return m_Instance;
        }
    }

    public static T GetInstance()
    {
        return Instance;
    }

    public static void DestroyInstance()
    {
        if (m_Instance == null || m_IsDestroy)
            return;
        GameObject gameObj = m_Instance.CachedGameObject;
        ResourceMgr.Instance.DestroyObject(gameObj);
    }

    void OnDestroy() {
        m_IsDestroy = true;
    }

    public static bool IsDestroy {
        get {
            return m_IsDestroy;
        }
    }

    protected static T m_Instance = null;
    protected static bool m_IsDestroy = false;
}
