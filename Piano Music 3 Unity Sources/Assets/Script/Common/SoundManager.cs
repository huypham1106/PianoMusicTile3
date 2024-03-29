using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
public class SoundManager : SingletonMonoTemp<SoundManager>
{
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    public static event Action<float> OnMusicVolumeChanged;
    public static event Action<float> OnSfxVolumeChanged;
    public static event Action<bool> OnMusicStatusChanged;
    public static event Action<bool> OnSfxStatusChanged;

    [SerializeField] private SaveData _saveData;
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private AudioMixerGroup _musicMixerGroup;
    [SerializeField] private AudioMixerGroup _sfxMixerGroup;
    [SerializeField] private int _sfxAudioSourceCount = 10;
    [SerializeField] private List<SoundSO> _cachedSoundSOs = new List<SoundSO>();

    private AudioSource _musicAudioSource;
    private List<AudioSource> _sfxAudioSources = new List<AudioSource>();
    private Dictionary<string, SoundSO> _identifierSoundSODict = new Dictionary<string, SoundSO>();

    #region Properties
    public bool MusicStatus
    {
        get => _saveData.musicStatus;
        set
        {
            if (_saveData.musicStatus != value)
            {
                _saveData.musicStatus = value;
                UpdateMusicMixerVolume();
                OnMusicStatusChanged?.Invoke(value);
                DataChanged = true;
            }
        }
    }

    public bool SFXStatus
    {
        get => _saveData.sfxStatus;
        set
        {
            if (_saveData.sfxStatus != value)
            {
                _saveData.sfxStatus = value;
                UpdateSFXMixerVolume();
                OnSfxStatusChanged?.Invoke(value);
                DataChanged = true;
            }
        }
    }

    public float MusicVolume
    {
        get => _saveData.musicVolume;
        set
        {
            if (_saveData.musicVolume != value)
            {
                _saveData.musicVolume = value;
                UpdateMusicMixerVolume();
                OnMusicVolumeChanged?.Invoke(value);
                DataChanged = true;
            }
        }
    }

    public float SFXVolume
    {
        get => _saveData.sfxVolume;
        set
        {
            if (_saveData.sfxVolume != value)
            {
                _saveData.sfxVolume = value;
                UpdateSFXMixerVolume();
                OnSfxVolumeChanged?.Invoke(value);
                DataChanged = true;
            }
        }
    }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        InitAudioSources();

