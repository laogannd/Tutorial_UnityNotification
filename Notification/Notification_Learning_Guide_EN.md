# Notification Module — Learning Guide

> Version: ssx (260518.0) | Project: UnrealToUnitymainworld

---

## 1. Module Overview

The Notification module displays text pop-ups to the player inside a VR scene — things like "Correct Answer", "Mission Started", etc.

**Core design idea**: Built on the SOAP (ScriptableObject Architecture Pattern) framework. A ScriptableObject asset acts as a shared event bus, completely decoupling the sender from the receiver. Any system that holds a reference to the same asset can communicate without directly referencing each other.

---

## 2. Architecture at a Glance

```
[Sender]                          [Receiver]
NotificationEventInvoker          VRNotificationPanel
SoapNotificationFeedback    -->  (ScriptableEventNotification)  -->  Fade-in/out UI
Any script calling .Raise(data)
```

**Data flow**:

```
NotificationData (struct)
    |
    v
ScriptableEventNotification.Raise(data)
    |
    v  (fires OnRaised delegate)
VRNotificationPanel.ShowNotification(data)
    |
    v
Coroutine: Fade In -> Hold -> Fade Out
```

---

## 3. Core Files Explained

### 3.1 NotificationData.cs — Data Container

```csharp
[Serializable]
public struct NotificationData
{
    public string Message;        // Text to display
    public float DisplayDuration; // How long the text stays visible (seconds)
    public float FadeOutDuration; // Duration of fade-in and fade-out (seconds)
}
```

**Key points**:
- `struct` instead of `class` — avoids heap allocation (GC-friendly)
- `[Serializable]` — makes it directly editable in the Inspector
- `FadeOutDuration` controls both fade-in and fade-out, keeping the animation symmetric

---

### 3.2 ScriptableEventNotification.cs — Event Bus

```csharp
[CreateAssetMenu(menuName = "Soap/ScriptableEvents/Notification")]
public class ScriptableEventNotification : ScriptableEvent<NotificationData>
{
}
```

**Key points**:
- Extends SOAP's `ScriptableEvent<T>` with `NotificationData` as the payload type
- It is a ScriptableObject asset (`.asset` file) — create it once in the Project, share it scene-wide
- `[CreateAssetMenu]` adds it to the right-click menu in the Project window
- Core API: `Raise(NotificationData)` fires the event; `OnRaised` is the subscriber delegate

---

### 3.3 NotificationEventInvoker.cs — Inspector Trigger

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

**Key points**:
- `Raise()` — sends the notification pre-configured in the Inspector; ideal for static UnityEvent bindings (e.g., button clicks)
- `RaiseWithMessage(string)` — overrides only the message text at runtime, reusing the durations from Inspector; ideal for dynamic UnityEvent string bindings
- `var data = _data` performs a **value copy** (struct copy) — modifying `data.Message` does not mutate the original `_data` field

---

### 3.4 VRNotificationPanel.cs — UI Display Panel

This is the receiver side. It listens for the event and drives the UI animation.

**Mechanism 1: Subscribe / Unsubscribe**

```csharp
private void OnEnable()  => _notificationEvent.OnRaised += ShowNotification;
private void OnDisable() => _notificationEvent.OnRaised -= ShowNotification;
```

Subscribing in `OnEnable` and unsubscribing in `OnDisable` ensures the panel ignores events while hidden, and prevents memory leaks.

**Mechanism 2: VR Billboard**

```csharp
private void LateUpdate()
{
    transform.LookAt(
        transform.position + _mainCamera.transform.rotation * Vector3.forward,
        _mainCamera.transform.rotation * Vector3.up
    );
}
```

Rotates the panel toward the camera every frame so the text always faces the player in VR. `LateUpdate` is used so the panel updates after the camera has already moved for the frame.

**Mechanism 3: Interrupt and Restart**

```csharp
private void ShowNotification(NotificationData data)
{
    if (_currentCoroutine != null)
        StopCoroutine(_currentCoroutine);
    _notificationText.text = data.Message;
    _currentCoroutine = StartCoroutine(DisplayRoutine(...));
}
```

If the previous notification is still playing, it is stopped immediately and the new one begins. This prevents notifications from queuing up or overlapping.

**Mechanism 4: Fade Coroutine**

