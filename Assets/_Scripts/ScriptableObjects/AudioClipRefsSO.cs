using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioClipRefsSO", menuName = "ScriptableObjects/AudioClipRefsSO", order = 1)]
public class AudioClipRefsSO : ScriptableObject
{
    public AudioClip[] cutting;
    public AudioClip[] deliveryFail;
    public AudioClip[] deliverySuccess;
    public AudioClip[] footstep;
    public AudioClip[] objectDrop;
    public AudioClip[] objectPickup;
    public AudioClip stoveSizzle;
    public AudioClip[] trash;
    public AudioClip[] warning;
}
