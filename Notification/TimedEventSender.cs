using System.Collections;
using UnityEngine;

using Obvious.Soap;

public class TimedEventSender : MonoBehaviour
{
    [System.Serializable]
    private struct TimedNotification
    {
        public float Delay;
        public NotificationData Data;
    }

    [SerializeField] private ScriptableEventNotification _event;
    [SerializeField] private TimedNotification[] _messages;
    [SerializeField] private bool _playOnStart = true;

    private void Start()
    {
        if (_playOnStart) Play();
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(SendRoutine());
    }

    private IEnumerator SendRoutine()
    {
        foreach (var item in _messages)
        {
            yield return new WaitForSeconds(item.Delay);
            _event.Raise(item.Data);
        }
    }
}
