using UnityEngine;
using System;

namespace Meow.Runtime.HotUpdate
{
    /// <summary>
    /// 抖动路径模式
    /// </summary>
    public enum ShakePathMode
    {
        /// <summary>随机噪声（默认）</summary>
        [InspectorName("随机噪声")]
        Random,

        /// <summary>圆形路径</summary>
        [InspectorName("圆形路径")]
        Circular,

        /// <summary>8字形路径</summary>
        [InspectorName("8字形路径")]
        Figure8,

        /// <summary>螺旋路径</summary>
        [InspectorName("螺旋路径")]
        Spiral,

        /// <summary>椭圆路径</summary>
        [InspectorName("椭圆路径")]
        Elliptical,

        /// <summary>来回线性路径</summary>
        [InspectorName("来回线性")]
        PingPong,

        /// <summary>自定义点位路径</summary>
        [InspectorName("自定义路径")]
        CustomPath
    }

    /// <summary>
    /// 预采样抖动曲线 - 支持随机噪声和特定路径抖动
    /// 优势：运行时性能高、效果可预测、可在Inspector中可视化编辑
    /// </summary>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Logic", "LOG004:禁止使用UnityEngine.Debug")]
    public class ShakeCurve
    {
        #region Serialized Fields

        [SerializeField] private AnimationCurve _curveX;
        [SerializeField] private AnimationCurve _curveY;
        [SerializeField] [Min(0f)] private float _duration = 0.5f;
        [SerializeField] [Min(2)] private int _resolution = 60;
        [SerializeField] [Tooltip("0表示每次随机")] private int _seed = 0;

        [Header("Path Settings")]
        [SerializeField] private ShakePathMode _pathMode = ShakePathMode.Random;

        // 路径参数
        [SerializeField] [Min(1f)] private float _pathRotations = 1f; // 圈数
        [SerializeField] [Min(0f)] private float _pathRadius = 1f; // 半径
        [SerializeField] private Vector2 _pathScale = Vector2.one; // 路径缩放（用于椭圆）
        [SerializeField] private bool _pathClockwise = true; // 顺时针方向
        [SerializeField] [Tooltip("自定义路径点位（归一化坐标，-1到1）")] private Vector2[] _customPathPoints;

        #endregion

        #region Properties

        /// <summary>X轴噪声曲线（可在Inspector中可视化编辑）</summary>
        public AnimationCurve CurveX => _curveX;

        /// <summary>Y轴噪声曲线（可在Inspector中可视化编辑）</summary>
        public AnimationCurve CurveY => _curveY;

        /// <summary>抖动持续时间</summary>
        public float Duration => _duration;

        /// <summary>采样分辨率（关键点数量）</summary>
        public int Resolution => _resolution;

        /// <summary>随机种子（用于复现相同的噪声）</summary>
        public int Seed => _seed;

        /// <summary>路径模式</summary>
        public ShakePathMode PathMode => _pathMode;

        /// <summary>路径圈数（用于圆形/8字形/螺旋）</summary>
        public float PathRotations => _pathRotations;

        /// <summary>路径半径（用于圆形/8字形/螺旋）</summary>
        public float PathRadius => _pathRadius;

        /// <summary>路径缩放（用于椭圆）</summary>
        public Vector2 PathScale => _pathScale;

        /// <summary>是否顺时针</summary>
        public bool PathClockwise => _pathClockwise;

        /// <summary>自定义路径点位</summary>
        public Vector2[] CustomPathPoints => _customPathPoints;

        #endregion

        #region Constructors

        public ShakeCurve() { }

