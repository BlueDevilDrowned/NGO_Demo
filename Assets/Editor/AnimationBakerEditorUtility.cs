using System.Collections.Generic;
using Animancer;
using UnityEditor;
using UnityEngine;

internal static class AnimationBakerEditorUtility
{
    public static string GetProjectPreferenceKey(string windowName, string settingName)
    {
        return $"NGO.AnimationBaker.{Application.dataPath}.{windowName}.{settingName}";
    }

    public static void SaveObject(string key, Object value)
    {
        if (value == null)
        {
            EditorPrefs.DeleteKey(key);
            return;
        }

        GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(value);
        EditorPrefs.SetString(key, id.ToString());
    }

    public static T LoadObject<T>(string key) where T : Object
    {
        string serializedId = EditorPrefs.GetString(key, string.Empty);
        if (string.IsNullOrEmpty(serializedId) || !GlobalObjectId.TryParse(serializedId, out GlobalObjectId id))
            return null;

        return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as T;
    }

    public static bool TryResolveClip(Object source, out AnimationClip clip, out string error)
    {
        clip = null;
        error = string.Empty;

        if (source == null)
            return true;

        if (source is AnimationClip animationClip)
        {
            clip = animationClip;
            return true;
        }

        if (source is not TransitionAssetBase transitionAsset)
        {
            error = "Animation Source must be an AnimationClip or an Animancer TransitionAsset.";
            return false;
        }

        if (transitionAsset.GetTransition() is ClipTransition clipTransition)
        {
            clip = clipTransition.Clip;
            if (clip != null)
                return true;
        }

        var clips = new List<AnimationClip>();
        transitionAsset.GetAnimationClips(clips);
        AnimationClip singleClip = null;

        for (int i = 0; i < clips.Count; i++)
        {
            AnimationClip candidate = clips[i];
            if (candidate == null || candidate == singleClip)
                continue;

            if (singleClip != null)
            {
                error = $"{transitionAsset.name} contains multiple AnimationClips. " +
                        "A single-clip baker cannot choose one automatically.";
                return false;
            }

            singleClip = candidate;
        }

        if (singleClip == null)
        {
            error = $"{transitionAsset.name} does not contain an AnimationClip.";
            return false;
        }

        clip = singleClip;
        return true;
    }
}
