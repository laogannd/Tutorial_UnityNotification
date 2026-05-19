using UnityEngine;

public class NotificationEventInvoker : MonoBehaviour
{
    [SerializeField] private ScriptableEventNotification _event;
    [SerializeField] private NotificationData _data;

    // 通过 UnityEvent 静态绑定调用，发送 Inspector 中配置好的通知数据
    public void Raise() => _event.Raise(_data);

    // 通过 UnityEvent 动态字符串绑定调用，使用传入文本覆盖 Message，其余时长沿用 Inspector 配置
    public void RaiseWithMessage(string message)
    {
        var data = _data;
        data.Message = message;
        _event.Raise(data);
    }
}
