using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

namespace Meow.Runtime.HotUpdate
{
    /// <summary>
    /// 预采样噪声曲线抖动效果组件
    /// 使用预先生成的AnimationCurve来驱动UI元素的抖动，性能高效且效果可预测
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("UI/Shake Curve Effect")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Logic", "LOG004:禁止使用UnityEngine.Debug")]
    public class ShakeCurveEffect : MonoBehaviour
    {
        #region Enums

        public enum EnvelopeType
        {
            [InspectorName("无衰减")]
            None,
            [InspectorName("线性衰减")]
            Linear,
            [InspectorName("指数衰减")]
            Exponential,
            [InspectorName("平滑阶跃衰减")]
            SmoothStep,
            [InspectorName("弹性衰减")]
            ElasticOut,
            [InspectorName("反弹衰减")]
            BounceOut,
            [InspectorName("自定义曲线")]
            CustomCurve
        }

        #endregion

        #region Serialized Fields

        [SerializeField] private ShakeCurve _shakeCurve = new ShakeCurve(0.5f);

        [SerializeField] private Vector2 _strength = new Vector2(20f, 20f);

        [SerializeField] private EnvelopeType _envelopeType = EnvelopeType.Exponential;
        [SerializeField] private AnimationCurve _customEnvelopeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        [SerializeField] private float _decaySpeed = 3f;

        [SerializeField] private bool _playOnEnable = false;
        [SerializeField] private bool _loop = false;
        [SerializeField] private bool _ignoreTimeScale = true;
        [SerializeField] private bool _resetOnComplete = true;

        [SerializeField] private UnityEvent _onShakeStarted;
        [SerializeField] private UnityEvent _onShakeComplete;

        #endregion

        #region Properties

        public ShakeCurve Curve => _shakeCurve;
        public bool IsPlaying { get; private set; }
        public float Progress { get; private set; }
        public float ElapsedTime { get; private set; }

        /// <summary>
        /// 是否忽略时间缩放
        /// </summary>
        public bool IgnoreTimeScale
        {
            get => _ignoreTimeScale;
            set => _ignoreTimeScale = value;
        }

        #endregion

        #region Private Fields

        private const float MIN_OFFSET_THRESHOLD = 0.1f;
        private Vector2 _originalPosition;
        private RectTransform _rectTransform;

#if UNITY_EDITOR
        private float _cachedDuration;
        private int _cachedResolution;
#endif

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originalPosition = _rectTransform.anchoredPosition;

            // 如果曲线没有生成，则生成一次
            if (_shakeCurve.CurveX == null || _shakeCurve.CurveX.keys.Length == 0)
            {
                _shakeCurve.Generate();
            }
        }

