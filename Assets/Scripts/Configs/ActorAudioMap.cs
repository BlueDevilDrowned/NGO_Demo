using System;
using System.Collections.Generic;
using UnityEngine;

public enum AudioClipSelectionMode : byte
{
    Single,
    Random,
    Variant,
}

[Serializable]
public struct ActorAudioVariant
{
    public string Key;
    public AudioClip Clip;
}

[Serializable]
public sealed class ActorAudioEntry
{
    public string Key;
    public AudioClipSelectionMode SelectionMode;
    [Range(0f,1f)]public float Volume=1f;
    public AudioClip Clip;
    public List<AudioClip> RandomClips=new();
    public List<ActorAudioVariant> Variants=new();

    public AudioClip Resolve(string variant=null)
    {
        return SelectionMode switch
        {
            AudioClipSelectionMode.Random=>ResolveRandom(),
            AudioClipSelectionMode.Variant=>ResolveVariant(variant),
            _=>Clip,
        };
    }

    private AudioClip ResolveRandom()
    {
        if(RandomClips==null||RandomClips.Count==0)return Clip;

        int start=UnityEngine.Random.Range(0,RandomClips.Count);
        for(int offset=0;offset<RandomClips.Count;offset++)
        {
            AudioClip candidate=RandomClips[(start+offset)%RandomClips.Count];
            if(candidate!=null)return candidate;
        }
        return Clip;
    }

    private AudioClip ResolveVariant(string variant)
    {
        if(Variants!=null&&!string.IsNullOrEmpty(variant))
        {
            for(int i=0;i<Variants.Count;i++)
            {
                ActorAudioVariant candidate=Variants[i];
                if(candidate.Clip!=null&&string.Equals(
                       candidate.Key,
                       variant,
                       StringComparison.OrdinalIgnoreCase))
                    return candidate.Clip;
            }
        }

        if(Clip!=null)return Clip;
        if(Variants==null)return null;

        for(int i=0;i<Variants.Count;i++)
        {
            if(Variants[i].Clip!=null)return Variants[i].Clip;
        }
        return null;
    }
}

[CreateAssetMenu(
    fileName="ActorAudioMap",
    menuName="Scriptable Objects/Actor Audio Map")]
public sealed class ActorAudioMap : ScriptableObject
{
    [SerializeField]private List<ActorAudioEntry> entries=new();

    public bool TryResolve(
        string key,
        string variant,
        out AudioClip clip,
        out float volume)
    {
        clip=null;
        volume=1f;
        if(entries==null||string.IsNullOrWhiteSpace(key))return false;

        for(int i=0;i<entries.Count;i++)
        {
            ActorAudioEntry entry=entries[i];
            if(entry==null||!string.Equals(
                   entry.Key,
                   key,
                   StringComparison.Ordinal))continue;

            clip=entry.Resolve(variant);
            volume=Mathf.Clamp01(entry.Volume);
            return clip!=null;
        }
        return false;
    }
}
