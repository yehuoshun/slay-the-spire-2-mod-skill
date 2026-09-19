# Godot 项目配置（project.godot）

> 学自 [Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) 的项目配置文件。Godot 项目标识文件，没有它游戏不认模组目录。

---

## 文件位置

项目根目录，与 `csproj` 同级：

```
MyMod/
├── MyModCode/
├── MyMod/
├── MyMod.csproj
├── MyMod.json
├── export_presets.cfg
├── project.godot              ← 这里
└── Sts2PathDiscovery.props
```

---

## 完整内容

```ini
; Engine configuration file.
; It's best edited using the editor UI and not directly,
; since the parameters that go here are not all obvious.
;
; Format:
;   [section] ; section goes between []
;   param=value ; assign values to parameters

config_version=5

[application]

config/name="MyMod"
config/features=PackedStringArray("4.5", "C#", "Mobile")
config/icon="res://MyMod/mod_image.png"

[display]

window/size/viewport_width=1920
window/size/viewport_height=1080
window/size/initial_position_type=3
window/stretch/mode="canvas_items"
window/stretch/aspect="expand"

[dotnet]

project/assembly_name="MyMod"

[rendering]

renderer/rendering_method="mobile"
environment/defaults/default_clear_color=Color(0.0923724, 0.122398, 0.116929, 1)
```

> 将以上内容中 `MyMod` 替换为实际项目名。

---

## 关键字段说明

| 字段 | 值 | 说明 |
|------|-----|------|
| `config_version` | `5` | Godot 配置版本号，跟引擎版本绑定，**不要手动改** |
| `config/name` | `"MyMod"` | 项目名称，Godot 编辑器中显示的名字 |
| `config/icon` | `"res://MyMod/mod_image.png"` | 模组图标路径，显示在游戏模组安装列表 |
| `config/features` | `"4.5", "C#", "Mobile"` | 项目特性标记。`"C#"` 必加，`"Mobile"` 是 Megadot 4.5.x 的渲染后端标记 |
| `viewport_width/height` | `1920x1080` | 游戏视口分辨率，匹配 STS2 标准 |
| `window/stretch/mode` | `"canvas_items"` | 拉伸模式，缩放时 Canvas 层按比例适配 |
| `project/assembly_name` | `"MyMod"` | C# 项目程序集名，**必须与 csproj 文件名一致** |
| `rendering_method` | `"mobile"` | Godot 4 渲染方法。STS2 使用 Mobile 渲染器 |

---

## 注意事项

- `config_version` 不要手动改，不同 Godot 引擎版本对应不同值。当前 Megadot 4.5.1 为 `5`
- `project/assembly_name` 必须与 `csproj` 文件名去掉扩展名一致，否则 Godot 找不到编译后的 DLL
- `config/icon` 指向 `mod_image.png`，该图片显示在游戏主菜单 → 模组管理列表