# RectMask2DAdvanced - 四边独立软化使用指南

## 📦 文件说明

### 核心组件
1. **RectMask2DAdvanced.cs** - 高级遮罩组件（提供裁剪和软化参数）
2. **AdvancedSoftnessRenderer.cs** - 材质渲染器（实现真正的四边独立软化）
3. **AdvancedSoftnessExample.cs** - 示例组件（提供Inspector可视化配置）
4. **UI-AdvancedSoftness.shader** - 自定义Shader（支持四边独立软化）

### 辅助文件
5. **UsageExample.cs** - 使用示例代码
6. **Editor/AdvancedSoftnessExampleEditor.cs** - 自定义Inspector编辑器

---

## 🎯 核心概念

### 为什么需要两个组件？

```
RectMask2DAdvanced (父级)
    ├─ 职责1：裁剪子物体到矩形区域
    ├─ 职责2：传递 softness 参数
    └─ 限制：只能传递 Vector2 (水平/垂直取最大值)

AdvancedSoftnessRenderer (子级)
    ├─ 职责：修改材质，注入 _EdgeSoftness (Vector4)
    ├─ 效果：真正实现四边独立软化
    └─ 配合：需要使用 UI-AdvancedSoftness Shader
```

### 两种使用模式

#### 模式A：标准裁剪（使用RectMask2DAdvanced的softness）
- ✅ 简单，只需一个组件
- ⚠️ softness会取水平/垂直的最大值
- ✅ 适合需要对称软化的情况

#### 模式B：四边独立软化（推荐）
- ✅ 完全独立控制四个边缘
- ✅ 精细的视觉效果
- ⚠️ 需要两个组件配合

---

## 📖 使用方法

### 方法1：Inspector可视化配置（推荐新手）

#### 步骤1：创建遮罩
```
1. 右键 Hierarchy -> Create Empty
2. 命名为 "MaskPanel"
3. 添加 RectMask2DAdvanced 组件
4. 设置 Padding (可选)
```

#### 步骤2：创建内容
```
1. 创建 Image 或其他UI元素
2. 作为 MaskPanel 的子物体
```

#### 步骤3：添加软化组件
```
1. 在Image上添加 AdvancedSoftnessExample 组件
2. 在Inspector中设置：
   - Left Softness: 左边缘软化
   - Bottom Softness: 下边缘软化
   - Right Softness: 右边缘软化
   - Top Softness: 上边缘软化
```

#### 步骤4：配置材质（可选）
```
通常 AdvancedSoftnessExample 会自动处理材质。
如果需要手动配置：
1. 选择Image组件
2. 在Material属性中创建新材质
3. 设置Shader为 "UI/AdvancedSoftness"
```

---

### 方法2：代码控制（推荐动态效果）

```csharp
using UnityEngine.UI;

public class MyScript : MonoBehaviour
{
    void Start()
    {
        // 获取AdvancedSoftnessRenderer
        var softnessRenderer = GetComponent<AdvancedSoftnessRenderer>();

        // 设置四边独立软化
        softnessRenderer.edgeSoftness = new Vector4(
            10,  // Left
            20,  // Bottom
            30,  // Right
            5     // Top
        );

        // 启用高级软化
        softnessRenderer.useAdvancedSoftness = true;
    }
}
```

---

## 🎨 常见效果预设

### 1. 均匀羽化（照片展示）
```
Left: 15, Bottom: 15, Right: 15, Top: 15
```

### 2. 滚动列表（上下提示可滚动）
```
Left: 0, Bottom: 20, Right: 0, Top: 20
```

### 3. 对话框淡入（从左下角进入）
```
Left: 30, Bottom: 30, Right: 0, Top: 0
```

### 4. 技能图标（顶部微弱淡化）
```
Left: 0, Bottom: 0, Right: 0, Top: 5
```

### 5. 进度条（左侧淡化，右侧清晰）
```
Left: 20, Bottom: 0, Right: 0, Top: 0
```

---

## 🔧 快捷API

### AdvancedSoftnessExample 组件

```csharp
// 设置所有边相同
softnessExample.SetAllSoftness(15f);

// 设置水平边
softnessExample.SetHorizontalSoftness(20f);

// 设置垂直边
softnessExample.SetVerticalSoftness(20f);

// 直接设置各边
softnessExample.leftSoftness = 10f;
softnessExample.bottomSoftness = 20f;
softnessExample.rightSoftness = 30f;
softnessExample.topSoftness = 40f;
```

### RectMask2DAdvanced 组件

```csharp
// 设置遮罩内边距
mask.padding = new Vector4(10, 10, 10, 10); // 左, 下, 右, 上

// 设置软化（注意：这是Vector2，会取最大值）
mask.edgeSoftness = new Vector4(10, 20, 30, 40); // 左, 下, 右, 上
```

---

## ⚠️ 常见问题

### Q1: 设置了edgeSoftness但没有效果？
**A:** 确认以下几点：
1. Image上是否添加了 AdvancedSoftnessRenderer 或 AdvancedSoftnessExample？
2. 父物体是否添加了 RectMask2DAdvanced？
3. useAdvancedSoftness 是否为 true？
4. softness值是否大于0？
5. 项目中是否包含了 UI-AdvancedSoftness.shader？

### Q2: 调整任意一边的软化值>0后遮罩消失了？
**A:** 这通常是因为材质没有正确设置。确保：
1. UI-AdvancedSoftness.shader 已导入项目
2. AdvancedSoftnessRenderer 组件已启用
3. 如果手动设置材质，必须使用 UI-AdvancedSoftness shader

### Q3: 只有部分边缘有软化效果？
**A:**
- 如果使用 RectMask2DAdvanced.edgeSoftness：这是正常的，因为它只能传递Vector2
- 如果使用 AdvancedSoftnessRenderer：检查是否所有边都设置了正值

### Q4: 边缘看起来很硬，没有淡化效果？
**A:**
- 增大 softness 值（建议10-50像素）
- 检查 Canvas 的 Render Mode 是否为 Screen Space - Overlay 或 Camera

### Q5: 性能下降严重？
**A:**
- 减少 softness 值
- 减少 UI 元素数量
- 考虑只使用标准的 RectMask2D (Vector2 softness)

---

## 💡 最佳实践

1. **适度使用**：softness值建议在10-50像素之间
2. **避免频繁修改**：不要在Update中频繁修改softness值
3. **材质复用**：多个UI元素可以共享相同的材质
4. **分层设计**：不同UI元素可以使用不同的softness配置
5. **测试优化**：在真机上测试，确保性能可接受

---

## 📚 完整示例

参考 `UsageExample.cs` 文件，包含：
- 场景1：运行时动态控制
- 场景2：Inspector配置
- 场景3：滚动列表边缘淡化
- 场景4：对话框淡入效果
- 运行时动态调整示例

---

## 🔗 相关链接

- Unity官方文档：[RectMask2D](https://docs.unity3d.com/Packages/com.unity.ugui@latest/manual/script-RectMask2D.html)
- Shader参考：Unity UI Default Shader

---

## 📄 许可

本代码基于Unity UGUI开源实现，可以自由使用和修改。
