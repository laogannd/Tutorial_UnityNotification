# Notification 模块学习文档

> 版本：ssx (260518.0) | 适用项目：UnrealToUnitymainworld

---

## 一、模块概览

Notification 模块负责在 VR 场景中向玩家展示文字通知，例如"回答正确"、"任务开始"等提示信息。

**核心设计思路**：基于 SOAP（ScriptableObject Architecture Pattern）框架，用 ScriptableObject 作为事件总线，将"发送通知"与"显示通知"完全解耦。任何系统只需持有同一个 ScriptableObject 资产引用，即可在不直接引用对方的情况下通信。

---

## 二、架构一览

```
[发送方]                         [接收方]
NotificationEventInvoker         VRNotificationPanel
SoapNotificationFeedback    -->  (ScriptableEventNotification)  -->  UI 淡入淡出显示
任何脚本 .Raise(data)
```

**数据流**：

```
NotificationData (struct)
    |
    v
ScriptableEventNotification.Raise(data)
    |
    v (触发 OnRaised 事件)
VRNotificationPanel.ShowNotification(data)
    |
    v
Coroutine: 淡入 → 停留 → 淡出
```

---

## 三、核心文件说明

### 3.1 NotificationData.cs — 数据容器

```csharp
[Serializable]
public struct NotificationData
{
    public string Message;        // 通知文字内容
    public float DisplayDuration; // 文字停留时长（秒）
    public float FadeOutDuration; // 淡入/淡出时长（秒）
}
```

**要点**：
- 使用 `struct` 而非 `class`，避免堆分配（GC 友好）
- 标记 `[Serializable]` 使其可在 Inspector 中直接配置
- `FadeOutDuration` 同时控制淡入和淡出，保持对称动画

---

### 3.2 ScriptableEventNotification.cs — 事件总线

```csharp
[CreateAssetMenu(menuName = "Soap/ScriptableEvents/Notification")]
public class ScriptableEventNotification : ScriptableEvent<NotificationData>
{
}
```

**要点**：
- 继承 SOAP 框架的 `ScriptableEvent<T>`，泛型参数为 `NotificationData`
- 本身是一个 ScriptableObject 资产（`.asset` 文件），在 Assets 中创建一次，全场景共享
- `[CreateAssetMenu]` 让你在 Project 窗口右键菜单中创建该资产
- 核心 API：`Raise(NotificationData)` 触发事件，`OnRaised` 是事件委托

---

### 3.3 NotificationEventInvoker.cs — Inspector 触发器

```csharp
public class NotificationEventInvoker : MonoBehaviour
{
    [SerializeField] private ScriptableEventNotification _event;
    [SerializeField] private NotificationData _data;

    public void Raise() => _event.Raise(_data);

    public void RaiseWithMessage(string message)
    {
        var data = _data;
        data.Message = message;
        _event.Raise(data);
    }
}
```

**要点**：
- `Raise()`：直接发送 Inspector 中预配置好的通知，适合 UnityEvent 静态绑定（如按钮点击）
- `RaiseWithMessage(string)`：动态覆盖消息文字，时长沿用 Inspector 配置，适合 UnityEvent 动态字符串绑定
- `var data = _data` 是值拷贝（struct），修改 `data.Message` 不会污染原始 `_data`

---

### 3.4 VRNotificationPanel.cs — UI 显示面板

这是模块的"接收端"，负责监听事件并驱动 UI 动画。

**关键机制 1：事件订阅/取消订阅**

```csharp
private void OnEnable()  => _notificationEvent.OnRaised += ShowNotification;
private void OnDisable() => _notificationEvent.OnRaised -= ShowNotification;
```

在 `OnEnable`/`OnDisable` 中管理订阅，确保面板隐藏时不响应事件，也不会产生内存泄漏。

**关键机制 2：VR Billboard（公告板）**

```csharp
private void LateUpdate()
{
    transform.LookAt(
        transform.position + _mainCamera.transform.rotation * Vector3.forward,
        _mainCamera.transform.rotation * Vector3.up
    );
}
```

每帧让面板朝向摄像机，使文字在 VR 中始终正对玩家视线。使用 `LateUpdate` 确保在摄像机移动后再更新朝向。

**关键机制 3：打断重播**

```csharp
private void ShowNotification(NotificationData data)
{
    if (_currentCoroutine != null)
        StopCoroutine(_currentCoroutine);
    _notificationText.text = data.Message;
    _currentCoroutine = StartCoroutine(DisplayRoutine(...));
}
```

若上一条通知还在播放，立即停止并开始新通知，避免通知堆积。