```csharp
private IEnumerator DisplayRoutine(float displayDuration, float fadeDuration)
{
    yield return Fade(0f, 1f, fadeDuration); // fade in
    yield return new WaitForSeconds(displayDuration); // hold
    yield return Fade(1f, 0f, fadeDuration); // fade out
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
    _canvasGroup.alpha = to; // guarantee exact final value
}
```

`CanvasGroup.alpha` controls the whole panel's transparency. `Mathf.Lerp` produces a smooth linear interpolation each frame.

---

### 3.5 SoapNotificationFeedback.cs — Question System Adapter

This is an **extension example** showing how to plug the Notification module into another system.

```csharp
[RequireComponent(typeof(QuestionPanel))]
public class SoapNotificationFeedback : MonoBehaviour, IQuestionFeedback
{
    public void OnAnswerSubmitted(AnswerResult result)
    {
        Raise(result.IsAllCorrect ? _correctMessage : _wrongMessage);
    }
}
```

**Design highlights**:
- Implements `IQuestionFeedback` to hook into the question system's feedback pipeline
- Internally calls `_notificationEvent.Raise(data)` — reuses the Notification module as-is
- `_broadcastOnPresent` toggle optionally fires a notification when a question is presented
- Fully decoupled from `VRNotificationPanel`; both communicate only through the shared ScriptableObject asset

---

## 4. Setup Steps

### Step 1: Create the ScriptableObject Asset

In the Project window, right-click → `Create > Soap > ScriptableEvents > Notification`. Name it `notification_event` (or any name you prefer).

### Step 2: Configure VRNotificationPanel

1. Create a Canvas in the scene (World Space mode)
2. Add a `CanvasGroup` component to the Canvas root
3. Add a `TextMeshProUGUI` child object for the message text
4. Attach the `VRNotificationPanel` script
5. Drag `notification_event` into the `Notification Event` field
6. Drag the `TextMeshProUGUI` and `CanvasGroup` into their respective fields

### Step 3: Configure a Sender

**Option A — NotificationEventInvoker (Inspector-driven)**:
1. Attach `NotificationEventInvoker` to any GameObject
2. Drag `notification_event` into the `Event` field
3. Fill in the message text and durations in the `Data` field
4. Wire `Raise()` or `RaiseWithMessage(string)` to a UnityEvent

**Option B — Code-driven**:

```csharp
[SerializeField] private ScriptableEventNotification _notificationEvent;

void SendNotification()
{
    _notificationEvent.Raise(new NotificationData
    {
        Message = "Mission Complete!",
        DisplayDuration = 2f,
        FadeOutDuration = 0.3f
    });
}
```

---

## 5. Troubleshooting

| Problem | Likely Cause | Fix |
|---------|-------------|-----|
| Notification never appears | Panel disabled, or mismatched asset | Confirm sender and receiver reference the **same** `.asset` file |
| Panel does not face the player | `_mainCamera` is null | Ensure the scene has a camera tagged `MainCamera` |
| New notification cuts off the old one | Interrupt-and-restart behavior | This is intentional; extend with a queue if needed |
| Panel stays visible after fade-out | `CanvasGroup.alpha` not reaching 0 | Check that `Fade` coroutine assigns `_canvasGroup.alpha = to` at the end |

---

## 6. Extension Ideas

1. **Notification queue** — maintain a `Queue<NotificationData>` in `VRNotificationPanel` and play them one by one
2. **Notification types** — add a `NotificationType` enum to `NotificationData` to drive color or icon changes
3. **Sound feedback** — call `AudioSource.PlayOneShot` inside `ShowNotification`
4. **Position follow** — parent the panel to the player's head, or use `Vector3.SmoothDamp` for smooth tracking

---

## 7. Design Principles Summary

| Principle | How it's applied |
|-----------|-----------------|
| Decoupling | Sender and receiver share only a ScriptableObject asset — no direct references |
| GC-friendly | `NotificationData` is a struct — no heap allocation per notification |
| Lifecycle safety | OnEnable/OnDisable manage subscriptions — no memory leaks |
| VR-ready | LateUpdate billboard keeps text facing the player at all times |
| Interruptible | New notifications immediately replace old ones — no stacking |

---

> Project version tag: <260518.0>
