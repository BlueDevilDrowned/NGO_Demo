using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ActorViewVisibilityController : MonoBehaviour
{
    [SerializeField]private Renderer[] firstPersonHiddenRenderers;
    [SerializeField]private Renderer[] thirdPersonHiddenRenderers;
    [SerializeField]private string hiddenLayerName="LocalFirstPersonHidden";

    private readonly Dictionary<GameObject,int>originalLayers=new();
    private Renderer[] dynamicFirstPersonHiddenRenderers;
    private Renderer[] dynamicThirdPersonHiddenRenderers;
    private int hiddenLayer=-1;
    private bool hasAppliedPerspectiveMode;

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

    public bool SetDynamicFirstPersonHiddenRoot(Transform root)
    {
        return SetDynamicHiddenRoot(
            ref dynamicFirstPersonHiddenRenderers,
            root,
            CameraPerspectiveMode.FirstPerson);
    }

    public bool SetDynamicThirdPersonHiddenRoot(Transform root)
    {
        return SetDynamicHiddenRoot(
            ref dynamicThirdPersonHiddenRenderers,
            root,
            CameraPerspectiveMode.ThirdPerson);
    }

    public bool SetPerspectiveMode(CameraPerspectiveMode mode)
    {
        if(!ActorPerspectiveSnapshotUtility.IsValid(mode))
            return false;
        if(hasAppliedPerspectiveMode&&mode==PerspectiveMode)
            return true;

        RestoreOriginalLayers();
        if(!HideRenderers(GetHiddenRenderers(mode)))
            return false;
        if(!HideRenderers(GetDynamicHiddenRenderers(mode)))
            return false;

        PerspectiveMode=mode;
        hasAppliedPerspectiveMode=true;
        return true;
    }

    private Renderer[] GetHiddenRenderers(CameraPerspectiveMode mode)
    {
        return mode==CameraPerspectiveMode.FirstPerson
            ?firstPersonHiddenRenderers
            :thirdPersonHiddenRenderers;
    }

    private Renderer[] GetDynamicHiddenRenderers(CameraPerspectiveMode mode)
    {
        return mode==CameraPerspectiveMode.FirstPerson
            ?dynamicFirstPersonHiddenRenderers
            :dynamicThirdPersonHiddenRenderers;
    }

    private bool SetDynamicHiddenRoot(
        ref Renderer[] renderers,
        Transform root,
        CameraPerspectiveMode hiddenMode)
    {
        RestoreOriginalLayers(renderers);
        renderers=root!=null
            ?root.GetComponentsInChildren<Renderer>(true)
            :null;

        return PerspectiveMode!=hiddenMode||HideRenderers(renderers);
    }

    private bool HideRenderers(Renderer[] renderers)
    {
        if(hiddenLayer<0)return false;

        if(renderers!=null)
        {
            for(int i=0;i<renderers.Length;i++)
            {
                Renderer target=renderers[i];
                if(target==null)continue;

                GameObject targetObject=target.gameObject;
                if(originalLayers.ContainsKey(targetObject))continue;

                originalLayers.Add(targetObject,targetObject.layer);
                targetObject.layer=hiddenLayer;
            }
        }

        return true;
    }

    private void RestoreOriginalLayers()
    {
        foreach(KeyValuePair<GameObject,int>entry in originalLayers)
        {
            if(entry.Key!=null)
                entry.Key.layer=entry.Value;
        }

        originalLayers.Clear();
    }

    private void RestoreOriginalLayers(Renderer[] renderers)
    {
        if(renderers==null)return;

        for(int i=0;i<renderers.Length;i++)
        {
            Renderer renderer=renderers[i];
            if(renderer==null)continue;

            GameObject targetObject=renderer.gameObject;
            if(!originalLayers.TryGetValue(targetObject,out int layer))
                continue;

            targetObject.layer=layer;
            originalLayers.Remove(targetObject);
        }
    }

    private void OnDisable()
    {
        if(originalLayers.Count>0)
            RestoreOriginalLayers();

        PerspectiveMode=CameraPerspectiveMode.ThirdPerson;
        hasAppliedPerspectiveMode=false;
    }
}
