using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using static DebugLogger;

public interface IInitializable {
    bool IsInitialized { get; }
    void Initialize();
}

public abstract class InitializableComponent : MonoBehaviour, IInitializable {
    public bool IsInitialized { get; protected set; }

    protected virtual void Awake() { }

    public virtual void Initialize() {
        IsInitialized = true;
    }
}

public class InitializationManager : Singleton<InitializationManager> {
    private Dictionary<System.Type, IInitializable> componentsByType = new Dictionary<System.Type, IInitializable>();
    private HashSet<IInitializable> initializedComponents = new HashSet<IInitializable>();
    private bool systemInitialized;

    public UnityEvent OnSystemInitialized { get; } = new UnityEvent();

    protected override void Awake() {
        base.Awake();
        Log("InitializationManager Awake", LogTag.Initialization);
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterComponent<T>(T component) where T : IInitializable {
        if (component == null) {
            LogError("Attempted to register null component", LogTag.Initialization);
            return;
        }

        var type = component.GetType();
        if (!componentsByType.ContainsKey(type)) {
            componentsByType[type] = component;
            Log($"Registered component type: {type.Name}", LogTag.Initialization);
        } else {
            LogWarning($"Component of type {type.Name} already registered", LogTag.Initialization);
        }

        if (component.IsInitialized) {
            initializedComponents.Add(component);
            Log($"Component {type.Name} registered as initialized", LogTag.Initialization);
            CheckSystemInitialization();
        }
    }

    public void InitializeComponent<T>(T component) where T : IInitializable {
        if (component == null) {
            LogError("Attempted to initialize null component", LogTag.Initialization);
            return;
        }

        var type = component.GetType();
        if (!componentsByType.ContainsKey(type)) {
            RegisterComponent(component);
        }

        if (!component.IsInitialized) {
            Log($"Starting initialization of {type.Name}", LogTag.Initialization);
            component.Initialize();

            if (component.IsInitialized) {
                initializedComponents.Add(component);
                Log($"Successfully initialized {type.Name}", LogTag.Initialization);
                CheckSystemInitialization();
            } else {
                LogWarning($"Component {type.Name} Initialize() called but IsInitialized is still false",
                    LogTag.Initialization);
            }
        }
    }

    public bool IsComponentInitialized<T>() where T : class {
        var type = typeof(T);
        if (componentsByType.TryGetValue(type, out var component)) {
            bool isInit = initializedComponents.Contains(component);
            Log($"Checking initialization status of {type.Name}: {isInit}", LogTag.Initialization);
            return isInit;
        }
        LogWarning($"Component type {type.Name} not registered", LogTag.Initialization);
        return false;
    }

    public bool IsComponentInitialized(IInitializable component) {
        if (component == null) return false;
        return initializedComponents.Contains(component);
    }

    private void CheckSystemInitialization() {
        if (!systemInitialized) {
            // Log the state of all registered components
            foreach (var kvp in componentsByType) {
                Log($"Component {kvp.Key.Name} - Initialized: {initializedComponents.Contains(kvp.Value)}",
                    LogTag.Initialization);
            }

            if (componentsByType.All(kvp => initializedComponents.Contains(kvp.Value))) {
                systemInitialized = true;
                OnSystemInitialized.Invoke();
                Log("System initialization complete", LogTag.Initialization);
            }
        }
    }

    protected override void OnDestroy() {
        componentsByType.Clear();
        initializedComponents.Clear();
        OnSystemInitialized.RemoveAllListeners();
        base.OnDestroy();
    }
}