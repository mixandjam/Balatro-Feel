using UnityEngine;
using static DebugLogger;

public abstract class Singleton<T> : InitializableComponent where T : InitializableComponent {
    protected static T instance;
    private static bool isQuitting = false;

    public static T Instance {
        get {
            if (isQuitting) {
                LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.", LogTag.Initialization);
                return null;
            }

            if (instance == null) {
                instance = FindObjectOfType<T>();

                if (instance == null) {
                    LogError($"{typeof(T).Name} not found in scene!", LogTag.Initialization);
                }
            }
            return instance;
        }
    }

    protected override void Awake() {
        base.Awake();

        isQuitting = false;

        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }
        instance = this as T;
    }

    protected virtual void OnDestroy() {
        if (instance == this) {
            instance = null;
        }
    }

    protected virtual void OnApplicationQuit() {
        isQuitting = true;
    }

    // This is to reset when entering play mode in editor
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() {
        isQuitting = false;
        instance = null;
    }
}