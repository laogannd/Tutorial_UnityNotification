using Sirenix.OdinInspector;
using UnityEngine;

// 演示用途：在玩家不知不觉间缩短等待体验
// 当原始计时器剩余时间充足时，切换到更短的优化计时方案
public class TimerUXOptimizer : MonoBehaviour
{
    [TitleGroup("计时方案")]
    [LabelText("原始计时方案"), Required]
    [SerializeField] private TimedEventSender _originalTimer;

    [LabelText("优化计时方案"), Required]
    [SerializeField] private TimedEventSender _optimizedTimer;

    [TitleGroup("优化参数")]
    [LabelText("剩余时间阈值"), SuffixLabel("秒", overlay: true), MinValue(0f)]
    [InfoBox("当原始计时器剩余时间低于此值时，不执行优化（让原始方案自然结束）")]
    [SerializeField] private float _threshold = 5f;

    [TitleGroup("状态"), ReadOnly]
    [LabelText("已优化"), ShowInInspector]
    private bool _isOptimized;

    public bool IsOptimized => _isOptimized;

    // 外部调用：尝试执行优化方案
    [TitleGroup("调试")]
    [Button("执行优化", ButtonSizes.Medium)]
    public void TryOptimize()
    {
        if (_isOptimized) return;
        if (!_originalTimer.IsPlaying) return;

        if (_originalTimer.RemainingTime <= _threshold)
            return;

        _originalTimer.Pause();
        _optimizedTimer.Play();
        _isOptimized = true;
    }

    // 重置状态，允许下次再触发优化
    [Button("重置状态", ButtonSizes.Small)]
    public void ResetState()
    {
        _isOptimized = false;
    }
}
