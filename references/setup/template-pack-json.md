# dotnet new 模板：template.json 详解

> 从 template-pack.md 拆出。`template.json` 是模板打包的核心元数据文件，位于每个模板的 `.template.config/` 目录。

## 完整示例

```json
{
  "author": "Alchyr",
  "name": "Slay the Spire 2 Content",
  "description": "A Slay the Spire 2 content mod relying on BaseLib",
  "identity": "Alchyr.Sts2ContentMod",
  "shortName": "alchyrsts2contentmod",
  "tags": { "language": "C#", "type": "project" },
  "sourceName": "ContentMod",
  "symbols": {
    "ModAuthor": {
      "type": "parameter", "dataType": "string",
      "isRequired": "true", "replaces": "{ModAuthor}", "defaultValue": "Author"
    },
    "PublicizeSts": {
      "type": "parameter", "dataType": "bool",
      "defaultValue": "false", "replaces": "{PublicizeSts}",
      "description": "If enabled, non-virtual private and protected symbols in the Slay the Spire 2 .dll will be publicized."
    },
    "NullableChecks": {
      "type": "parameter", "dataType": "choice",
      "choices": [
        { "choice": "enable", "description": "More strict handling of possibly null values." },
        { "choice": "disable", "description": "" }
      ],
      "replaces": "{NullableChecks}", "defaultValue": "enable"
    }
  }
}
```

## 机制要点

- **`sourceName`**：模板内所有文件名/命名空间里的 `ContentMod` 字样，创建时整体替换为项目名
- **`symbols`**：参数化占位。`replaces` 指定替换的占位符，如 `{ModAuthor}` → 用户输入值
- **`dataType: bool`**：生成 `{PublicizeSts}` 为 `true/false`，直接喂给 csproj 的 `<Publicize>` 属性
- **`dataType: choice`**：预置选项（如 Nullable 开关），生成对应值（`enable`/`disable` → 写入 `<Nullable>`）
- 模板可排除文件：`sources[].modifiers` 里 `exclude` 掉 `.git/`、`.godot/`、`README.md` 等

## 打包与使用

```bash
# 打包（模板仓库根目录有 Alchyr.Sts2.Templates.csproj）
dotnet pack -c Release

# 安装到本机
dotnet new install <nupkg路径或目录>

# 生成新模组项目（模板会替换 ContentMod → MyMod 并问 ModAuthor/PublicizeSts/Nullable）
dotnet new alchyrsts2contentmod -n MyMod -o MyMod --ModAuthor "Me" --PublicizeSts false
```
