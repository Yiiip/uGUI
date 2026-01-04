# RectMask2DAdvanced - 高级矩形遮罩（四边独立软化）

## 📦 文件说明

### 核心组件
1. **RectMask2DAdvanced.cs** - 高级遮罩组件（自动管理子物体的软化效果）
2. **AdvancedSoftnessRenderer.cs** - 内部材质渲染器（自动添加，用户不可见）
3. **UI-AdvancedSoftness.shader** - 自定义Shader（支持四边独立软化）

### 辅助文件
4. **Editor/RectMask2DAdvancedEditor.cs** - 自定义Inspector编辑器（可视化预览）

---

## 🎯 核心特性

### 简化设计
- ✅ **只需一个组件** - 在父物体上挂载 `RectMask2DAdvanced` 即可
- ✅ **自动管理** - 子物体自动添加必要的组件，无需手动操作
- ✅ **可视化编辑** - 带有实时预览的自定义 Inspector
- ✅ **四边独立控制** - Left/Right/Top/Bottom 可以设置不同的软化值

### 工作原理
```
RectMask2DAdvanced (父级 - 你只需挂这一个组件)
    ├─ 自动为所有 Graphic 子物体添加 AdvancedSoftnessRenderer
    ├─ 统一管理所有子物体的软化参数
    ├─ 提供可视化 Inspector 编辑器
    └─ 支持运行时和编辑时预览
```

---

## 📖 使用方法

### 基础使用（三步搞定）

#### 步骤1：创建遮罩GameObject
```
1. 右键 Hierarchy -> Create Empty
2. 命名为 "MaskPanel"
3. 添加 RectMask2DAdvanced 组件（在 Add Component 中搜索）
```

#### 步骤2：添加子UI元素
```
1. 创建 Image、Text 或其他UI元素
2. 作为 MaskPanel 的子物体
3. 可以添加多个子物体，都会自动应用软化效果
```

#### 步骤3：设置软化参数
```
在 RectMask2DAdvanced 的 Inspector 中：
- Left Edge: 左边缘软化值（像素）
- Bottom Edge: 下边缘软化值
- Right Edge: 右边缘软化值
- Top Edge: 上边缘软化值
```

就这么简单！✨

---

### 代码控制（动态效果）

```csharp
using UnityEngine.UI;

public class MyScript : MonoBehaviour
{
    public RectMask2DAdvanced mask;

    void Start()
    {
        // 设置四边独立软化（左, 下, 右, 上）
        mask.edgeSoftness = new Vector4(10, 20, 30, 5);

        // 或者使用快捷方法
        mask.SetAllSoftness(15f);              // 所有边相同
        mask.SetHorizontalSoftness(20f);       // 左右相同
        mask.SetVerticalSoftness(20f);         // 上下相同
    }
}
```

---

## 🎨 常见效果预设

在 Inspector 中使用快速按钮，或手动设置以下值：

### 1. 均匀羽化（照片展示）
```
点击 "Set All to 15" 按钮
或手动设置：Left: 15, Bottom: 15, Right: 15, Top: 15
```

### 2. 滚动列表（上下提示可滚动）
```
点击 "Vertical Only" 按钮
或手动设置：Left: 0, Bottom: 20, Right: 0, Top: 20
```

### 3. 对话框淡入（从左下角进入）
```
点击 "Bottom Only" + "Left Only" 按钮
或手动设置：Left: 30, Bottom: 30, Right: 0, Top: 0
```

### 4. 技能图标（顶部微弱淡化）
```
点击 "Top Only" 按钮
或手动设置：Left: 0, Bottom: 0, Right: 0, Top: 5
```

### 5. 进度条（左侧淡化，右侧清晰）
```
点击 "Left Only" 按钮
或手动设置：Left: 20, Bottom: 0, Right: 0, Top: 0
```

---

## 🔧 API 参考

### RectMask2DAdvanced 属性

