#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Meow.Runtime.HotUpdate
{
    /// <summary>
    /// ShakeCurveEffect 的自定义编辑器，提供曲线可视化
    /// </summary>
    [CustomEditor(typeof(ShakeCurveEffect))]
    [CanEditMultipleObjects]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Logic", "LOG004:禁止使用UnityEngine.Debug")]
    public class ShakeCurveEffectEditor : Editor
    {
        // ShakeCurve 子属性
        private SerializedProperty _curveX;
        private SerializedProperty _curveY;
        private SerializedProperty _duration;
        private SerializedProperty _resolution;
        private SerializedProperty _seed;
        private SerializedProperty _pathMode;
        private SerializedProperty _pathRotations;
        private SerializedProperty _pathRadius;
        private SerializedProperty _pathScale;
        private SerializedProperty _pathClockwise;
        private SerializedProperty _customPathPoints;

        private SerializedProperty _strength;
        private SerializedProperty _envelopeType;
        private SerializedProperty _customEnvelopeCurve;
        private SerializedProperty _decaySpeed;
        private SerializedProperty _playOnEnable;
        private SerializedProperty _loop;
        private SerializedProperty _ignoreTimeScale;
        private SerializedProperty _resetOnComplete;
        private SerializedProperty _onShakeStarted;
        private SerializedProperty _onShakeComplete;

        private ShakeCurveEffect _effect;
        private bool _showCurvePreview = true;
        private bool _showStrengthPreview = true;
        private bool _showEnvelopePreview = true;
        private bool _showPlayPreview = true;
        private bool _showEventsPreview = true;
        private bool _frameByFrame = false;

        private void OnEnable()
        {
            // 获取 ShakeCurve 的子属性
            _shakeCurve = serializedObject.FindProperty("_shakeCurve");
            _curveX = _shakeCurve.FindPropertyRelative("_curveX");
            _curveY = _shakeCurve.FindPropertyRelative("_curveY");
            _duration = _shakeCurve.FindPropertyRelative("_duration");
            _resolution = _shakeCurve.FindPropertyRelative("_resolution");
            _seed = _shakeCurve.FindPropertyRelative("_seed");
            _pathMode = _shakeCurve.FindPropertyRelative("_pathMode");
            _pathRotations = _shakeCurve.FindPropertyRelative("_pathRotations");
            _pathRadius = _shakeCurve.FindPropertyRelative("_pathRadius");
            _pathScale = _shakeCurve.FindPropertyRelative("_pathScale");
            _pathClockwise = _shakeCurve.FindPropertyRelative("_pathClockwise");
            _customPathPoints = _shakeCurve.FindPropertyRelative("_customPathPoints");

            _strength = serializedObject.FindProperty("_strength");
            _envelopeType = serializedObject.FindProperty("_envelopeType");
            _customEnvelopeCurve = serializedObject.FindProperty("_customEnvelopeCurve");
            _decaySpeed = serializedObject.FindProperty("_decaySpeed");
            _playOnEnable = serializedObject.FindProperty("_playOnEnable");
            _loop = serializedObject.FindProperty("_loop");
            _ignoreTimeScale = serializedObject.FindProperty("_ignoreTimeScale");
            _resetOnComplete = serializedObject.FindProperty("_resetOnComplete");
            _onShakeStarted = serializedObject.FindProperty("_onShakeStarted");
            _onShakeComplete = serializedObject.FindProperty("_onShakeComplete");

            _effect = (ShakeCurveEffect)target;
        }

        private SerializedProperty _shakeCurve;

        private static readonly string[] s_PathModeNames = System.Enum.GetNames(typeof(ShakePathMode));

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("预采样噪声曲线抖动效果", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Shake Curve Configuration
            DrawShakeCurveSection();
            EditorGUILayout.Space();

            // Strength Settings
            DrawStrengthSection();
            EditorGUILayout.Space();

            // Envelope Settings
            DrawEnvelopeSection();
            EditorGUILayout.Space();

            // Play Settings
            DrawPlaySection();
            EditorGUILayout.Space();

            // Events
            DrawEventsSection();

            serializedObject.ApplyModifiedProperties();

            // 底部按钮区域
            DrawActionButtons();
        }

        private void DrawShakeCurveSection()
        {
            _showCurvePreview = EditorGUILayout.Foldout(_showCurvePreview, "曲线配置", true);
            if (_showCurvePreview)
            {
                EditorGUI.indentLevel++;

                // 基础参数（带自动刷新）
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_duration, new GUIContent("持续时间 (s)"));
                EditorGUILayout.PropertyField(_resolution, new GUIContent("采样分辨率"));
                if (EditorGUI.EndChangeCheck())
                {
                    RefreshPathCurve();  // 持续时间或分辨率改变，重新生成曲线
                }

                // 种子仅在随机噪声模式下有效（带自动刷新）
                bool isRandomMode = _pathMode.enumValueIndex == (int)ShakePathMode.Random;
                EditorGUI.BeginDisabledGroup(!isRandomMode);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_seed, new GUIContent("随机种子 (0为随机)"));
                if (EditorGUI.EndChangeCheck() && isRandomMode)
                {
                    GenerateCurveWithCurrentSeed();  // 种子改变，使用新种子重新生成
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space();

                // 路径模式选择
                int pathModeIndex = _pathMode.enumValueIndex;
                string[] pathModeDisplayNames = new string[s_PathModeNames.Length];
                for (int i = 0; i < s_PathModeNames.Length; i++)
                {
                    var fieldInfo = typeof(ShakePathMode).GetField(s_PathModeNames[i]);
                    var attr = fieldInfo?.GetCustomAttributes(typeof(InspectorNameAttribute), false);
                    pathModeDisplayNames[i] = attr != null && attr.Length > 0
                        ? ((InspectorNameAttribute)attr[0]).displayName
                        : s_PathModeNames[i];
                }

                int newPathModeIndex = EditorGUILayout.Popup("路径模式", pathModeIndex, pathModeDisplayNames);
                if (newPathModeIndex != pathModeIndex)
                {
                    _pathMode.enumValueIndex = newPathModeIndex;

                    // 切换到自定义路径时，如果没有有效点位，提供默认点位
                    if ((ShakePathMode)newPathModeIndex == ShakePathMode.CustomPath)
                    {
                        if (_customPathPoints.arraySize < 2)
                        {
                            _customPathPoints.arraySize = 3;
                            _customPathPoints.GetArrayElementAtIndex(0).vector2Value = new Vector2(-0.5f, -0.5f);
                            _customPathPoints.GetArrayElementAtIndex(1).vector2Value = new Vector2(0.5f, -0.5f);
                            _customPathPoints.GetArrayElementAtIndex(2).vector2Value = new Vector2(0f, 0.5f);
                        }
                    }

                    // 路径模式改变，重新生成曲线
                    serializedObject.ApplyModifiedProperties();
                    _effect.Curve.Generate();
                    EditorUtility.SetDirty(target);
                }

                // 根据路径模式显示对应参数
                var currentMode = (ShakePathMode)_pathMode.enumValueIndex;
                DrawPathModeSpecificSettings(currentMode);

                EditorGUILayout.Space();

                // AnimationCurve 字段显示（可查看和手动编辑）
                EditorGUILayout.PropertyField(_curveX, new GUIContent("X轴曲线"));
                EditorGUILayout.PropertyField(_curveY, new GUIContent("Y轴曲线"));

                // 曲线预览（Unity AnimationCurve 编辑器已自带可视化）
                // if (_effect.Curve != null && _effect.Curve.CurveX != null)
                // {
                //     DrawCurvePreview(_effect.Curve.CurveX, "X轴噪声曲线", Color.red);
                //     DrawCurvePreview(_effect.Curve.CurveY, "Y轴噪声曲线", Color.green);
                // }
                // else
                // {
                //     EditorGUILayout.HelpBox("曲线未生成，点击下方按钮生成", MessageType.Info);
                // }

                // 随机生成按钮（仅当种子为0且为随机噪声模式时显示）
                if (currentMode == ShakePathMode.Random && _seed.intValue == 0)
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("随机更换曲线", GUILayout.Height(30)))
                    {
                        GenerateCurve();
                    }
                }

                // 曲线生成/刷新按钮（参数修改后已自动刷新，以下按钮已注释）
                // EditorGUILayout.Space();
                // EditorGUILayout.BeginHorizontal();

                // if (currentMode == ShakePathMode.Random)
                // {
                //     // 随机噪声模式：两个生成按钮
                //     if (GUILayout.Button("重新生成曲线", GUILayout.Height(30), GUILayout.ExpandWidth(true)))
                //     {
                //         GenerateCurve();
                //     }
                //     if (GUILayout.Button("使用当前种子生成", GUILayout.Height(30), GUILayout.ExpandWidth(true)))
                //     {
                //         GenerateCurveWithCurrentSeed();
                //     }
                // }
                // else
                // {
                //     // 路径模式：手动刷新按钮（参数已自动刷新，此按钮可选）
                //     if (GUILayout.Button("手动刷新曲线", GUILayout.Height(30), GUILayout.ExpandWidth(true)))
                //     {
                //         RefreshPathCurve();
                //     }
                // }

                // EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }
        }

        private void DrawPathModeSpecificSettings(ShakePathMode mode)
        {
            EditorGUI.indentLevel++;

            switch (mode)
            {
                case ShakePathMode.Random:
                    EditorGUILayout.HelpBox("随机噪声模式 - 生成不可预测的随机抖动", MessageType.Info);
                    break;

                case ShakePathMode.Circular:
                    EditorGUILayout.HelpBox("圆形路径 - 沿圆形轨迹旋转抖动", MessageType.Info);
                    DrawPathParameterFields();
                    break;

                case ShakePathMode.Figure8:
                    EditorGUILayout.HelpBox("8字形路径 - Lissajous 8字形曲线抖动", MessageType.Info);
                    DrawPathParameterFields();
                    break;

                case ShakePathMode.Spiral:
                    EditorGUILayout.HelpBox("螺旋路径 - 从中心向外螺旋抖动", MessageType.Info);
                    DrawPathParameterFields();
                    break;

                case ShakePathMode.Elliptical:
                    EditorGUILayout.HelpBox("椭圆路径 - 可缩放的椭圆轨迹抖动", MessageType.Info);
                    DrawPathParameterFields(withScale: true);
                    break;

                case ShakePathMode.PingPong:
                    EditorGUILayout.HelpBox("来回线性 - 线性来回移动抖动", MessageType.Info);
                    DrawPathParameterFields(withScale: true);
                    break;

                case ShakePathMode.CustomPath:
                    EditorGUILayout.HelpBox("自定义路径 - 根据指定点位插值生成路径", MessageType.Info);

                    // 绘制路径预览图
                    DrawCustomPathPreview();

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(_customPathPoints, new GUIContent("路径点位 (归一化 -1~1)"), true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        RefreshPathCurve();
                    }
                    if (_customPathPoints.arraySize < 2)
                    {
                        EditorGUILayout.HelpBox("自定义路径需要至少2个点位", MessageType.Warning);
                    }
                    break;
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 绘制路径参数字段（带自动刷新）
        /// </summary>
        private void DrawPathParameterFields(bool withScale = false)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_pathRotations, new GUIContent("圈数/循环次数"));
            EditorGUILayout.PropertyField(_pathRadius, new GUIContent("半径"));
            EditorGUILayout.PropertyField(_pathClockwise, new GUIContent("顺时针方向"));
            if (withScale)
            {
                EditorGUILayout.PropertyField(_pathScale, new GUIContent("缩放 (X/Y)"));
            }
            if (EditorGUI.EndChangeCheck())
            {
                RefreshPathCurve();
            }
        }

        /// <summary>
        /// 绘制自定义路径预览图
        /// </summary>
        private void DrawCustomPathPreview()
        {
            if (_customPathPoints.arraySize < 2)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("路径预览（归一化坐标系 -1~1）", EditorStyles.boldLabel);

            // 获取预览区域的矩形
            Rect rect = GUILayoutUtility.GetRect(200, 200);
            GUI.BeginGroup(rect);

            // 绘制背景
            EditorGUI.DrawRect(new Rect(0, 0, rect.width, rect.height), new Color(0.15f, 0.15f, 0.15f));

            // 绘制网格和中心线
            float centerX = rect.width * 0.5f;
            float centerY = rect.height * 0.5f;
            float scale = Mathf.Min(rect.width, rect.height) * 0.4f; // 缩放因子，留出边距

            // 绘制网格线（每 0.5 单位一条线）
            Handles.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            for (int i = -2; i <= 2; i++)
            {
                float offset = i * 0.5f * scale;
                // 垂直线
                Handles.DrawLine(new Vector3(centerX + offset, 0, 0), new Vector3(centerX + offset, rect.height, 0));
                // 水平线
                Handles.DrawLine(new Vector3(0, centerY - offset, 0), new Vector3(rect.width, centerY - offset, 0));
            }

            // 绘制中心轴
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            Handles.DrawLine(new Vector3(centerX, 0, 0), new Vector3(centerX, rect.height, 0)); // Y轴
            Handles.DrawLine(new Vector3(0, centerY, 0), new Vector3(rect.width, centerY, 0)); // X轴

            // 绘制边界框（-1 到 1 的范围）
            Handles.color = new Color(0.4f, 0.6f, 0.4f, 0.4f);
            float boundarySize = scale;
            Handles.DrawSolidRectangleWithOutline(
                new Rect(centerX - boundarySize, centerY - boundarySize, boundarySize * 2, boundarySize * 2),
                new Color(0, 0, 0, 0),
                new Color(0.4f, 0.6f, 0.4f, 0.5f)
            );

            // 读取所有点位
            Vector2[] points = new Vector2[_customPathPoints.arraySize];
            for (int i = 0; i < _customPathPoints.arraySize; i++)
            {
                points[i] = _customPathPoints.GetArrayElementAtIndex(i).vector2Value;
            }

            // 绘制连接线（路径）
            if (points.Length >= 2)
            {
                Handles.color = new Color(0.3f, 0.8f, 1f, 0.8f);
                Vector3[] linePoints = new Vector3[points.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    linePoints[i] = new Vector3(
                        centerX + points[i].x * scale,
                        centerY - points[i].y * scale,  // Y轴反转，Unity GUI坐标系Y向下
                        0
                    );
                }
                Handles.DrawAAPolyLine(2f, linePoints);
            }

            // 根据点位数量动态调整圆点大小，避免重叠
            // 点位越少，圆点越大；点位越多，圆点越小
            float dotRadius = Mathf.Lerp(6f, 1f, Mathf.Min(1f, (points.Length - 2) / 30f));

            // 绘制点位和序号
            for (int i = 0; i < points.Length; i++)
            {
                float x = centerX + points[i].x * scale;
                float y = centerY - points[i].y * scale;

                // 绘制点位
                Handles.color = i == 0 ? new Color(0.2f, 1f, 0.2f) : // 起点绿色
                               i == points.Length - 1 ? new Color(1f, 0.2f, 0.2f) : // 终点红色
                               new Color(0.2f, 0.6f, 1f); // 中间点蓝色
                Handles.DrawSolidDisc(new Vector3(x, y, 0), Vector3.forward, dotRadius);

                // 绘制序号（点位较少时显示）
                if (points.Length <= 20)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.9f);
                    GUI.Label(new Rect(x + dotRadius + 2, y - 10, 30, 20), i.ToString(), EditorStyles.miniLabel);
                }
            }

            // 绘制起点和终点标签
            if (points.Length >= 1)
            {
                float startX = centerX + points[0].x * scale;
                float startY = centerY - points[0].y * scale;
                GUI.color = new Color(0.2f, 1f, 0.2f);
                GUI.Label(new Rect(startX + 8, startY - 25, 40, 15), "起点", EditorStyles.miniLabel);
            }

            if (points.Length >= 2)
            {
                float endX = centerX + points[points.Length - 1].x * scale;
                float endY = centerY - points[points.Length - 1].y * scale;
                GUI.color = new Color(1f, 0.2f, 0.2f);
                GUI.Label(new Rect(endX + 8, endY - 25, 40, 15), "终点", EditorStyles.miniLabel);
            }

            // 绘制坐标轴标签
            GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            GUI.Label(new Rect(rect.width - 35, centerY - 10, 30, 20), "+X", EditorStyles.miniLabel);
            GUI.Label(new Rect(5, centerY - 10, 30, 20), "-X", EditorStyles.miniLabel);
            GUI.Label(new Rect(centerX - 10, 5, 20, 15), "+Y", EditorStyles.miniLabel);
            GUI.Label(new Rect(centerX - 10, rect.height - 20, 20, 15), "-Y", EditorStyles.miniLabel);

            // 恢复 GUI 颜色，避免影响后续绘制
            GUI.color = Color.white;

            GUI.EndGroup();
            EditorGUILayout.EndVertical();
        }

        private void DrawStrengthSection()
        {
            _showStrengthPreview = EditorGUILayout.Foldout(_showStrengthPreview, "强度设置", true);
            if (_showStrengthPreview)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_strength, new GUIContent("Strength"));
                EditorGUI.indentLevel--;
            }
        }

        private void DrawEnvelopeSection()
        {
            _showEnvelopePreview = EditorGUILayout.Foldout(_showEnvelopePreview, "衰减设置", true);
            if (_showEnvelopePreview)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_envelopeType, new GUIContent("Envelope Type"));

                if ((ShakeCurveEffect.EnvelopeType)_envelopeType.enumValueIndex == ShakeCurveEffect.EnvelopeType.CustomCurve)
                {
                    EditorGUILayout.PropertyField(_customEnvelopeCurve, new GUIContent("Custom Envelope Curve"));
                }
                else if ((ShakeCurveEffect.EnvelopeType)_envelopeType.enumValueIndex == ShakeCurveEffect.EnvelopeType.Exponential)
                {
                    EditorGUILayout.PropertyField(_decaySpeed, new GUIContent("Decay Speed"));
                }

                // 衰减包络预览
                DrawEnvelopePreview();

                EditorGUI.indentLevel--;
            }
        }

        private void DrawPlaySection()
        {
            _showPlayPreview = EditorGUILayout.Foldout(_showPlayPreview, "播放设置", true);
            if (_showPlayPreview)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_playOnEnable, new GUIContent("Play On Enable"));
                EditorGUILayout.PropertyField(_loop, new GUIContent("Loop"));
                EditorGUILayout.PropertyField(_ignoreTimeScale, new GUIContent("Ignore Time Scale"));

                // Reset On Complete 仅在非循环模式下有效
                bool isLooping = _loop.boolValue;
                EditorGUI.BeginDisabledGroup(isLooping);
                EditorGUILayout.PropertyField(_resetOnComplete, new GUIContent("Reset On Complete" + (isLooping ? " (循环时无效)" : "")));
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
            }
        }

        private void DrawEventsSection()
        {
            _showEventsPreview = EditorGUILayout.Foldout(_showEventsPreview, "事件", true);
            if (_showEventsPreview)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_onShakeStarted, new GUIContent("On Shake Started"));
                EditorGUILayout.PropertyField(_onShakeComplete, new GUIContent("On Shake Complete"));
                EditorGUI.indentLevel--;
            }
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space();

            // Frame By Frame Toggle
            _frameByFrame = EditorGUILayout.Toggle(new GUIContent("Frame By Frame", "勾选后播放会暂停编辑器，可逐帧预览效果"), _frameByFrame);

            EditorGUILayout.BeginHorizontal();

            // 播放按钮 - 播放中时置灰
            bool canPlay = Application.isPlaying && !_effect.IsPlaying;
            GUI.enabled = canPlay;
            if (GUILayout.Button("播放", GUILayout.Height(30), GUILayout.ExpandWidth(true)))
            {
                PreviewInEditor();
            }

            // 停止按钮 - 未播放时置灰
            bool canStop = _effect.IsPlaying;
            GUI.enabled = canStop;
            if (GUILayout.Button("停止", GUILayout.Height(30), GUILayout.ExpandWidth(true)))
            {
                _effect.Stop();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            // 播放状态显示
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox($"播放状态: {(_effect.IsPlaying ? "播放中" : "已停止")}\n进度: {_effect.Progress:P2}", MessageType.None);
            }
        }

        // private void DrawCurvePreview(AnimationCurve curve, string label, Color color) //目前未调用，但方法保留以备将来使用
        // {
        //     if (curve == null || curve.length == 0)
        //     {
        //         return;
        //     }
        //
        //     EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        //     EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        //
        //     Rect rect = GUILayoutUtility.GetRect(200, 80);
        //     GUI.BeginGroup(rect);
        //
        //     // 绘制背景
        //     EditorGUI.DrawRect(new Rect(0, 0, rect.width, rect.height), new Color(0.2f, 0.2f, 0.2f));
        //
        //     // 绘制中心线
        //     float centerY = rect.height * 0.5f;
        //     Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        //     Handles.DrawLine(new Vector3(0, centerY, 0), new Vector3(rect.width, centerY, 0));
        //
        //     // 绘制曲线（使用足够的采样点来保证平滑）
        //     Handles.color = color;
        //     int sampleCount = Mathf.Max(100, (int)(rect.width / 2));
        //     Vector3[] points = new Vector3[sampleCount];
        //     for (int i = 0; i < sampleCount; i++)
        //     {
        //         float t = (float)i / (sampleCount - 1);
        //         float value = curve.Evaluate(t * _effect.Curve.Duration);
        //         float x = t * rect.width;
        //         float y = centerY - value * rect.height * 0.4f;
        //         points[i] = new Vector3(x, y, 0);
        //     }
        //     Handles.DrawAAPolyLine(2f, points);
        //
        //     GUI.EndGroup();
        //     EditorGUILayout.EndVertical();
        // }

        private void DrawEnvelopePreview()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("衰减曲线预览", EditorStyles.boldLabel);

            Rect rect = GUILayoutUtility.GetRect(200, 60);
            GUI.BeginGroup(rect);

            // 绘制背景
            EditorGUI.DrawRect(new Rect(0, 0, rect.width, rect.height), new Color(0.2f, 0.2f, 0.2f));

            // 绘制包络曲线
            Handles.color = Color.yellow;
            Vector3[] points = new Vector3[50];
            for (int i = 0; i < points.Length; i++)
            {
                float t = (float)i / (points.Length - 1);
                float value = GetEnvelopeValue(t);
                float x = t * rect.width;
                float y = rect.height - value * rect.height;
                points[i] = new Vector3(x, y, 0);
            }
            Handles.DrawAAPolyLine(2f, points);

            GUI.EndGroup();
            EditorGUILayout.EndVertical();
        }

        private float GetEnvelopeValue(float t)
        {
            var envelopeType = (ShakeCurveEffect.EnvelopeType)_envelopeType.enumValueIndex;
            var decaySpeed = _decaySpeed.floatValue;
            var customCurve = _customEnvelopeCurve != null ? _customEnvelopeCurve.animationCurveValue : null;

            return ShakeCurveEffect.GetEnvelopeValue(envelopeType, t, decaySpeed, customCurve);
        }

        private void GenerateCurve()
        {
            serializedObject.ApplyModifiedProperties();
            _effect.RegenerateCurveWithNewSeed();
            EditorUtility.SetDirty(target);
        }

        private void GenerateCurveWithCurrentSeed()
        {
            serializedObject.ApplyModifiedProperties();
            _effect.RegenerateCurveWithCurrentSeed();
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// 刷新曲线（不修改种子，仅用当前参数重新生成）
        /// 用于路径模式下的参数修改后自动刷新，或手动刷新
        /// </summary>
        private void RefreshPathCurve()
        {
            serializedObject.ApplyModifiedProperties();
            _effect.Curve.Generate();
            EditorUtility.SetDirty(target);
        }

        private void PreviewInEditor()
        {
            if (_effect.Curve == null || _effect.Curve.CurveX == null)
            {
                _effect.RegenerateCurve();
            }

            EditorUtility.SetDirty(target);

            if (!Application.isPlaying)
            {
                Debug.Log("预览曲线已生成。进入播放模式查看完整效果。");
            }
            else
            {
                _effect.Play();

                // 逐帧暂停功能
                if (_frameByFrame)
                {
                    EditorApplication.isPaused = true;
                }
            }
        }
    }
}
#endif