        foreach (var soundSO in _cachedSoundSOs)
        {
            _identifierSoundSODict.Add(soundSO.identifier, soundSO);
        }
    }

    private IEnumerator Start()
    {
        yield return null;
        UpdateMusicMixerVolume();
        UpdateSFXMixerVolume();
    }

    private void InitAudioSources()
    {
        _musicAudioSource = gameObject.AddComponent<AudioSource>();
        _musicAudioSource.playOnAwake = false;
        _musicAudioSource.outputAudioMixerGroup = _musicMixerGroup;

        for (int i = 0; i < _sfxAudioSourceCount; i++)
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.outputAudioMixerGroup = _sfxMixerGroup;
            _sfxAudioSources.Add(audioSource);
        }
    }

    private void UpdateMusicMixerVolume()
    {
        var vol = MusicStatus ? MusicVolume : 0f;
        vol = Mathf.Clamp(vol, 0.0001f, 1f);
        _audioMixer.SetFloat(MUSIC_VOLUME_KEY, Mathf.Log10(vol) * 20);
    }

    private void UpdateSFXMixerVolume()
    {
        var vol = SFXStatus ? SFXVolume : 0f;
        vol = Mathf.Clamp(vol, 0.0001f, 1f);
        _audioMixer.SetFloat(SFX_VOLUME_KEY, Mathf.Log10(vol) * 20);
    }

    public void Cache(List<SoundSO> soundSOs)
    {
        foreach (var soundSO in soundSOs)
        {
            if (_identifierSoundSODict.ContainsKey(soundSO.identifier))
            {
                //Logger.LogError($"Already cached identifier [{soundSO.identifier}]", this);
            }
            else
            {
                _identifierSoundSODict.Add(soundSO.identifier, soundSO);
                _cachedSoundSOs.Add(soundSO);
            }
        }
    }

    public void ClearCache(List<SoundSO> soundSOs)
    {
        foreach (var soundSO in soundSOs)
        {
            if (_identifierSoundSODict.ContainsKey(soundSO.identifier))
            {
                _identifierSoundSODict.Remove(soundSO.identifier);
                _cachedSoundSOs.Remove(soundSO);
            }
            else
            {
                //Logger.LogError($"Cannot find identifier [{soundSO.identifier}]", this);
            }
        }
    }

    public void ClearStoppedSound()
    {
        if (!_musicAudioSource.isPlaying)
            _musicAudioSource.clip = null;

        foreach (var source in _sfxAudioSources)
        {
            if (!source.isPlaying)
                source.clip = null;
        }
    }

    public SoundSO GetSoundSO(string name)
    {
        if (_identifierSoundSODict.ContainsKey(name))
            return _identifierSoundSODict[name];

        //Logger.LogError($"Cannot get SoundSO with name:{name}");
        return null;
    }

    public void PlayMusic(string identifier, bool loop = true, float volumeScale = 1f, float pitchScale = 1f,
              bool ignoreListnerPause = false, bool ignoreLisnerVolume = false, float fadeTime = 0f)
    {
        if (_identifierSoundSODict.TryGetValue(identifier, out var soundSO))
            PlayMusic(soundSO, loop, volumeScale, pitchScale, ignoreListnerPause, ignoreLisnerVolume, fadeTime);
        //else
            //Logger.LogError($"Cannot find soundSO with identifier [{identifier}]", this);
    }

    public void PlayMusic(SoundSO soundSO, bool loop = true, float volumeScale = 1f, float pitchScale = 1f,
        bool ignoreListnerPause = false, bool ignoreLisnerVolume = false, float fadeTime = 0f)
    {
        _musicAudioSource.clip = soundSO.clip;
        _musicAudioSource.loop = loop;
        _musicAudioSource.pitch = soundSO.pitch * pitchScale;
        _musicAudioSource.ignoreListenerPause = ignoreListnerPause;
        _musicAudioSource.ignoreListenerVolume = ignoreLisnerVolume;

        if (fadeTime <= 0f)
        {
            _musicAudioSource.volume = soundSO.volume * volumeScale;
        }
        else
        {
            _musicAudioSource.volume = 0f;
            var targetVol = soundSO.volume * volumeScale;
            DOVirtual.Float(0f, targetVol, fadeTime, value =>
            {
                _musicAudioSource.volume = value;
            }).SetEase(Ease.Linear);
        }
        _musicAudioSource.Play();
    }

    public void StopMusic(float fadeTime = 0f)
    {
        if (fadeTime <= 0f)
        {
            _musicAudioSource.Stop();
        }
        else
        {
            DOVirtual.Float(_musicAudioSource.volume, 0f, fadeTime, value =>
            {
                _musicAudioSource.volume = value;
            }).SetEase(Ease.Linear).OnComplete(() => _musicAudioSource.Stop());
        }
    }

    public AudioSource PlaySFX(string identifier, bool loop = false, float volumeScale = 1f, float pitchScale = 1f,
        bool ignoreListnerPause = false, bool ignoreLisnerVolume = false, float fadeTime = 0f)
    {
        if (_identifierSoundSODict.TryGetValue(identifier, out var soundSO))
        {
            return PlaySFX(soundSO, loop, volumeScale, pitchScale, ignoreListnerPause, ignoreLisnerVolume, fadeTime);
        }
        else
        {
            //Logger.LogError($"Cannot find soundSO with identifier [{identifier}]", this);
            return null;
        }
    }

    public AudioSource PlaySFX(SoundSO soundSO, bool loop = false, float volumeScale = 1f, float pitchScale = 1f,
        bool ignoreListnerPause = false, bool ignoreLisnerVolume = false, float fadeTime = 0f)
    {
        if (TryGetAudioSource(out var audioSource))
        {
            audioSource.clip = soundSO.clip;
            audioSource.loop = loop;
            audioSource.pitch = soundSO.pitch * pitchScale;
            audioSource.ignoreListenerPause = ignoreListnerPause;
            audioSource.ignoreListenerVolume = ignoreLisnerVolume;

            if (fadeTime <= 0f)
            {
                audioSource.volume = soundSO.volume * volumeScale;
            }
            else
            {
                audioSource.volume = 0f;
                var targetVol = soundSO.volume * volumeScale;
                DOVirtual.Float(0f, targetVol, fadeTime, value =>
                {
                    audioSource.volume = value;
                }).SetEase(Ease.Linear);
            }
            audioSource.Play();
        }
        else
        {
            //Logger.Log("Cannot get audioSource", this);
        }
        return audioSource;
    }

    public void PlaySFX(AudioClip clip, AudioMixerGroup output,
        float volume = 1f, float pitch = 1f, bool ignoreListnerPause = false, bool ignoreLisnerVolume = false)
    {
        if (TryGetAudioSource(out var audioSource))
        {
            audioSource.clip = clip;
            audioSource.outputAudioMixerGroup = output;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.ignoreListenerPause = ignoreListnerPause;
            audioSource.ignoreListenerVolume = ignoreLisnerVolume;
            audioSource.Play();
        }
        else
        {
            //Logger.LogError("Cannot get audioSource", this);
        }
    }

    private bool TryGetAudioSource(out AudioSource targetSource)
    {
        targetSource = null;
        foreach (var source in _sfxAudioSources)
        {
            if (!source.isPlaying)
            {
                targetSource = source;
                break;
            }
        }
        return targetSource != null;
    }

    #region ISaveable
    [System.Serializable]
    public class SaveData
    {
        public bool musicStatus = true;
        public bool sfxStatus = true;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
    }

    public string SaveKey => "SoundManager";

    public bool DataChanged { get; set; }

    public object GetData() => _saveData;

    public void SetData(string data)
    {
        if (string.IsNullOrEmpty(data))
            _saveData = new SaveData();
        else
            _saveData = JsonUtility.FromJson<SaveData>(data);

        UpdateMusicMixerVolume();
        UpdateSFXMixerVolume();
    }

    public void OnAllDataLoaded() { }
    #endregion

}