        public ShakeCurve(float duration, int resolution = 60, int seed = 0)
        {
            _duration = duration;
            _resolution = resolution;
            _seed = seed;
            Generate();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 生成抖动曲线
        /// </summary>
        public void Generate()
        {
            _curveX = new AnimationCurve();
            _curveY = new AnimationCurve();

            switch (_pathMode)
            {
                case ShakePathMode.Random:
                    GenerateRandomNoise();
                    break;
                case ShakePathMode.Circular:
                    GenerateCircularPath();
                    break;
                case ShakePathMode.Figure8:
                    GenerateFigure8Path();
                    break;
                case ShakePathMode.Spiral:
                    GenerateSpiralPath();
                    break;
                case ShakePathMode.Elliptical:
                    GenerateEllipticalPath();
                    break;
                case ShakePathMode.PingPong:
                    GeneratePingPongPath();
                    break;
                case ShakePathMode.CustomPath:
                    GenerateCustomPath();
                    break;
            }

            // 平滑曲线切线
            SmoothCurve(_curveX);
            SmoothCurve(_curveY);
        }

        /// <summary>
        /// 设置路径模式并重新生成
        /// </summary>
        public void SetPathMode(ShakePathMode mode)
        {
            _pathMode = mode;
            Generate();
        }

        /// <summary>
        /// 设置路径参数并重新生成
        /// </summary>
        public void SetPathParameters(float rotations = 1f, float radius = 1f, Vector2? scale = null, bool clockwise = true)
        {
            _pathRotations = rotations;
            _pathRadius = radius;
            if (scale.HasValue)
            {
                _pathScale = scale.Value;
            }
            _pathClockwise = clockwise;
            Generate();
        }

        /// <summary>
        /// 设置自定义路径点位并重新生成
        /// </summary>
        public void SetCustomPath(Vector2[] points)
        {
            _customPathPoints = points;
            _pathMode = ShakePathMode.CustomPath;
            Generate();
        }

        /// <summary>
        /// 使用新的随机种子重新生成噪声曲线
        /// </summary>
        public void Regenerate()
        {
            _seed = Environment.TickCount;
            Generate();
        }

        /// <summary>
        /// 使用当前种子重新生成噪声曲线（结果可复现）
        /// </summary>
        public void RegenerateWithCurrentSeed()
        {
            Generate();
        }

        /// <summary>
        /// 使用新的随机种子重新生成噪声曲线（不修改当前种子值）
        /// 用于种子为0时手动刷新曲线
        /// </summary>
        public void RegenerateWithNewSeed()
        {
            int originalSeed = _seed;
            int newSeed = Environment.TickCount + new System.Random().Next(1000);
            GenerateRandomNoiseWithSeed(newSeed);
            SmoothCurve(_curveX);
            SmoothCurve(_curveY);
            _seed = originalSeed; // 恢复原种子
        }

        /// <summary>
        /// 根据时间采样噪声值
        /// </summary>
        public Vector2 Evaluate(float time)
        {
            time = Mathf.Clamp(time, 0f, _duration);
            return new Vector2(
                _curveX != null ? _curveX.Evaluate(time) : 0f,
                _curveY != null ? _curveY.Evaluate(time) : 0f
            );
        }

        /// <summary>
        /// 根据归一化进度采样噪声值（0-1）
        /// </summary>
        public Vector2 EvaluateNormalized(float t)
        {
            return Evaluate(t * _duration);
        }

        /// <summary>
        /// 带衰减包络的采样
        /// </summary>
        /// <param name="time">当前时间</param>
        /// <param name="envelope">衰减包络（0-1的曲线或函数）</param>
        public Vector2 EvaluateWithEnvelope(float time, Func<float, float> envelope)
        {
            Vector2 noise = Evaluate(time);
            float envelopeValue = envelope != null ? envelope(time / _duration) : 1f;
            return noise * envelopeValue;
        }

        /// <summary>
        /// 带指数衰减的采样
        /// </summary>
        /// <param name="time">当前时间</param>
        /// <param name="decaySpeed">衰减速度</param>
        public Vector2 EvaluateWithDecay(float time, float decaySpeed = 5f)
        {
            Vector2 noise = Evaluate(time);
            float decay = Mathf.Exp(-(time / _duration) * decaySpeed);
            return noise * decay;
        }

        #endregion

        #region Editor Methods

#if UNITY_EDITOR
        /// <summary>
        /// 在编辑器中参数改变时重新生成曲线
        /// </summary>
        private void OnValidate()
        {
            if (_curveX == null || _curveX.keys.Length == 0)
            {
                return;
            }

            // 检查是否需要重新生成（duration 或 resolution 改变）
            float lastDuration = _curveX.keys[_curveX.keys.Length - 1].time;
            int lastResolution = _curveX.keys.Length - 1;

            if (!Mathf.Approximately(lastDuration, _duration) || lastResolution != _resolution)
            {
                Generate();
            }
        }
#endif

        #endregion

        #region Private Methods

        /// <summary>生成随机噪声</summary>
        private void GenerateRandomNoise()
        {
            var rng = new System.Random(_seed != 0 ? _seed : Environment.TickCount);

            for (int i = 0; i <= _resolution; i++)
            {
                float t = (float)i / _resolution;
                float time = t * _duration;

                // 生成 -1 到 1 的随机值
                float randomX = (float)rng.NextDouble() * 2f - 1f;
                float randomY = (float)rng.NextDouble() * 2f - 1f;

                _curveX.AddKey(new Keyframe(time, randomX, 0f, 0f));
                _curveY.AddKey(new Keyframe(time, randomY, 0f, 0f));
            }
        }

        /// <summary>使用指定种子生成随机噪声（不修改当前种子值）</summary>
        private void GenerateRandomNoiseWithSeed(int seed)
        {
            _curveX = new AnimationCurve();
            _curveY = new AnimationCurve();

            var rng = new System.Random(seed);

            for (int i = 0; i <= _resolution; i++)
            {
                float t = (float)i / _resolution;
                float time = t * _duration;

                // 生成 -1 到 1 的随机值
                float randomX = (float)rng.NextDouble() * 2f - 1f;
                float randomY = (float)rng.NextDouble() * 2f - 1f;

                _curveX.AddKey(new Keyframe(time, randomX, 0f, 0f));
                _curveY.AddKey(new Keyframe(time, randomY, 0f, 0f));
            }
        }

        /// <summary>生成圆形路径</summary>
        private void GenerateCircularPath()
        {
            float direction = _pathClockwise ? 1f : -1f;

            for (int i = 0; i <= _resolution; i++)
            {
                float t = (float)i / _resolution;
                float time = t * _duration;
                float angle = t * _pathRotations * Mathf.PI * 2f * direction;

                float x = Mathf.Cos(angle) * _pathRadius;
                float y = Mathf.Sin(angle) * _pathRadius;

                _curveX.AddKey(new Keyframe(time, x, 0f, 0f));
                _curveY.AddKey(new Keyframe(time, y, 0f, 0f));
            }
        }

        /// <summary>生成8字形路径（Lissajous曲线）</summary>
        private void GenerateFigure8Path()
        {
            float direction = _pathClockwise ? 1f : -1f;

            for (int i = 0; i <= _resolution; i++)
            {
                float t = (float)i / _resolution;
                float time = t * _duration;
                float angle = t * _pathRotations * Mathf.PI * 2f * direction;

                // 8字形：X轴1倍频，Y轴2倍频
                float x = Mathf.Sin(angle) * _pathRadius;
                float y = Mathf.Sin(angle * 2f) * _pathRadius;

                _curveX.AddKey(new Keyframe(time, x, 0f, 0f));
                _curveY.AddKey(new Keyframe(time, y, 0f, 0f));
            }
        }

        /// <summary>生成螺旋路径（从中心向外或向内）</summary>
        private void GenerateSpiralPath()
        {
            float direction = _pathClockwise ? 1f : -1f;

            for (int i = 0; i <= _resolution; i++)
            {
                float t = (float)i / _resolution;
                float time = t * _duration;
                float angle = t * _pathRotations * Mathf.PI * 2f * direction;
                float radius = t * _pathRadius; // 从0到最大半径

                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;

                _curveX.AddKey(new Keyframe(time, x, 0f, 0f));
                _curveY.AddKey(new Keyframe(time, y, 0f, 0f));
            }
        }

        /// <summary>生成椭圆路径</summary>
        private void GenerateEllipticalPath()
        {
            float direction = _pathClockwise ? 1f : -1f;

            for (int i = 0; i <= _resolution; i++)
            {
                float t = (float)i / _resolution;
                float time = t * _duration;
                float angle = t * _pathRotations * Mathf.PI * 2f * direction;

                float x = Mathf.Cos(angle) * _pathRadius * _pathScale.x;
                float y = Mathf.Sin(angle) * _pathRadius * _pathScale.y;

                _curveX.AddKey(new Keyframe(time, x, 0f, 0f));
                _curveY.AddKey(new Keyframe(time, y, 0f, 0f));
            }
        }

        /// <summary>生成来回线性路径</summary>
        private void GeneratePingPongPath()
        {
            for (int i = 0; i <= _resolution; i++)
            {
                float t = (float)i / _resolution;
                float time = t * _duration;

                // 使用 PingPong 函数实现来回移动
                float pingPong = Mathf.PingPong(t * _pathRotations * 2f, 1f);
                float x = Mathf.Lerp(-_pathRadius, _pathRadius, pingPong) * _pathScale.x;
                float y = Mathf.Lerp(-_pathRadius, _pathRadius, pingPong) * _pathScale.y;

                _curveX.AddKey(new Keyframe(time, x, 0f, 0f));
                _curveY.AddKey(new Keyframe(time, y, 0f, 0f));
            }
        }

        /// <summary>生成自定义路径</summary>
        private void GenerateCustomPath()
        {
            if (_customPathPoints == null || _customPathPoints.Length < 2)
            {
                // 如果没有有效点位，退化为圆形路径
                GenerateCircularPath();
                return;
            }

            for (int i = 0; i <= _resolution; i++)
            {
                float t = (float)i / _resolution;
                float time = t * _duration;

                // 在自定义点位之间插值
                float scaledT = t * (_customPathPoints.Length - 1);
                int index = Mathf.FloorToInt(scaledT);
                float localT = scaledT - index;

                Vector2 p0 = _customPathPoints[Mathf.Min(index, _customPathPoints.Length - 1)];
                Vector2 p1 = _customPathPoints[Mathf.Min(index + 1, _customPathPoints.Length - 1)];

                float x = Mathf.Lerp(p0.x, p1.x, localT);
                float y = Mathf.Lerp(p0.y, p1.y, localT);

                _curveX.AddKey(new Keyframe(time, x, 0f, 0f));
                _curveY.AddKey(new Keyframe(time, y, 0f, 0f));
            }
        }

        private void SmoothCurve(AnimationCurve curve)
        {
            for (int i = 0; i < curve.keys.Length; i++)
            {
                Keyframe key = curve.keys[i];
                float tangent = 0f;

                // 计算平滑切线
                if (i > 0 && i < curve.keys.Length - 1)
                {
                    float prevTime = curve.keys[i - 1].time;
                    float nextTime = curve.keys[i + 1].time;
                    float prevValue = curve.keys[i - 1].value;
                    float nextValue = curve.keys[i + 1].value;
                    tangent = (nextValue - prevValue) / (nextTime - prevTime) * 0.5f;
                }

                curve.MoveKey(i, new Keyframe(key.time, key.value, tangent, tangent));
            }
        }

        #endregion
    }

