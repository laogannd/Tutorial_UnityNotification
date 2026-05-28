using System;
using System.Collections;
using System.Collections.Generic;

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

using Obvious.Soap;

public class TimedEventSender : MonoBehaviour
{
    [Serializable]
    public class DelayedEvent
    {
        [LabelText("延时"), SuffixLabel("秒", overlay: true), MinValue(0f)]
        public float Delay;

        [PropertySpace(4)]
        public UnityEvent Callback;
    }

    [Serializable]
    private struct TimedNotification
    {
        public float Delay;
        public NotificationData Data;
    }

    [TitleGroup("Event")]
    [SerializeField] private ScriptableEventNotification _event;

    [TitleGroup("Messages")]
    [SerializeField] private TimedNotification[] _messages;
    [SerializeField] private bool _playOnStart = true;

    [TitleGroup("Callbacks")]
    [SerializeField, ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
    [LabelText("事件列表")]
    private List<DelayedEvent> _events = new();

    private bool _isPaused;
    private bool _isRunning;
    private float _totalDuration;
    private float _elapsed;

    public bool IsPlaying => _isRunning && !_isPaused;
    public bool IsPaused => _isRunning && _isPaused;
    public float RemainingTime => _isRunning ? Mathf.Max(0f, _totalDuration - _elapsed) : 0f;

    private void Start()
    {
        if (_playOnStart) Play();
    }

    // 开始播放，如果正在运行会重新开始
    public void Play()
    {
        StopAllCoroutines();
        _isPaused = false;
        _isRunning = true;
        _elapsed = 0f;
        _totalDuration = 0f;
        foreach (var item in _messages)
            _totalDuration += item.Delay;
        StartCoroutine(SendRoutine());
    }

    // 暂停计时器
    public void Pause()
    {
        if (!_isRunning) return;
        _isPaused = true;
    }

    // 恢复计时器
    public void Resume()
    {
        if (!_isRunning) return;
        _isPaused = false;
    }

    // 停止计时器，invokeCallbacks 控制是否触发剩余回调
    public void Stop(bool invokeCallbacks = true)
    {
        if (!_isRunning) return;
        StopAllCoroutines();
        _isRunning = false;
        _isPaused = false;
        if (invokeCallbacks) InvokeAll();
    }

    // 重置并重新开始，invokeCallbacks 控制是否触发当前轮次的回调
    public void Reset(bool invokeCallbacks = true)
    {
        if (_isRunning)
        {
            StopAllCoroutines();
            if (invokeCallbacks) InvokeAll();
        }
        _isPaused = false;
        _isRunning = true;
        _elapsed = 0f;
        _totalDuration = 0f;
        foreach (var item in _messages)
            _totalDuration += item.Delay;
        StartCoroutine(SendRoutine());
    }

    // 切换暂停/恢复
    public void TogglePause()
    {
        if (_isPaused) Resume();
        else Pause();
    }

    private IEnumerator SendRoutine()
    {
        foreach (var item in _messages)
        {
            yield return WaitPausable(item.Delay);
            _event.Raise(item.Data);
        }
        InvokeAll();
        _isRunning = false;
    }

    private IEnumerator WaitPausable(float duration)
    {
        float waited = 0f;
        while (waited < duration)
        {
            if (_isPaused)
            {
                yield return null;
                continue;
            }
            float dt = Time.deltaTime;
            waited += dt;
            _elapsed += dt;
            yield return null;
        }
    }

    private void InvokeAll()
    {
        for (int i = 0; i < _events.Count; i++)
        {
            var entry = _events[i];
            if (entry.Delay > 0f)
            {
                StartCoroutine(DelayedInvoke(entry.Delay, entry.Callback));
                continue;
            }
            entry.Callback?.Invoke();
        }
    }

    private IEnumerator DelayedInvoke(float delay, UnityEvent callback)
    {
        yield return WaitPausable(delay);
        callback?.Invoke();
    }
}
