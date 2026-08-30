using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ActorViewVisibilityController : MonoBehaviour
{
    [SerializeField]private Renderer[] firstPersonHiddenRenderers;
    [SerializeField]private Renderer[] thirdPersonHiddenRenderers;
    [SerializeField]private string hiddenLayerName="LocalFirstPersonHidden";

    private readonly Dictionary<Renderer,int>originalLayers=new();
    private int hiddenLayer=-1;

    public CameraPerspectiveMode PerspectiveMode{get;private set;}
    public bool IsFirstPersonHidden=>
        PerspectiveMode==CameraPerspectiveMode.FirstPerson;

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
        return SetPerspectiveMode(
            hidden
                ?CameraPerspectiveMode.FirstPerson
                :CameraPerspectiveMode.ThirdPerson);
    }

    public bool SetPerspectiveMode(CameraPerspectiveMode mode)
    {
        if(!ActorPerspectiveSnapshotUtility.IsValid(mode))
            return false;
        if(mode==PerspectiveMode&&
           (originalLayers.Count>0||
            !HasConfiguredHiddenRenderers(mode)))
            return true;

        RestoreOriginalLayers();
        if(!HideRenderers(GetHiddenRenderers(mode)))
            return false;

        PerspectiveMode=mode;
        return true;
    }

    private Renderer[] GetHiddenRenderers(CameraPerspectiveMode mode)
    {
        return mode==CameraPerspectiveMode.FirstPerson
            ?firstPersonHiddenRenderers
            :thirdPersonHiddenRenderers;
    }

    private bool HasConfiguredHiddenRenderers(CameraPerspectiveMode mode)
    {
        Renderer[] renderers=GetHiddenRenderers(mode);
        return renderers!=null&&renderers.Length>0;
    }

    private bool HideRenderers(Renderer[] renderers)
    {
        if(hiddenLayer<0)return false;

        if(renderers!=null)
        {
            for(int i=0;i<renderers.Length;i++)
            {
                Renderer target=renderers[i];
                if(target==null||originalLayers.ContainsKey(target))continue;

                originalLayers.Add(target,target.gameObject.layer);
                target.gameObject.layer=hiddenLayer;
            }
        }

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
    }

    private void OnDisable()
    {
        if(originalLayers.Count>0)
            RestoreOriginalLayers();

        PerspectiveMode=CameraPerspectiveMode.ThirdPerson;
    }
}
