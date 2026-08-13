using UnityEngine;

public sealed class ActorAudioSystem
{
    private readonly ActorAudioMap audioMap;
    private readonly ActorAudioEmitter emitter;
    private string currentLoopKey;

    public string CurrentLoopKey=>emitter!=null&&emitter.IsLoopPlaying
        ?currentLoopKey
        :null;

    public ActorAudioSystem(
        ActorAudioMap audioMap,
        ActorAudioEmitter emitter)
    {
        this.audioMap=audioMap;
        this.emitter=emitter;
    }
    public bool PlayOneShot(AudioClip audio,float volume=1)
    {
        if(audio==null)return false;
        return emitter.PlayOneShot(audio,volume);
    }
    public bool PlayOneShot(string key,string variant=null)
    {
        if(audioMap==null||emitter==null)return false;
        if(!audioMap.TryResolve(key,variant,out var clip,out float volume))
            return false;

        return emitter.PlayOneShot(clip,volume);
    }

    public bool PlayLoop(string key,string variant=null)
    {
        if(audioMap==null||emitter==null)return false;
        if(!audioMap.TryResolve(key,variant,out var clip,out float volume))
            return false;

        if(!emitter.PlayLoop(clip,volume))return false;

        currentLoopKey=key;
        return true;
    }

    public bool IsLoopPlaying(string key)
    {
        return !string.IsNullOrWhiteSpace(key)&&
               emitter!=null&&
               emitter.IsLoopPlaying&&
               currentLoopKey==key;
    }

    public void StopLoop()
    {
        emitter?.StopLoop();
        currentLoopKey=null;
    }
}
