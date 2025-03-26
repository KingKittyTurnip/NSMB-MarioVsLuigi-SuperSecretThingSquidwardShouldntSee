using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PezoliIntroEventManager : MonoBehaviour
{
    public UnityEvent startMoviePlayback;
    public UnityEvent pezolisIntro;
    public UnityEvent releasePezAndPlayer;

    public void StartMoviePlayback()
    {
        if (startMoviePlayback != null)
        {
            startMoviePlayback.Invoke();
        }
        GameManager.Instance.paralizePlayer = true;
    }
    public void PezolisIntro()
    {
        if (pezolisIntro != null)
        {
            pezolisIntro.Invoke();
        }
        GameManager.Instance.paralizePlayer = true;
    }
    public void ReleasePezAndPlayer()
    {
        if (releasePezAndPlayer != null)
        {
            releasePezAndPlayer.Invoke();
        }
        Destroy(gameObject);
    }
}
