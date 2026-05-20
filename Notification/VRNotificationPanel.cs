using System.Collections;
using TMPro;
using UnityEngine;

using Obvious.Soap;

public class VRNotificationPanel : MonoBehaviour
{
    [SerializeField] private ScriptableEventNotification _notificationEvent;
    [SerializeField] private TextMeshProUGUI _notificationText;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private AudioSource _audioSource;

    private Coroutine _currentCoroutine;
    private Coroutine _audioCoroutine;
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
        StopAudio();
        _notificationText.text = data.Message;
        _currentCoroutine = StartCoroutine(DisplayRoutine(data));
    }

    private IEnumerator DisplayRoutine(NotificationData data)
    {
        yield return Fade(0f, 1f, data.FadeOutDuration);

        if (data.Audio.Clip != null)
            _audioCoroutine = StartCoroutine(AudioRoutine(data.Audio));

        yield return new WaitForSeconds(data.DisplayDuration);
        yield return Fade(1f, 0f, data.FadeOutDuration);
        StopAudio();
        _currentCoroutine = null;
    }

    private IEnumerator AudioRoutine(NotificationAudioConfig config)
    {
        _audioSource.spatialBlend = config.SpatialBlend;
        _audioSource.minDistance = config.MinDistance;
        _audioSource.maxDistance = config.MaxDistance;
        _audioSource.pitch = config.Pitch;

        if (config.Delay > 0f)
            yield return new WaitForSeconds(config.Delay);

        if (config.Loop)
        {
            _audioSource.clip = config.Clip;
            _audioSource.volume = config.Volume;
            _audioSource.loop = true;
            _audioSource.Play();
        }
        else
        {
            _audioSource.PlayOneShot(config.Clip, config.Volume);
        }

        _audioCoroutine = null;
    }

    private void StopAudio()
    {
        if (_audioCoroutine != null)
        {
            StopCoroutine(_audioCoroutine);
            _audioCoroutine = null;
        }

        if (_audioSource == null) return;

        _audioSource.Stop();
        _audioSource.loop = false;
        _audioSource.clip = null;
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
