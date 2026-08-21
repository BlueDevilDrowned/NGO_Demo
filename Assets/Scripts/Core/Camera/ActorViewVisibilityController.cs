using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ActorViewVisibilityController : MonoBehaviour
{
    [SerializeField]private Renderer[] firstPersonHiddenRenderers;
    [SerializeField]private string hiddenLayerName="LocalFirstPersonHidden";

    private readonly Dictionary<Renderer,int>originalLayers=new();
    private int hiddenLayer=-1;

    public bool IsFirstPersonHidden{get;private set;}

    private void Awake()
    {
        hiddenLayer=LayerMask.NameToLayer(hiddenLayerName);
        if(hiddenLayer<0)
        {
            Debug.LogError(
                $"Layer is not configured: {hiddenLayerName}",
                this);
        }
    }

    public bool SetFirstPersonHidden(bool hidden)
    {
        if(hidden==IsFirstPersonHidden)return true;

        if(hidden)
            return HideFirstPersonRenderers();

        RestoreOriginalLayers();
        return true;
    }

    private bool HideFirstPersonRenderers()
    {
        if(hiddenLayer<0)return false;

        originalLayers.Clear();
        if(firstPersonHiddenRenderers!=null)
        {
            for(int i=0;i<firstPersonHiddenRenderers.Length;i++)
            {
                Renderer target=firstPersonHiddenRenderers[i];
                if(target==null||originalLayers.ContainsKey(target))continue;

                originalLayers.Add(target,target.gameObject.layer);
                target.gameObject.layer=hiddenLayer;
            }
        }

        IsFirstPersonHidden=true;
        return true;
    }

    private void RestoreOriginalLayers()
    {
        foreach(KeyValuePair<Renderer,int>entry in originalLayers)
        {
            if(entry.Key!=null)
                entry.Key.gameObject.layer=entry.Value;
        }

        originalLayers.Clear();
        IsFirstPersonHidden=false;
    }

    private void OnDisable()
    {
        if(IsFirstPersonHidden)
            RestoreOriginalLayers();
    }
}