        private void OnEnable()
        {
            if (_playOnEnable)
            {
                if (_loop)
                {
                    PlayLoop();
                }
                else
                {
                    Play();
                }
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                return;
            }

            // 更新时间
            float deltaTime = _ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
            ElapsedTime += deltaTime;
            Progress = Mathf.Clamp01(ElapsedTime / _shakeCurve.Duration);

            // 应用抖动
            ApplyShake();

            // 检查是否完成
            if (Progress >= 1f)
            {
                if (_loop)
                {
                    ElapsedTime = 0f;
                    Progress = 0f;
                }
                else
                {
                    CompleteShake();
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 播放抖动效果
        /// </summary>
        public void Play()
        {
            if (_shakeCurve.Duration <= 0f)
            {
                Debug.LogWarning("[ShakeCurveEffect] Duration must be greater than 0");
                return;
            }

            // 当种子为0且为随机噪声模式时，每次播放使用新的随机曲线
            if (_shakeCurve.Seed == 0 && _shakeCurve.PathMode == ShakePathMode.Random)
            {
                _shakeCurve.RegenerateWithNewSeed();
            }

            IsPlaying = true;
            ElapsedTime = 0f;
            Progress = 0f;
            _originalPosition = _rectTransform.anchoredPosition;
            _onShakeStarted?.Invoke();
        }

        /// <summary>
        /// 播放循环抖动效果
        /// </summary>
        public void PlayLoop()
        {
            _loop = true;
            Play();
        }

        /// <summary>
        /// 停止抖动效果
        /// </summary>
        public void Stop(bool resetPosition = true)
        {
            IsPlaying = false;
            if (resetPosition)
            {
                _rectTransform.anchoredPosition = _originalPosition;
            }
        }

        /// <summary>
        /// 暂停抖动效果
        /// </summary>
        public void Pause()
        {
            IsPlaying = false;
        }

        /// <summary>
        /// 恢复抖动效果
        /// </summary>
        public void Resume()
        {
            if (Progress < 1f)
            {
                IsPlaying = true;
            }
        }

        /// <summary>
        /// 使用新的随机种子重新生成噪声曲线
        /// </summary>
        public void RegenerateCurve()
        {
            _shakeCurve.Regenerate();
        }

        /// <summary>
        /// 使用当前种子重新生成噪声曲线（结果可复现）
        /// </summary>
        public void RegenerateCurveWithCurrentSeed()
        {
            _shakeCurve.RegenerateWithCurrentSeed();
        }

        /// <summary>
        /// 使用新的随机种子重新生成噪声曲线（不修改当前种子值）
        /// 用于种子为0时手动刷新曲线
        /// </summary>
        public void RegenerateCurveWithNewSeed()
        {
            _shakeCurve.RegenerateWithNewSeed();
        }

        /// <summary>
        /// 使用新的参数重新生成曲线
        /// </summary>
        public void RegenerateCurve(float duration, int resolution = 100, int seed = 0)
        {
            _shakeCurve = new ShakeCurve(duration, resolution, seed);
            _shakeCurve.Generate();
        }

        /// <summary>
        /// 设置抖动强度
        /// </summary>
        public void SetStrength(Vector2 strength)
        {
            _strength = strength;
        }

        /// <summary>
        /// 设置路径模式并重新生成曲线
        /// </summary>
        public void SetPathMode(ShakePathMode mode)
        {
            _shakeCurve.SetPathMode(mode);
        }

        /// <summary>
        /// 设置路径参数并重新生成曲线
        /// </summary>
        public void SetPathParameters(float rotations = 1f, float radius = 1f, Vector2? scale = null, bool clockwise = true)
        {
            _shakeCurve.SetPathParameters(rotations, radius, scale, clockwise);
        }

        /// <summary>
        /// 设置自定义路径点位并重新生成曲线
        /// </summary>
        public void SetCustomPath(Vector2[] points)
        {
            _shakeCurve.SetCustomPath(points);
        }

        /// <summary>
        /// 根据包络类型计算衰减值（静态辅助方法）
        /// </summary>
        public static float GetEnvelopeValue(EnvelopeType type, float t, float decaySpeed = 3f, AnimationCurve customCurve = null)
        {
            return type switch
            {
                EnvelopeType.None => 1f,
                EnvelopeType.Linear => EnvelopePresets.Linear(t),
                EnvelopeType.Exponential => EnvelopePresets.Exponential(t, decaySpeed),
                EnvelopeType.SmoothStep => EnvelopePresets.SmoothStep(t),
                EnvelopeType.ElasticOut => EnvelopePresets.ElasticOut(t),
                EnvelopeType.BounceOut => EnvelopePresets.BounceOut(t),
                EnvelopeType.CustomCurve => customCurve != null ? customCurve.Evaluate(t) : 1f,
                _ => 1f,
            };
        }

        #endregion

        #region Private Methods

        private void ApplyShake()
        {
            // 获取基础噪声值
            Vector2 noise = _shakeCurve.Evaluate(ElapsedTime);

            // 应用衰减包络
            float envelope = GetEnvelopeValue(Progress);
            noise *= envelope;

            // 应用强度
            Vector2 offset = noise * _strength;

            // 当偏移量大于阈值时才更新位置，减少移动端 GPU/CPU 通信开销
            if (offset.sqrMagnitude > MIN_OFFSET_THRESHOLD * MIN_OFFSET_THRESHOLD)
            {
                _rectTransform.anchoredPosition = _originalPosition + offset;
            }
        }

        private float GetEnvelopeValue(float t)
        {
            return GetEnvelopeValue(_envelopeType, t, _decaySpeed, _customEnvelopeCurve);
        }

        private void CompleteShake()
        {
            IsPlaying = false;

            if (_resetOnComplete)
            {
                _rectTransform.anchoredPosition = _originalPosition;
            }

            _onShakeComplete?.Invoke();
        }

        #endregion

        #region Editor Methods

#if UNITY_EDITOR
        /// <summary>
        /// 在编辑器中预览曲线（仅在编辑器中调用）
        /// </summary>
        public void PreviewCurve()
        {
            if (!Application.isPlaying)
            {
                if (_shakeCurve.CurveX == null || _shakeCurve.CurveX.keys.Length == 0)
                {
                    _shakeCurve.Generate();
                }
            }
        }

        /// <summary>
        /// 在编辑器中参数改变时重新生成曲线
        /// </summary>
        private void OnValidate()
        {
            if (_shakeCurve == null || _shakeCurve.CurveX == null || _shakeCurve.CurveX.keys.Length == 0)
            {
                return;
            }

            // 检查是否需要重新生成（duration 或 resolution 改变）
            float currentDuration = _shakeCurve.Duration;
            int currentResolution = _shakeCurve.Resolution;

            if (!Mathf.Approximately(_cachedDuration, currentDuration) || _cachedResolution != currentResolution)
            {
                _shakeCurve.Generate();
                _cachedDuration = currentDuration;
                _cachedResolution = currentResolution;
            }
        }
#endif

        #endregion

        #region Static Helper Methods

        /// <summary>
        /// 快速添加并播放抖动效果
        /// </summary>
        public static ShakeCurveEffect Play(RectTransform target, float duration, Vector2 strength)
        {
            var effect = target.GetComponent<ShakeCurveEffect>();
            if (effect == null)
            {
                effect = target.gameObject.AddComponent<ShakeCurveEffect>();
            }

            effect._shakeCurve = new ShakeCurve(duration, 100, 0);
            effect._shakeCurve.Generate();
            effect._strength = strength;
            effect.Play();

            return effect;
        }

        /// <summary>
        /// 快速添加并播放带衰减的抖动效果
        /// </summary>
        public static ShakeCurveEffect PlayWithDecay(RectTransform target, float duration, Vector2 strength, float decaySpeed = 3f)
        {
            var effect = Play(target, duration, strength);
            effect._envelopeType = EnvelopeType.Exponential;
            effect._decaySpeed = decaySpeed;
            return effect;
        }

        /// <summary>
        /// 快速添加并播放圆形路径抖动效果
        /// </summary>
        public static ShakeCurveEffect PlayCircular(RectTransform target, float duration, float radius, float rotations = 1f, bool clockwise = true)
        {
            var effect = target.GetComponent<ShakeCurveEffect>();
            if (effect == null)
            {
                effect = target.gameObject.AddComponent<ShakeCurveEffect>();
            }

            effect._shakeCurve = new ShakeCurve(duration, 100, 0);
            effect._shakeCurve.SetPathMode(ShakePathMode.Circular);
            effect._shakeCurve.SetPathParameters(rotations, radius, Vector2.one, clockwise);
            effect.Play();

            return effect;
        }

        /// <summary>
        /// 快速添加并播放8字形路径抖动效果
        /// </summary>
        public static ShakeCurveEffect PlayFigure8(RectTransform target, float duration, float radius, float rotations = 1f, bool clockwise = true)
        {
            var effect = target.GetComponent<ShakeCurveEffect>();
            if (effect == null)
            {
                effect = target.gameObject.AddComponent<ShakeCurveEffect>();
            }

            effect._shakeCurve = new ShakeCurve(duration, 100, 0);
            effect._shakeCurve.SetPathMode(ShakePathMode.Figure8);
            effect._shakeCurve.SetPathParameters(rotations, radius, Vector2.one, clockwise);
            effect.Play();

            return effect;
        }

        /// <summary>
        /// 快速添加并播放椭圆路径抖动效果
        /// </summary>
        public static ShakeCurveEffect PlayElliptical(RectTransform target, float duration, Vector2 scale, float radius = 1f, float rotations = 1f, bool clockwise = true)
        {
            var effect = target.GetComponent<ShakeCurveEffect>();
            if (effect == null)
            {
                effect = target.gameObject.AddComponent<ShakeCurveEffect>();
            }

            effect._shakeCurve = new ShakeCurve(duration, 100, 0);
            effect._shakeCurve.SetPathMode(ShakePathMode.Elliptical);
            effect._shakeCurve.SetPathParameters(rotations, radius, scale, clockwise);
            effect.Play();

            return effect;
        }

        /// <summary>
        /// 快速添加并播放螺旋路径抖动效果
        /// </summary>
        public static ShakeCurveEffect PlaySpiral(RectTransform target, float duration, float maxRadius, float rotations = 1f, bool clockwise = true)
        {
            var effect = target.GetComponent<ShakeCurveEffect>();
            if (effect == null)
            {
                effect = target.gameObject.AddComponent<ShakeCurveEffect>();
            }

            effect._shakeCurve = new ShakeCurve(duration, 100, 0);
            effect._shakeCurve.SetPathMode(ShakePathMode.Spiral);
            effect._shakeCurve.SetPathParameters(rotations, maxRadius, Vector2.one, clockwise);
            effect.Play();

            return effect;
        }

        /// <summary>
        /// 快速添加并播放自定义路径抖动效果
        /// </summary>
        public static ShakeCurveEffect PlayCustomPath(RectTransform target, float duration, Vector2[] points)
        {
            var effect = target.GetComponent<ShakeCurveEffect>();
            if (effect == null)
            {
                effect = target.gameObject.AddComponent<ShakeCurveEffect>();
            }

            effect._shakeCurve = new ShakeCurve(duration, 100, 0);
            effect._shakeCurve.SetCustomPath(points);
            effect.Play();

            return effect;
        }

        #endregion
    }
}
