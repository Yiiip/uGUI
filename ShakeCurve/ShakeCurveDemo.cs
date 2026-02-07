using UnityEngine;
using Sirenix.OdinInspector;

namespace Meow.Runtime.HotUpdate
{
    /// <summary>
    /// 预采样噪声曲线抖动效果演示脚本
    /// 提供多种预设效果和参数控制，方便测试和对比不同配置
    /// </summary>
    [AddComponentMenu("UI/Shake Curve Demo")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Logic", "LOG004:禁止使用UnityEngine.Debug")]
    public class ShakeCurveDemo : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _target;

        [Title("预设抖动配置", bold: false, horizontalLine: false)]
        [SerializeField] private ShakePreset _lightShake = new ShakePreset { duration = 0.3f, strength = new Vector2(10f, 10f), envelopeType = ShakeCurveEffect.EnvelopeType.Exponential };
        [SerializeField] private ShakePreset _mediumShake = new ShakePreset { duration = 0.5f, strength = new Vector2(20f, 20f), envelopeType = ShakeCurveEffect.EnvelopeType.Exponential };
        [SerializeField] private ShakePreset _heavyShake = new ShakePreset { duration = 0.8f, strength = new Vector2(40f, 40f), envelopeType = ShakeCurveEffect.EnvelopeType.Exponential };
        [SerializeField] private ShakePreset _bounceShake = new ShakePreset { duration = 0.6f, strength = new Vector2(25f, 25f), envelopeType = ShakeCurveEffect.EnvelopeType.BounceOut };

        [Title("路径抖动配置", bold: false, horizontalLine: false)]
        [SerializeField] private float _pathDuration = 1f;
        [SerializeField] private float _pathRadius = 30f;
        [SerializeField] private float _pathRotations = 2f;
        [SerializeField] private Vector2 _pathScale = new Vector2(1.5f, 1f);

        [Title("自定义设置", bold: false, horizontalLine: false)]
        [SerializeField] private float _customDuration = 0.5f;
        [SerializeField] private Vector2 _customStrength = new Vector2(20f, 20f);
        [SerializeField] private ShakeCurveEffect.EnvelopeType _customEnvelope = ShakeCurveEffect.EnvelopeType.Exponential;

        [Title("运行时状态", bold: false, horizontalLine: false)]
        [ShowInInspector, ReadOnly, PropertyOrder(-1)]
        private string StatusInfo => _effect != null ? $"状态: {(IsPlaying ? "播放中" : "已停止")}\n进度: {Progress:P2}" : "未初始化";

        private bool IsPlaying => _effect != null && _effect.IsPlaying;
        private float Progress => _effect != null ? _effect.Progress : 0f;

        private ShakeCurveEffect _effect;

        [System.Serializable]
        public class ShakePreset
        {
            [LabelText("持续时间")] public float duration = 0.5f;
            [LabelText("强度")] public Vector2 strength = new(20f, 20f);
            [LabelText("衰减类型")] public ShakeCurveEffect.EnvelopeType envelopeType = ShakeCurveEffect.EnvelopeType.Exponential;
            [LabelText("采样分辨率")] public int resolution = 100;
        }

        private void Awake()
        {
            if (_target == null)
            {
                _target = GetComponent<RectTransform>();
            }

            // 自动添加 ShakeCurveEffect
            _effect = _target.GetComponent<ShakeCurveEffect>();
            if (_effect == null)
            {
                _effect = _target.gameObject.AddComponent<ShakeCurveEffect>();
            }
        }

        #region Public API - 用于UI按钮调用

        [Title("预设效果测试", bold: true)]
        [Button("轻微抖动", ButtonSizes.Medium), DisableInEditorMode]
        [GUIColor(0.7f, 0.9f, 1f)]
        public void PlayLightShake()
        {
            ApplyPreset(_lightShake);
        }

        [Button("中等抖动", ButtonSizes.Medium), DisableInEditorMode]
        [GUIColor(0.5f, 0.8f, 1f)]
        public void PlayMediumShake()
        {
            ApplyPreset(_mediumShake);
        }

        [Button("剧烈抖动", ButtonSizes.Medium), DisableInEditorMode]
        [GUIColor(1f, 0.6f, 0.6f)]
        public void PlayHeavyShake()
        {
            ApplyPreset(_heavyShake);
        }

        [Button("反弹抖动", ButtonSizes.Medium), DisableInEditorMode]
        [GUIColor(1f, 0.9f, 0.6f)]
        public void PlayBounceShake()
        {
            ApplyPreset(_bounceShake);
        }

        [Title("自定义与控制", bold: true)]
        [Button("播放自定义", ButtonSizes.Small), DisableInEditorMode]
        public void PlayCustomShake()
        {
            _effect.RegenerateCurve(_customDuration, 100, 0);
            _effect.SetStrength(_customStrength);
            _effect.SetEnvelopeType(_customEnvelope);
            _effect.Play();
        }

        [Button("重新生成曲线", ButtonSizes.Small)]
        public void RegenerateCurve()
        {
            _effect.RegenerateCurve();
        }

        [Button("停止播放", ButtonSizes.Small), DisableInEditorMode]
        [GUIColor(1f, 0.4f, 0.4f)]
        public void StopShake()
        {
            _effect.Stop();
        }

        [Button("循环播放", ButtonSizes.Small), DisableInEditorMode]
        [GUIColor(0.7f, 1f, 0.7f)]
        public void ToggleLoop()
        {
            if (_effect.IsPlaying)
            {
                _effect.Stop();
            }
            _effect.PlayLoop();
        }

        [Title("路径抖动效果", bold: true)]
        [Button("圆形路径", ButtonSizes.Medium), DisableInEditorMode]
        [GUIColor(0.6f, 1f, 0.8f)]
        public void PlayCircularPath()
        {
            _effect.Stop();
            _effect.Curve.SetPathMode(ShakePathMode.Circular);
            _effect.Curve.SetPathParameters(_pathRotations, _pathRadius, Vector2.one, true);
            _effect.Play();
        }

        [Button("8字形路径", ButtonSizes.Medium), DisableInEditorMode]
        [GUIColor(0.6f, 0.8f, 1f)]
        public void PlayFigure8Path()
        {
            _effect.Stop();
            _effect.Curve.SetPathMode(ShakePathMode.Figure8);
            _effect.Curve.SetPathParameters(_pathRotations, _pathRadius, Vector2.one, true);
            _effect.Play();
        }

        [Button("椭圆路径", ButtonSizes.Medium), DisableInEditorMode]
        [GUIColor(1f, 0.8f, 0.6f)]
        public void PlayEllipticalPath()
        {
            _effect.Stop();
            _effect.Curve.SetPathMode(ShakePathMode.Elliptical);
            _effect.Curve.SetPathParameters(_pathRotations, _pathRadius, _pathScale, true);
            _effect.Play();
        }

        [Button("螺旋路径", ButtonSizes.Medium), DisableInEditorMode]
        [GUIColor(1f, 0.7f, 1f)]
        public void PlaySpiralPath()
        {
            _effect.Stop();
            _effect.Curve.SetPathMode(ShakePathMode.Spiral);
            _effect.Curve.SetPathParameters(_pathRotations, _pathRadius, Vector2.one, true);
            _effect.Play();
        }

        [Button("来回线性", ButtonSizes.Medium), DisableInEditorMode]
        [GUIColor(0.8f, 0.8f, 1f)]
        public void PlayPingPongPath()
        {
            _effect.Stop();
            _effect.Curve.SetPathMode(ShakePathMode.PingPong);
            _effect.Curve.SetPathParameters(_pathRotations, _pathRadius, Vector2.one, true);
            _effect.Play();
        }

        [Button("星形路径", ButtonSizes.Medium), DisableInEditorMode]
        [GUIColor(1f, 1f, 0.6f)]
        public void PlayStarPath()
        {
            // 创建五角星路径
            var starPoints = new Vector2[11]; // 10个点 + 1个闭合点
            for (int i = 0; i < 11; i++)
            {
                float angle = i * Mathf.PI * 2f / 10f - Mathf.PI / 2f;
                float radius = (i % 2 == 0) ? 1f : 0.4f; // 外点和内点
                starPoints[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            }

            _effect.Stop();
            _effect.Curve.SetCustomPath(starPoints);
            _effect.Play();
        }

        #endregion

        #region Private Methods

        private void ApplyPreset(ShakePreset preset)
        {
            _effect.RegenerateCurve(preset.duration, preset.resolution, 0);
            _effect.SetStrength(preset.strength);
            _effect.SetEnvelopeType(preset.envelopeType);
            _effect.Play();
        }

        #endregion

        #region Context Menu

        [ContextMenu("Play Light Shake")]
        private void ContextPlayLight() => PlayLightShake();

        [ContextMenu("Play Medium Shake")]
        private void ContextPlayMedium() => PlayMediumShake();

        [ContextMenu("Play Heavy Shake")]
        private void ContextPlayHeavy() => PlayHeavyShake();

        [ContextMenu("Play Bounce Shake")]
        private void ContextPlayBounce() => PlayBounceShake();

        [ContextMenu("Regenerate Curve")]
        private void ContextRegenerate() => RegenerateCurve();

        #endregion
    }

    public static class ShakeCurveEffectExtensions
    {
        public static void SetEnvelopeType(this ShakeCurveEffect effect, ShakeCurveEffect.EnvelopeType type)
        {
            // 通过序列化反射设置私有字段（仅在编辑器中使用）
            #if UNITY_EDITOR
            var envelopeTypeField = typeof(ShakeCurveEffect).GetField("_envelopeType",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            envelopeTypeField?.SetValue(effect, type);
            #endif
        }
    }
}
