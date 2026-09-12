using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IWindowStackItem { void CloseFromStack(); }

public sealed class WindowStack : MonoBehaviour
{
    private static WindowStack instance;
    private readonly Stack<IWindowStackItem> stack = new();
    public Transform Root => transform;
    public static WindowStack Instance {
        get {
            if (instance != null) return instance;
            var go = new GameObject("WindowStack");
            instance = go.AddComponent<WindowStack>();
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            go.AddComponent<CanvasScaler>();
            DontDestroyOnLoad(go);
            return instance;
        }
    }
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public bool IsTop(IWindowStackItem item) => stack.Count > 0 && ReferenceEquals(stack.Peek(), item);
    public void Push(IWindowStackItem item) { if (item != null) stack.Push(item); }
    public void CloseTop() { if (stack.Count > 0) stack.Pop().CloseFromStack(); }
    public void Remove(IWindowStackItem item) { if (stack.Count > 0 && ReferenceEquals(stack.Peek(), item)) stack.Pop(); }
}
