using System.Collections;
using TMPro;
using UnityEngine;

using Obvious.Soap;

public class VRNotificationPanel : MonoBehaviour
{
    [SerializeField] private ScriptableEventNotification _notificationEvent;
    [SerializeField] private TextMeshProUGUI _notificationText;
    [SerializeField] private CanvasGroup _canvasGroup;

    private Coroutine _currentCoroutine;
    private Camera _mainCamera;

    private void Awake() => _mainCamera = Camera.main;

    private void OnEnable() => _notificationEvent.OnRaised += ShowNotification;

    private void OnDisable() => _notificationEvent.OnRaised -= ShowNotification;

    private void LateUpdate()
    {
        if (_mainCamera == null) return;
        transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
            _mainCamera.transform.rotation * Vector3.up);
    }

    private void ShowNotification(NotificationData data)
    {
        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);
        _notificationText.text = data.Message;
        _currentCoroutine = StartCoroutine(DisplayRoutine(data.DisplayDuration, data.FadeOutDuration));
    }

    private IEnumerator DisplayRoutine(float displayDuration, float fadeDuration)
    {
        yield return Fade(0f, 1f, fadeDuration);
        yield return new WaitForSeconds(displayDuration);
        yield return Fade(1f, 0f, fadeDuration);
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
        _canvasGroup.alpha = to;
    }
}
