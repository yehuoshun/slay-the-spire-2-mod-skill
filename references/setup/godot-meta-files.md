# Godot 元文件的入库约定

> 学自 [lf201014/STS2_MarisaMod](https://github.com/lf201014/STS2_MarisaMod) 2026-11-14 的提交：新增脚本或图片时，`.cs.uid`、`.png.import` 与源文件在**同一个提交**里一起入库。

---

## 两种伴生文件

| 文件 | 生成者 | 内容 | 作用 |
|------|--------|------|------|
| `<脚本>.cs.uid` | Godot 4.4+ 保存脚本时生成 | 单行 `uid://...` | 脚本的稳定标识，与路径解耦 |
| `<资源>.import` | 导入资源时自动生成 | 纯文本 cfg，约 40 行 | 记录导入设置 |

Godot 4.4 起，脚本保存时旁边会多出一个 `.uid` 伴生文件；C# 脚本对应的是 `<脚本名>.cs.uid`（本 mod 用 Godot 4.5，全仓 168 个 `.cs` 与 168 个 `.cs.uid` 一一配对，实测如此；官方博客的 `.uid` 示例只举了 `.gd`/`.shader`，C# 这一档没有专述）。它是脚本的稳定标识：脚本改名或移动后，场景与资源里指向它的引用仍能解析。

---

## `.cs.uid` 的实际内容

`Scripts\Powers\HeatDeathOfUniversePower.cs` 新增时，同目录同时新增 `HeatDeathOfUniversePower.cs.uid`：

```text
uid://ddqxtokkfwwsq
```

同一次提交还补齐了 4 个原本缺 `.uid` 的旧脚本；到这一版全仓 **168 个 `.cs` 对应 168 个 `.cs.uid`**，一一配对。

---

## `*.import` 的实际内容

新增 `marisamod\images\cards\*.png` 时，每张图旁边同时提交了同名 `.png.import`（约 40 行）。节选：

```ini
importer="texture"
type="CompressedTexture2D"
compress/mode=0
mipmaps=false
```

`.import` 只存**设置**；导入产物（`.ctex` 等二进制）在 `.godot/imported/` 缓存里，那个目录忽略。

---

## 上游的 `.gitignore`

```gitignore
.godot/
/android/
bin/windows/
.idea/
windows/~libspine_godot*.dll
```

要点：**没有 `*.uid`，也没有 `*.import`** —— 两者都在版本库里。模板骨架的忽略清单见 [skeleton-directory.md](skeleton-directory.md) 的 `.gitignore` 一节，不重复。

---

## 结论

| 文件 | 处置 | 理由 |
|------|------|------|
| `*.cs.uid` | 必须入库 | 官方要求；丢了引用会断 |
| `*.import` | 必须入库 | 官方要求；含导入设置等元数据 |
| `project.godot` | 必须入库 | 项目配置本体 |
| `export_presets.cfg` | 必须入库 | 导出 / PCK 打包必需 |
| `.godot/` | 必须忽略 | 纯缓存，可自动重建 |
| `bin/`、`*.dll` 构建产物 | 必须忽略 | 构建重新生成 |

`deps/sts2.dll` 这类**引用库**不是构建产物：本仓库示例工程按项目约定把 `deps/` 入库。

---

## 常见问题

| 现象 | 处置 |
|------|------|
| 删了 `.uid`，Godot 重新生成一个新 uid | 新 uid 与旧引用不匹配，场景 / 资源引用可能解析失败；必须成对提交 |
| 合并冲突落在 `.uid` 上 | 只有一行也**不能任选一方**，否则两个脚本抢同一个 uid；保留与被引用脚本一致的那个，不确定就重新生成并同步修引用 |
| CI 里 `dotnet build` 通过 | 编译不生成也不校验 `.uid`；靠人眼 / 白名单，或在 CI 加一步「每个 `.cs` 必须有同名 `.cs.uid`」的校验 |
| 把 `.godot/imported/` 里的 `.ctex` 也提交 | 只提交伴生 `.import`，二进制产物一律不入库 |

---

## 来源

- [Import process（官方文档）](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/import_process.html)：`<asset>.import` 存导入配置，原文 "Make sure to commit these files to your version control system"；同页指出导入产物是 `.godot/imported/` 里的 `.ctex` 与 `.md5`。
- [Version control systems（官方文档）](https://docs.godotengine.org/en/stable/tutorials/best_practices/version_control_systems.html)：「Files to exclude from VCS」（Godot 4.1+）只列 `.godot/` 与 `*.translation`，不含 `.uid` / `.import`。
- [UID changes coming to Godot 4.4（官方博客）](https://godotengine.org/article/uid-changes-coming-to-godot-4-4/)："make sure `.uid` files are committed to version control"、"`*.uid` should **not** be added to `.gitignore`"。
