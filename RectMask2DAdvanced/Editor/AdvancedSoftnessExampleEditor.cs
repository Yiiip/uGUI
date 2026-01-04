#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace UnityEngine.UI
{
    /// <summary>
    /// Custom editor for AdvancedSoftnessExample with improved UI
    /// </summary>
    [CustomEditor(typeof(AdvancedSoftnessExample))]
    public class AdvancedSoftnessExampleEditor : Editor
    {
        private SerializedProperty m_LeftSoftness;
        private SerializedProperty m_BottomSoftness;
        private SerializedProperty m_RightSoftness;
        private SerializedProperty m_TopSoftness;
        private SerializedProperty m_EnableAdvancedSoftness;

        private AdvancedSoftnessExample m_Target;

        private void OnEnable()
        {
            m_Target = target as AdvancedSoftnessExample;
            m_LeftSoftness = serializedObject.FindProperty("leftSoftness");
            m_BottomSoftness = serializedObject.FindProperty("bottomSoftness");
            m_RightSoftness = serializedObject.FindProperty("rightSoftness");
            m_TopSoftness = serializedObject.FindProperty("topSoftness");
            m_EnableAdvancedSoftness = serializedObject.FindProperty("enableAdvancedSoftness");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Advanced Softness Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure softness for each edge independently.\n" +
                "Values are in pixels representing the falloff range.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Enable toggle
            EditorGUILayout.PropertyField(m_EnableAdvancedSoftness, new GUIContent("Enable Advanced Softness"));

            if (!m_EnableAdvancedSoftness.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "Advanced softness is disabled. Enable it to use per-edge softness.",
                    MessageType.Warning
                );
            }

            EditorGUILayout.Space();

            // Edge softness controls with better layout
            EditorGUILayout.PropertyField(m_LeftSoftness, new GUIContent("Left Edge"));
            EditorGUILayout.PropertyField(m_RightSoftness, new GUIContent("Right Edge"));
            EditorGUILayout.PropertyField(m_TopSoftness, new GUIContent("Top Edge"));
            EditorGUILayout.PropertyField(m_BottomSoftness, new GUIContent("Bottom Edge"));

            EditorGUILayout.Space();

            // Quick actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            // 第一行：全部设置和方向设置
            EditorGUILayout.BeginHorizontal();
            Rect buttonRect1 = EditorGUILayout.GetControlRect(false, 25);
            float buttonWidth = buttonRect1.width / 4f;
            if (GUI.Button(new Rect(buttonRect1.x, buttonRect1.y, buttonWidth, buttonRect1.height), "Set All to 0"))
            {
                SetAllValues(0f);
            }
            if (GUI.Button(new Rect(buttonRect1.x + buttonWidth, buttonRect1.y, buttonWidth, buttonRect1.height), "Set All to 15"))
            {
                SetAllValues(15f);
            }
            if (GUI.Button(new Rect(buttonRect1.x + buttonWidth * 2, buttonRect1.y, buttonWidth, buttonRect1.height), "Horizontal Only"))
            {
                SetHorizontalOnly(15f);
            }
            if (GUI.Button(new Rect(buttonRect1.x + buttonWidth * 3, buttonRect1.y, buttonWidth, buttonRect1.height), "Vertical Only"))
            {
                SetVerticalOnly(15f);
            }
            EditorGUILayout.EndHorizontal();

            // 第二行：单边设置
            EditorGUILayout.BeginHorizontal();
            Rect buttonRect2 = EditorGUILayout.GetControlRect(false, 25);
            buttonWidth = buttonRect2.width / 4f;
            if (GUI.Button(new Rect(buttonRect2.x, buttonRect2.y, buttonWidth, buttonRect2.height), "Top Only"))
            {
                SetTopOnly(15f);
            }
            if (GUI.Button(new Rect(buttonRect2.x + buttonWidth, buttonRect2.y, buttonWidth, buttonRect2.height), "Bottom Only"))
            {
                SetBottomOnly(15f);
            }
            if (GUI.Button(new Rect(buttonRect2.x + buttonWidth * 2, buttonRect2.y, buttonWidth, buttonRect2.height), "Left Only"))
            {
                SetLeftOnly(15f);
            }
            if (GUI.Button(new Rect(buttonRect2.x + buttonWidth * 3, buttonRect2.y, buttonWidth, buttonRect2.height), "Right Only"))
            {
                SetRightOnly(15f);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Visual preview
            DrawSoftnessPreview();

            serializedObject.ApplyModifiedProperties();
        }

        private void SetAllValues(float value)
        {
            m_LeftSoftness.floatValue = value;
            m_BottomSoftness.floatValue = value;
            m_RightSoftness.floatValue = value;
            m_TopSoftness.floatValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        private void SetHorizontalOnly(float value)
        {
            m_LeftSoftness.floatValue = value;
            m_BottomSoftness.floatValue = 0f;
            m_RightSoftness.floatValue = value;
            m_TopSoftness.floatValue = 0f;
            serializedObject.ApplyModifiedProperties();
        }

        private void SetVerticalOnly(float value)
        {
            m_LeftSoftness.floatValue = 0f;
            m_BottomSoftness.floatValue = value;
            m_RightSoftness.floatValue = 0f;
            m_TopSoftness.floatValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        private void SetTopOnly(float value)
        {
            m_LeftSoftness.floatValue = 0f;
            m_BottomSoftness.floatValue = 0f;
            m_RightSoftness.floatValue = 0f;
            m_TopSoftness.floatValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        private void SetBottomOnly(float value)
        {
            m_LeftSoftness.floatValue = 0f;
            m_BottomSoftness.floatValue = value;
            m_RightSoftness.floatValue = 0f;
            m_TopSoftness.floatValue = 0f;
            serializedObject.ApplyModifiedProperties();
        }

        private void SetLeftOnly(float value)
        {
            m_LeftSoftness.floatValue = value;
            m_BottomSoftness.floatValue = 0f;
            m_RightSoftness.floatValue = 0f;
            m_TopSoftness.floatValue = 0f;
            serializedObject.ApplyModifiedProperties();
        }

        private void SetRightOnly(float value)
        {
            m_LeftSoftness.floatValue = 0f;
            m_BottomSoftness.floatValue = 0f;
            m_RightSoftness.floatValue = value;
            m_TopSoftness.floatValue = 0f;
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSoftnessPreview()
        {
            EditorGUILayout.LabelField("Softness Preview", EditorStyles.boldLabel);

            Rect previewRect = EditorGUILayout.GetControlRect(false, 100);
            previewRect = EditorGUI.IndentedRect(previewRect);

            EditorGUI.DrawRect(previewRect, new Color(0.1f, 0.1f, 0.1f));

            // Draw mask area
            float border = 2f;
            float availableWidth = previewRect.width - border * 2;
            float availableHeight = previewRect.height - border * 2;

            // Clamp softness values to available space
            float leftSoft = Mathf.Min(m_LeftSoftness.floatValue, availableWidth);
            float rightSoft = Mathf.Min(m_RightSoftness.floatValue, availableWidth);
            float bottomSoft = Mathf.Min(m_BottomSoftness.floatValue, availableHeight);
            float topSoft = Mathf.Min(m_TopSoftness.floatValue, availableHeight);

            // Clamp total softness to prevent negative mask area
            float totalWidth = leftSoft + rightSoft;
            float totalHeight = topSoft + bottomSoft;

            if (totalWidth > availableWidth)
            {
                float scale = availableWidth / totalWidth;
                leftSoft *= scale;
                rightSoft *= scale;
            }

            if (totalHeight > availableHeight)
            {
                float scale = availableHeight / totalHeight;
                topSoft *= scale;
                bottomSoft *= scale;
            }

            Rect maskRect = new Rect(
                previewRect.x + border + leftSoft,
                previewRect.y + border + topSoft,
                availableWidth - leftSoft - rightSoft,
                availableHeight - topSoft - bottomSoft
            );

            // Draw softness regions
            if (leftSoft > 0)
            {
                Rect leftSoftRect = new Rect(previewRect.x + border, previewRect.y + border, leftSoft, availableHeight);
                EditorGUI.DrawRect(leftSoftRect, new Color(1, 0.8f, 0, 0.3f));
                // Draw vertical boundary line from top to bottom
                float x = previewRect.x + border + leftSoft;
                float y1 = previewRect.y + border;
                float y2 = previewRect.y + border + availableHeight;
                Color oldColor = Handles.color;
                Handles.color = new Color(1, 0.9f, 0.3f, 0.8f);
                Handles.DrawAAPolyLine(2f, new Vector3(x, y1, 0), new Vector3(x, y2, 0));
                Handles.color = oldColor;
            }

            if (rightSoft > 0)
            {
                Rect rightSoftRect = new Rect(previewRect.x + border + availableWidth - rightSoft, previewRect.y + border, rightSoft, availableHeight);
                EditorGUI.DrawRect(rightSoftRect, new Color(1, 0.8f, 0, 0.3f));
                // Draw vertical boundary line from top to bottom
                float x = previewRect.x + border + availableWidth - rightSoft;
                float y1 = previewRect.y + border;
                float y2 = previewRect.y + border + availableHeight;
                Color oldColor = Handles.color;
                Handles.color = new Color(1, 0.9f, 0.3f, 0.8f);
                Handles.DrawAAPolyLine(2f, new Vector3(x, y1, 0), new Vector3(x, y2, 0));
                Handles.color = oldColor;
            }

            if (topSoft > 0)
            {
                Rect topSoftRect = new Rect(previewRect.x + border, previewRect.y + border, availableWidth, topSoft);
                EditorGUI.DrawRect(topSoftRect, new Color(1, 0.8f, 0, 0.3f));
                // Draw horizontal boundary line from left to right
                float y = previewRect.y + border + topSoft;
                float x1 = previewRect.x + border;
                float x2 = previewRect.x + border + availableWidth;
                Color oldColor = Handles.color;
                Handles.color = new Color(1, 0.9f, 0.3f, 0.8f);
                Handles.DrawAAPolyLine(2f, new Vector3(x1, y, 0), new Vector3(x2, y, 0));
                Handles.color = oldColor;
            }

            if (bottomSoft > 0)
            {
                Rect bottomSoftRect = new Rect(previewRect.x + border, previewRect.y + border + availableHeight - bottomSoft, availableWidth, bottomSoft);
                EditorGUI.DrawRect(bottomSoftRect, new Color(1, 0.8f, 0, 0.3f));
                // Draw horizontal boundary line from left to right
                float y = previewRect.y + border + availableHeight - bottomSoft;
                float x1 = previewRect.x + border;
                float x2 = previewRect.x + border + availableWidth;
                Color oldColor = Handles.color;
                Handles.color = new Color(1, 0.9f, 0.3f, 0.8f);
                Handles.DrawAAPolyLine(2f, new Vector3(x1, y, 0), new Vector3(x2, y, 0));
                Handles.color = oldColor;
            }

            // Draw main rect
            if (maskRect.width > 0 && maskRect.height > 0)
            {
                EditorGUI.DrawRect(maskRect, new Color(0, 1, 0.5f, 0.5f));
            }

            // Draw border
            Handles.DrawSolidRectangleWithOutline(previewRect, Color.clear, new Color(0.5f, 0.5f, 0.5f, 1));

            // Labels
            GUI.Label(new Rect(previewRect.x, previewRect.y + previewRect.height + 5, previewRect.width, 20),
                $"Left: {m_LeftSoftness.floatValue:F1} | Right: {m_RightSoftness.floatValue:F1} | Top: {m_TopSoftness.floatValue:F1} | Bottom: {m_BottomSoftness.floatValue:F1}",
                EditorStyles.miniLabel);
        }
    }
}
#endif
