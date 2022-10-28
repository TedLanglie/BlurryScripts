using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance { get; private set; }

    private AudioSource _source;

    void Awake()
    {
        instance = this;

        _source = GetComponent<AudioSource>();
    }

    public void PlaySound(AudioClip sound)
    {
        _source.PlayOneShot(sound);
        //SoundManager.instance.PlaySound(sound); // this line will play any sound from any script. nice
    }
}