    /// <summary>
    /// 衰减包络预设
    /// </summary>
    public static class EnvelopePresets
    {
        /// <summary>线性衰减（1到0）</summary>
        public static float Linear(float t) => 1f - t;

        /// <summary>指数衰减</summary>
        public static float Exponential(float t, float decaySpeed = 3f) => Mathf.Exp(-t * decaySpeed);

        /// <summary>平滑阶跃衰减（SmoothStep）</summary>
        public static float SmoothStep(float t) => 1f - Mathf.SmoothStep(0f, 1f, t);

        /// <summary>弹性衰减（开始快，后面慢）</summary>
        public static float ElasticOut(float t) => 1f - Mathf.Pow(1f - t, 3f);

        /// <summary>反弹衰减</summary>
        public static float BounceOut(float t) => 1f - Bounce(t);

        private static float Bounce(float t)
        {
            if (t < 0.363636f)
            {
                return 7.5685f * t * t;
            }
            else if (t < 0.727272f)
            {
                return 7.5625f * (t -= 0.545454f) * t + 0.75f;
            }
            else if (t < 0.909090f)
            {
                return 7.5625f * (t -= 0.818181f) * t + 0.9375f;
            }
            else
            {
                return 7.5625f * (t -= 0.9545454f) * t + 0.984375f;
            }
        }
    }
}
