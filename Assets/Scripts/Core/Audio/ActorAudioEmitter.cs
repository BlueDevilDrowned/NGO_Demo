using UnityEngine;

[DisallowMultipleComponent]
public sealed class ActorAudioEmitter : MonoBehaviour
{
    [SerializeField]private AudioSource oneShotPlayer;
    [SerializeField]private AudioSource loopPlayer;

    public bool IsConfigured=>oneShotPlayer!=null&&loopPlayer!=null;
    public bool IsLoopPlaying=>loopPlayer!=null&&
                               loopPlayer.isPlaying&&
                               loopPlayer.clip!=null;

    private void Reset()
    {
        EnsurePlayers();
    }

    private void Awake()
    {
        EnsurePlayers();
    }

    private void EnsurePlayers()
    {
        AudioSource[] players=GetComponents<AudioSource>();
        if(oneShotPlayer==null)
            oneShotPlayer=players.Length>0
                ?players[0]
                :gameObject.AddComponent<AudioSource>();
        if(loopPlayer==null||loopPlayer==oneShotPlayer)
        {
            loopPlayer=null;
            for(int i=0;i<players.Length;i++)
            {
                if(players[i]!=oneShotPlayer)
                {
                    loopPlayer=players[i];
                    break;
                }
            }
            if(loopPlayer==null)
                loopPlayer=gameObject.AddComponent<AudioSource>();
        }

        Configure(oneShotPlayer,false);
        Configure(loopPlayer,true);
    }

    public bool PlayOneShot(AudioClip clip,float volume=1f)
    {
        if(clip==null||oneShotPlayer==null)return false;

        oneShotPlayer.PlayOneShot(clip,Mathf.Clamp01(volume));
        return true;
    }

    public bool PlayLoop(AudioClip clip,float volume=1f)
    {
        if(clip==null||loopPlayer==null)return false;

        loopPlayer.loop=true;
        loopPlayer.volume=Mathf.Clamp01(volume);
        if(loopPlayer.clip==clip&&loopPlayer.isPlaying)return true;

        loopPlayer.clip=clip;
        loopPlayer.Play();
        return true;
    }

    public void StopLoop()
    {
        if(loopPlayer==null)return;

        loopPlayer.Stop();
        loopPlayer.clip=null;
    }

    private void OnDisable()
    {
        StopLoop();
    }

    private static void Configure(AudioSource player,bool loop)
    {
        player.playOnAwake=false;
        player.loop=loop;
        player.spatialBlend=1f;
    }
}
