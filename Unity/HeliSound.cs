using System.Collections;
using System.Collections.Generic;
#if EFT
using Comfort.Common;
using EFT;
#endif
using UnityEngine;
using UnityEngine.Audio;

public class HeliSound : MonoBehaviour
{
    public AudioSource closeEngine;
    public AudioSource distantEngine;
    public AudioSource closeRotors;
    public AudioSource distantRotors;
    public AnimationCurve distanceCurve;

    #if EFT
    private GameWorld _gameWorld;
    #endif

    // Start is called before the first frame update
    void Awake()
    {
        #if EFT
        _gameWorld = Singleton<GameWorld>.Instance;
        
        
        AudioMixerGroup outputAudioMixerGroup = Singleton<BetterAudio>.Instance.EnvTechnicalSoundsGroup;
        closeEngine.outputAudioMixerGroup = outputAudioMixerGroup;
        distantEngine.outputAudioMixerGroup = outputAudioMixerGroup;
        closeRotors.outputAudioMixerGroup = outputAudioMixerGroup;
        distantRotors.outputAudioMixerGroup = outputAudioMixerGroup;
        #endif
    }

    // Update is called once per frame
    void Update()
    {
        CrossFade();
    }

    void CrossFade()
    {
        #if EFT
        if (!_gameWorld.MainPlayer.HealthController.IsAlive)
            return;
        
        float distance = Vector3.Distance(_gameWorld.MainPlayer.Position, transform.position);
        float volume = distanceCurve.Evaluate(distance);
        float fadeVolume = distanceCurve.Evaluate(distance / 10);
        float fadeClamped = Mathf.Clamp01(1 - fadeVolume);

        closeEngine.volume = Mathf.Clamp01(1 - volume);
        distantEngine.volume = Mathf.Min(Mathf.Clamp01(volume), fadeClamped);
    
        closeRotors.volume = Mathf.Clamp01(1 - volume);
        distantRotors.volume = Mathf.Min(Mathf.Clamp01(volume), fadeClamped);
        #endif
    }
}
