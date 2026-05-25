using System;
using UnityEngine;

[Serializable]
public struct NotificationAudioConfig
{
    public AudioClip Clip;
    [Range(0f, 1f)] public float Volume;
    [Range(-3f, 3f)] public float Pitch;
    [Range(0f, 1f)] public float SpatialBlend;
    [Min(0f)] public float MinDistance;
    [Min(0f)] public float MaxDistance;
    public bool Loop;
    [Min(0f)] public float Delay;
}

[Serializable]
public struct NotificationData
{
    public string Message;
    public float DisplayDuration;
    public float FadeOutDuration;
    public NotificationAudioConfig Audio;
}
