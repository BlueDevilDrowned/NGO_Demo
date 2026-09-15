using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IWindowStackItem
{
    void CloseFromStack();
}

public sealed class WindowStack : MonoBehaviour
{
    private static WindowStack instance;
    private readonly Stack<IWindowStackItem> stack = new();
    public Transform Root
    {
        get { return transform; }
    }
    public static WindowStack Instance {
        get {
            if (instance != null)
            {
                return instance;
            }

            instance = Object.FindFirstObjectByType<WindowStack>();
            if (instance != null)
            {
                instance.NormalizeCanvasTransform();
                return instance;
            }

            var go = new GameObject("WindowStack");
            instance = go.AddComponent<WindowStack>();
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            go.AddComponent<CanvasScaler>();
            instance.NormalizeCanvasTransform();
            DontDestroyOnLoad(go);
            return instance;
        }
    }
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        NormalizeCanvasTransform();
    }

    private void NormalizeCanvasTransform()
    {
        RectTransform rect = transform as RectTransform;
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public bool IsTop(IWindowStackItem item)
    {
        return stack.Count > 0 && ReferenceEquals(stack.Peek(), item);
    }
    public void Push(IWindowStackItem item)
    {
        if (item == null || stack.Contains(item))
        {
            return;
        }
        stack.Push(item);
    }
    public void CloseTop()
    {
        if (stack.Count > 0)
        {
            stack.Pop().CloseFromStack();
        }
    }

    public void Remove(IWindowStackItem item)
    {
        if (stack.Count > 0 && ReferenceEquals(stack.Peek(), item))
        {
            stack.Pop();
        }
    }
}
