# 导出预设（export_presets.cfg）

> 学自 [Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) 的导出配置。Godot 打包 PCK 必需配置文件。

---

## 文件位置

项目根目录，与 `csproj` 同级：

```
MyMod/
├── MyModCode/
├── MyMod/
├── MyMod.csproj
├── MyMod.json
├── export_presets.cfg      ← 这里
├── project.godot
└── Sts2PathDiscovery.props
```

---

## 完整内容

```ini
[preset.0]

name="BasicExport"
platform="Windows Desktop"
runnable=true
advanced_options=false
custom_features=""
export_filter="all_resources"
include_filter=""
exclude_filter="MyMod.json"
export_path=""
patches=PackedStringArray()
encryption_include_filters=""
encryption_exclude_filters=""
seed=0
encrypt_pck=false
encrypt_directory=false

[preset.0.options]

custom_template/debug=""
custom_template/release=""
binary_format/embed_pck=false
texture_format/s3tc_bptc=true
texture_format/etc2_astc=false
shader_baker/enabled=false
binary_format/architecture="msil"
codesign/enable=false
```

> 将以上内容中 `MyMod.json` 替换为实际项目名。

---

## 关键字段说明

| 字段 | 值 | 说明 |
|------|-----|------|
| `name` | `"BasicExport"` | 预设名称，与 `--export-pack` 参数匹配 |
| `platform` | `"Windows Desktop"` | 目标平台。模组 PCK 打 Windows 即可 |
| `exclude_filter` | `"MyMod.json"` | **必须排除 manifest JSON**，避免 Godot 将其打包进 PCK（由 csproj 单独拷贝） |
| `binary_format/architecture` | `"msil"` | 跨平台 IL，模组应支持所有 CPU |
| `binary_format/embed_pck` | `false` | 不嵌入 PCK（模组 PCK 是独立文件） |

---

## 配合 csproj 使用

`export_presets.cfg` 需要搭配 csproj 中的导出 Target：

```xml
<Target Name="GodotPublish" AfterTargets="Publish"
        Condition="'$(GodotPath)' != '' and Exists('$(GodotPath)')">
    <Exec Command="&quot;$(GodotPath)&quot; --headless
            --export-pack &quot;BasicExport&quot;
            &quot;$(ModsPath)$(MSBuildProjectName)/$(MSBuildProjectName).pck&quot;"
          EnvironmentVariables="IsInnerGodotExport=true" />
</Target>
```

> `GodotPath` 在 `Directory.Build.props` 中配置，见 [skeleton-build.md](skeleton-build.md)。

---

## 注意事项

- 预设名 `"BasicExport"` 必须与 `--export-pack "BasicExport"` 参数一致
- `exclude_filter` 不排除 manifest JSON 会导致 Godot 修改后的 JSON 覆盖原文件，游戏识别不到模组
- `binary_format/architecture="msil"` 保证 PCK 兼容 x64/ARM64 平台