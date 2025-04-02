using UnityEngine;

// ReSharper disable StaticMemberInGenericType

public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
{


    private static T _instance;
    private static readonly object InstanceLock = new();
    private static bool _quitting;

    /// <summary>
    /// DontDestroyOnLoad Controller
    /// </summary>
    protected virtual bool PersistThroughScenes => false;

    public static T Instance
    {
        get
        {
            lock (InstanceLock)
            {
                if (_instance == null && !_quitting)
                {
                    _instance = GameObject.FindAnyObjectByType<T>();
                    if (_instance == null)
                    {
                        var go = new GameObject(typeof(T).ToString());
                        _instance = go.AddComponent<T>();

                        // DontDestroyOnLoad(_instance.gameObject);
                    }
                }
                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = gameObject.GetComponent<T>();
            if (PersistThroughScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (_instance.GetInstanceID() != GetInstanceID())
        {
            Destroy(gameObject);
            Debug.LogError($"[{typeof(T)}] Instance already exists! Destroying duplicate: {gameObject.name}");
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _quitting = true;
    }
}