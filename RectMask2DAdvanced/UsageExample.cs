using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.UI
{
    /// <summary>
    /// 示例：如何正确使用RectMask2DAdvanced实现四边独立软化
    /// </summary>
    public class UsageExample : MonoBehaviour
    {
        /// <summary>
        /// 场景1：RectMask2DAdvanced + AdvancedSoftnessRenderer (推荐用于运行时动态控制)
        /// </summary>
        public void SetupScenario1()
        {
            // 1. 创建遮罩GameObject
            GameObject maskObject = new GameObject("Mask");
            maskObject.transform.SetParent(transform);

            // 2. 添加RectMask2DAdvanced (用于裁剪)
            RectMask2DAdvanced mask = maskObject.AddComponent<RectMask2DAdvanced>();
            mask.padding = new Vector4(10, 10, 10, 10); // 左, 下, 右, 上

            // 3. 创建Image作为子物体
            GameObject imageObject = new GameObject("Image");
            imageObject.transform.SetParent(maskObject.transform);

            // 4. 添加Image组件
            Image image = imageObject.AddComponent<Image>();
            image.color = Color.white;

            // 5. 添加AdvancedSoftnessRenderer (用于四边独立软化)
            AdvancedSoftnessRenderer softnessRenderer = imageObject.AddComponent<AdvancedSoftnessRenderer>();

            // 6. 设置四边软化 (左, 下, 右, 上)
            softnessRenderer.edgeSoftness = new Vector4(20, 30, 10, 5);
            softnessRenderer.useAdvancedSoftness = true;
        }

        /// <summary>
        /// 场景2：RectMask2DAdvanced + AdvancedSoftnessExample (推荐用于Inspector配置)
        /// </summary>
        public void SetupScenario2()
        {
            // 1. 创建遮罩GameObject
            GameObject maskObject = new GameObject("Mask");
            maskObject.transform.SetParent(transform);

            // 2. 添加RectMask2DAdvanced
            RectMask2DAdvanced mask = maskObject.AddComponent<RectMask2DAdvanced>();
            mask.padding = new Vector4(10, 10, 10, 10);

            // 3. 创建Image作为子物体
            GameObject imageObject = new GameObject("Image");
            imageObject.transform.SetParent(maskObject.transform);

            // 4. 添加Image组件
            Image image = imageObject.AddComponent<Image>();
            image.color = Color.white;

            // 5. 添加AdvancedSoftnessExample (提供Inspector可视化配置)
            AdvancedSoftnessExample softnessExample = imageObject.AddComponent<AdvancedSoftnessExample>();

            // 6. 通过代码设置软化值 (也可以在Inspector中设置)
            softnessExample.leftSoftness = 20;
            softnessExample.bottomSoftness = 30;
            softnessExample.rightSoftness = 10;
            softnessExample.topSoftness = 5;
        }

        /// <summary>
        /// 场景3：滚动列表边缘淡化 (只有上下边缘有软化)
        /// </summary>
        public void SetupScrollableList()
        {
            GameObject scrollMask = new GameObject("ScrollMask");
            scrollMask.transform.SetParent(transform);

            RectMask2DAdvanced mask = scrollMask.AddComponent<RectMask2DAdvanced>();

            // 创建内容Image
            GameObject contentImage = new GameObject("Content");
            contentImage.transform.SetParent(scrollMask.transform);
            Image image = contentImage.AddComponent<Image>();

            // 只对上下边缘应用软化，提示内容可滚动
            AdvancedSoftnessRenderer softnessRenderer = contentImage.AddComponent<AdvancedSoftnessRenderer>();
            softnessRenderer.edgeSoftness = new Vector4(0, 20, 0, 20); // 只有上下软化
            softnessRenderer.useAdvancedSoftness = true;
        }

        /// <summary>
        /// 场景4：对话框淡入效果 (只有左下边缘有软化)
        /// </summary>
        public void SetupDialogBox()
        {
            GameObject dialogMask = new GameObject("DialogMask");
            dialogMask.transform.SetParent(transform);

            RectMask2DAdvanced mask = dialogMask.AddComponent<RectMask2DAdvanced>();

            // 创建对话框背景Image
            GameObject dialogImage = new GameObject("DialogBackground");
            dialogImage.transform.SetParent(dialogMask.transform);
            Image image = dialogImage.AddComponent<Image>();

            // 只对左下边缘应用软化，适合从左下角进入的对话框
            AdvancedSoftnessRenderer softnessRenderer = dialogImage.AddComponent<AdvancedSoftnessRenderer>();
            softnessRenderer.edgeSoftness = new Vector4(30, 30, 0, 0); // 只有左下软化
            softnessRenderer.useAdvancedSoftness = true;
        }

        /// <summary>
        /// 运行时动态调整软化
        /// </summary>
        public void UpdateSoftnessDynamically(AdvancedSoftnessExample softnessExample)
        {
            // 淡入效果：从0逐渐增加到目标值
            float targetLeft = 20f;
            float currentLeft = softnessExample.leftSoftness;

            // 可以在Update或协程中逐渐改变
            softnessExample.leftSoftness = Mathf.Lerp(currentLeft, targetLeft, Time.deltaTime);
        }
    }
}
