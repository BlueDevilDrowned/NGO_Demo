using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class InteractionOptionsUI : MonoBehaviour
{
    private InteractSystem interactSystem;
    [SerializeField] private Transform content;
    [SerializeField] private InteractionOptionView optionPrefab;
    private readonly List<InteractionOptionView> labels=new();
    public void Bind(InteractSystem system) => interactSystem=system;
    public static InteractionOptionsUI Create(InteractSystem system)
    {
        var prefab=Resources.Load<InteractionOptionsUI>("UI/InteractionOptionsUI");
        if(prefab!=null) { var instance=Instantiate(prefab); instance.Bind(system); return instance; }
        return CreateDefault(system);
    }
    public static InteractionOptionsUI CreateDefault(InteractSystem system)
    {
        var root=new GameObject("InteractionOptionsUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder=30;
        var scaler=root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);
        var panel=new GameObject("Options",typeof(RectTransform),typeof(VerticalLayoutGroup));
        panel.transform.SetParent(root.transform,false);
        var rect=panel.GetComponent<RectTransform>();
        rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f);
        rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(40,-40);
        rect.sizeDelta=new Vector2(340,300);
        var layout=panel.GetComponent<VerticalLayoutGroup>();
        layout.childControlHeight=true; layout.childForceExpandHeight=false;
        layout.spacing=6;
        var ui=root.AddComponent<InteractionOptionsUI>();
        ui.content=panel.transform;
        ui.optionPrefab=Resources.Load<InteractionOptionView>("UI/InteractionOption");
        ui.Bind(system);
        return ui;
    }
    private void LateUpdate()
    {
        if(interactSystem==null || content==null) return;
        var options=interactSystem.CurrentOptions;
        while(labels.Count<options.Count)
        {
            labels.Add(optionPrefab!=null ? Instantiate(optionPrefab,content) : InteractionOptionView.CreateDefault(content));
        }
        for(int i=0;i<labels.Count;i++)
        {
            labels[i].gameObject.SetActive(i<options.Count);
            if(i>=options.Count) continue;
            labels[i].Set(options[i],i==interactSystem.SelectedOptionIndex);
        }
    }
}
