using System;
using System.Collections;
using System.Collections.Generic;

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class NotificationEventInvoker : MonoBehaviour
{
    [Serializable]
    public class DelayedEvent
    {
        [LabelText("延时"), SuffixLabel("秒", overlay: true), MinValue(0f)]
        public float Delay;

        [PropertySpace(4)]
        public UnityEvent Callback;
    }

    [TitleGroup("Event")]
    [SerializeField, Required] private ScriptableEventNotification _event;

    [TitleGroup("Data")]
    [SerializeField] private NotificationData _data;

    [TitleGroup("Callbacks")]
    [SerializeField, ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
    [LabelText("事件列表")]
    private List<DelayedEvent> _events = new();

    // 通过 UnityEvent 静态绑定调用，发送 Inspector 中配置好的通知数据
    [TitleGroup("Event"), Button("Raise", ButtonSizes.Medium)]
    public void Raise()
    {
        _event.Raise(_data);
        InvokeAll();
    }

    // 通过 UnityEvent 动态字符串绑定调用，使用传入文本覆盖 Message，其余时长沿用 Inspector 配置
    public void RaiseWithMessage(string message)
    {
        var data = _data;
        data.Message = message;
        _event.Raise(data);
        InvokeAll();
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
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }
}