**关键机制 4：淡入淡出协程**

```csharp
private IEnumerator DisplayRoutine(float displayDuration, float fadeDuration)
{
    yield return Fade(0f, 1f, fadeDuration); // 淡入
    yield return new WaitForSeconds(displayDuration); // 停留
    yield return Fade(1f, 0f, fadeDuration); // 淡出
    _currentCoroutine = null;
}

private IEnumerator Fade(float from, float to, float duration)
{
    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
        yield return null;
    }
    _canvasGroup.alpha = to; // 确保最终值精确
}
```

通过 `CanvasGroup.alpha` 控制整个面板透明度，`Mathf.Lerp` 实现线性插值动画。

---

### 3.5 SoapNotificationFeedback.cs — 答题系统适配器

这是一个**扩展示例**，展示如何将 Notification 模块接入其他系统。

```csharp
[RequireComponent(typeof(QuestionPanel))]
public class SoapNotificationFeedback : MonoBehaviour, IQuestionFeedback
{
    // ...
    public void OnAnswerSubmitted(AnswerResult result)
    {
        Raise(result.IsAllCorrect ? _correctMessage : _wrongMessage);
    }
}
```

**设计亮点**：
- 实现 `IQuestionFeedback` 接口，插入答题系统的反馈管道
- 内部调用 `_notificationEvent.Raise(data)`，复用 Notification 模块
- `_broadcastOnPresent` 开关控制题目展示时是否也发通知，灵活可配
- 与 `VRNotificationPanel` 完全解耦，两者只通过 ScriptableObject 资产通信

---

## 四、搭建步骤

### 步骤 1：创建 ScriptableObject 资产

在 Project 窗口右键 → `Create > Soap > ScriptableEvents > Notification`，命名为 `notification_event`（或任意名称）。

### 步骤 2：配置 VRNotificationPanel

1. 在场景中创建一个 Canvas（World Space 模式）
2. 添加 `CanvasGroup` 组件
3. 添加 `TextMeshProUGUI` 子物体用于显示文字
4. 挂载 `VRNotificationPanel` 脚本
5. 将 `notification_event` 资产拖入 `Notification Event` 字段
6. 将 `TextMeshProUGUI` 和 `CanvasGroup` 分别拖入对应字段

### 步骤 3：配置发送方

**方式 A — 使用 NotificationEventInvoker（Inspector 触发）**：
1. 在任意 GameObject 上挂载 `NotificationEventInvoker`
2. 将 `notification_event` 拖入 `Event` 字段
3. 在 `Data` 中填写消息文字和时长
4. 在 UnityEvent 中调用 `Raise()` 或 `RaiseWithMessage(string)`

**方式 B — 代码直接触发**：

```csharp
[SerializeField] private ScriptableEventNotification _notificationEvent;

void SendNotification()
{
    _notificationEvent.Raise(new NotificationData
    {
        Message = "任务完成！",
        DisplayDuration = 2f,
        FadeOutDuration = 0.3f
    });
}
```

---

## 五、常见问题

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| 通知不显示 | 面板未启用或事件资产不一致 | 确认发送方和接收方引用同一个 `.asset` 文件 |
| 通知不朝向玩家 | `_mainCamera` 为 null | 确保场景中有 `Camera.main`（Tag 为 MainCamera） |
| 通知被截断 | 新通知打断旧通知 | 这是预期行为；如需队列，需自行扩展 |
| 淡出后仍可见 | `CanvasGroup.alpha` 未归零 | 检查 `Fade` 协程末尾是否正确赋值 `to` |

---

## 六、扩展思路

1. **通知队列**：在 `VRNotificationPanel` 中维护 `Queue<NotificationData>`，逐条播放
2. **多种通知类型**：为 `NotificationData` 添加 `NotificationType` 枚举，控制颜色或图标
3. **音效联动**：在 `ShowNotification` 中调用 `AudioSource.PlayOneShot`
4. **位置跟随**：将面板设为玩家头部子物体，或用 `SmoothDamp` 跟随

---

## 七、关键设计原则总结

| 原则 | 体现 |
|------|------|
| 解耦 | 发送方与接收方只共享 ScriptableObject 资产，互不引用 |
| GC 友好 | `NotificationData` 为 struct，避免堆分配 |
| 生命周期安全 | OnEnable/OnDisable 管理订阅，防止内存泄漏 |
| VR 适配 | LateUpdate Billboard 确保文字始终朝向玩家 |
| 可打断 | 新通知立即替换旧通知，避免堆积 |

---

> 项目版本标识：<260518.0>