```csharp
// 四边独立软化（左, 下, 右, 上）
public Vector4 edgeSoftness { get; set; }

// 是否自动管理子物体组件（默认true）
public bool autoManageRenderers { get; set; }
```

### RectMask2DAdvanced 快捷方法

```csharp
// 设置所有边相同
mask.SetAllSoftness(15f);

// 设置水平边（左右）
mask.SetHorizontalSoftness(20f);

// 设置垂直边（上下）
mask.SetVerticalSoftness(20f);
```

---

## 🎮 Inspector 功能

### 可视化预览
- 实时显示每个边的软化效果
- 黄色区域表示软化范围
- 绿色区域表示有效遮罩区域

### 快速操作按钮
- **Set All to 0** - 清除所有软化
- **Set All to 15** - 快速应用均匀软化
- **Horizontal Only** - 只对左右边缘软化
- **Vertical Only** - 只对上下边缘软化
- **Top/Bottom/Left/Right Only** - 单边软化

---

## ⚠️ 常见问题

### Q1: 设置了 edgeSoftness 但没有效果？
**A:** 确认以下几点：
1. 父物体是否添加了 `RectMask2DAdvanced` 组件？
2. `autoManageRenderers` 是否为 true？
3. softness 值是否大于 0？
4. 子物体是否是 Graphic 组件（Image、Text 等）？
5. 项目中是否包含了 `UI-AdvancedSoftness.shader`？

### Q2: 只有部分子物体有软化效果？
**A:**
- 检查 `autoManageRenderers` 是否开启
- 确认子物体是否是 Graphic 组件
- 如果子物体手动添加了 `AdvancedSoftnessRenderer`，`RectMask2DAdvanced` 不会管理它

### Q3: 边缘看起来很硬，没有淡化效果？
**A:**
- 增大 softness 值（建议 10-50 像素）
- 检查 Canvas 的 Render Mode 是否为 Screen Space - Overlay 或 Camera
- 确保 `UI-AdvancedSoftness.shader` 已正确导入

### Q4: 性能下降严重？
**A:**
- 减少 softness 值
- 减少 UI 元素数量
- 考虑只使用标准的 RectMask2D（对称软化）

### Q5: 禁用 autoManageRenderers 后，如何手动控制？
**A:**
1. 在需要的子物体上手动添加 `AdvancedSoftnessRenderer` 组件
2. 单独设置该子物体的 `edgeSoftness` 属性
3. `RectMask2DAdvanced` 不会管理手动添加的组件

---

## 💡 最佳实践

1. **适度使用**：softness 值建议在 10-50 像素之间
2. **避免频繁修改**：不要在 Update 中频繁修改 softness 值
3. **层级设计**：不同 UI 元素可以使用不同的遮罩配置
4. **测试优化**：在真机上测试，确保性能可接受
5. **使用快速按钮**：Inspector 中的快速按钮可以快速设置常见效果

---

## 🔧 技术细节

### 自动管理机制
- `RectMask2DAdvanced` 自动为所有 Graphic 子物体添加 `AdvancedSoftnessRenderer`
- 自动添加的组件使用 `HideFlags.HideInInspector`，在 Inspector 中不可见
- 当子物体被销毁或移除时，自动清理对应的组件

### 材质管理
- 每个子物体使用 `IMaterialModifier` 接口修改材质
- 材质使用 `UI-AdvancedSoftness` shader
- 支持运行时和编辑时实时预览

### 坐标空间转换
- 自动处理 Canvas 空间到物体空间的坐标转换
- 支持不同 Canvas Render Mode
- 自动同步父遮罩的变化

---

## 🔗 相关链接

- Unity 官方文档：[RectMask2D](https://docs.unity3d.com/Packages/com.unity.ugui@latest/manual/script-RectMask2D.html)
- Shader 参考：Unity UI Default Shader

---

## 📄 许可

本代码基于 Unity UGUI 开源实现，可以自由使用和修改。
