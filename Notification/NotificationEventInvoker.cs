using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class NotificationEventInvoker : MonoBehaviour
{
    [TitleGroup("Event")]
    [SerializeField, Required] private ScriptableEventNotification _event;

    [TitleGroup("Data")]
    [SerializeField] private NotificationData _data;

    [TitleGroup("Callbacks")]
    [SerializeField, FoldoutGroup("Callbacks/On Raise")]
    private UnityEvent _onRaise;

    [SerializeField, FoldoutGroup("Callbacks/On Raise")]
    private UnityEvent<NotificationData> _onRaiseWithData;

    // 通过 UnityEvent 静态绑定调用，发送 Inspector 中配置好的通知数据
    [TitleGroup("Event"), Button("Raise", ButtonSizes.Medium)]
    public void Raise()
    {
        _event.Raise(_data);
        _onRaise?.Invoke();
        _onRaiseWithData?.Invoke(_data);
    }

    // 通过 UnityEvent 动态字符串绑定调用，使用传入文本覆盖 Message，其余时长沿用 Inspector 配置
    public void RaiseWithMessage(string message)
    {
        var data = _data;
        data.Message = message;
        _event.Raise(data);
        _onRaise?.Invoke();
        _onRaiseWithData?.Invoke(data);
    }
}
