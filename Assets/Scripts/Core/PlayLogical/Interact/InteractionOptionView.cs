using UnityEngine;
using UnityEngine.UI;
public sealed class InteractionOptionView : MonoBehaviour
{
    [SerializeField] private Text label;
    [SerializeField] private Image icon;
    [SerializeField] private GameObject selectedMarker;
    public static InteractionOptionView CreateDefault(Transform parent)
    {
        var row=new GameObject("InteractionOption",typeof(RectTransform),typeof(LayoutElement));
        row.transform.SetParent(parent,false);
        row.GetComponent<LayoutElement>().preferredHeight=36;
        var textObject=new GameObject("Label",typeof(RectTransform),typeof(Text));
        textObject.transform.SetParent(row.transform,false);
        var rect=textObject.GetComponent<RectTransform>();
        rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one;
        rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero;
        var view=row.AddComponent<InteractionOptionView>();
        view.label=textObject.GetComponent<Text>();
        view.label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        view.label.fontSize=24; view.label.raycastTarget=false;
        return view;
    }
    public void Set(ItemInteractionOption option,bool selected)
    {
        if(label!=null)
        {
            if(label.font==null) label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text=(selected ? "▶ " : "   ")+option.DisplayName;
            label.color=!option.Available ? Color.gray : selected ? new Color(1f,0.8f,0.3f) : Color.white;
        }
        if(icon!=null) { icon.sprite=option.Icon; icon.enabled=option.Icon!=null; }
        if(selectedMarker!=null) selectedMarker.SetActive(selected);
    }
}
