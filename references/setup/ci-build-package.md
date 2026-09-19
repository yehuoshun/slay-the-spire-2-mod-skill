# CI 构建：打包分发与多模块方案

> 接 [ci-build.md](ci-build.md) 主文。

---

## 打包分发的产物

| 产物 | 内容 | 说明 |
|------|------|------|
| `MyMod.dll` | 编译好的 C# DLL | 放入游戏 `Mods/MyMod/` |
| `MyMod.json` | 模组清单 | 同上 |
| `MyMod.pck` | 资源包（可选） | 有图片/场景等资源时需提供 |

推荐打成 ZIP 发布：

```yaml
      - name: Package
        run: |
          mkdir -p publish/MyMod
          cp MyMod.dll publish/MyMod/
          cp MyMod.json publish/MyMod/
          [ -f MyMod.pck ] && cp MyMod.pck publish/MyMod/
          cd publish && zip -r MyMod.zip MyMod/
```

---

## 多模块方案（参考 sts2-shunmod）

如果模组分多个独立模块（如 Core + 角色 + 修改包），需要**先构建 Core**，其余模块通过 `ProjectReference` 引用 Core 的 DLL：

```yaml
jobs:
  build-core:
    steps:
      - name: Build Core
        run: dotnet build Core/Core.csproj -c Release -p:Sts2DataDir=...

      - name: Upload Core artifact
        uses: actions/upload-artifact@v4
        with:
          name: build-core
          path: Core/bin/Release/Core.dll

  build-modules:
    needs: build-core
    strategy:
      matrix:
        module: [ModA, ModB]
    steps:
      - name: Download Core artifact
        uses: actions/download-artifact@v4
        with:
          name: build-core

      - name: Place Core.dll
        run: |
          mkdir -p ModA/.godot/mono/temp/bin/Release
          cp Core.dll ModA/.godot/mono/temp/bin/Release/

      - name: Build
        run: dotnet build ModA/ModA.csproj -c Release -p:Sts2DataDir=...
```