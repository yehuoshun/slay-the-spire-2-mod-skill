# CI 构建：PCK 打包

> 接 [ci-build.md](ci-build.md) 主文。

---

## PCK 打包

两种方式。推荐**优先用 PckPacker**（简资源时），复杂资源用 Godot 导出。

### 方式 A：PckPacker（NuGet 包，单体模组适用）

在 csproj 中加入：

```xml
<ItemGroup>
  <PackageReference Include="BSchneppe.StS2.PckPacker" Version="0.1.1" PrivateAssets="All"/>
</ItemGroup>
<PropertyGroup>
  <PckPackerSourceDir>assets/</PckPackerSourceDir>
  <PckPackerResPrefix>$(AssemblyName)</PckPackerResPrefix>
  <PckPackerOutputPath>$(OutputPath)$(AssemblyName).pck</PckPackerOutputPath>
</PropertyGroup>
<ItemGroup>
  <GodotResourceFiles Include="assets\**"/>
</ItemGroup>
```

构建时自动生成 `.pck`，无需下载 Godot。

### 方式 B：Godot 导出（复杂场景/多模块）

```yaml
      - name: Cache Godot
        id: cache-godot
        uses: actions/cache@v4
        with:
          path: godot-bin
          key: godot-${{ env.GODOT_VERSION }}

      - name: Download Godot
        if: steps.cache-godot.outputs.cache-hit != 'true'
        run: |
          GODOT_URL="https://github.com/godotengine/godot/releases/download/\
            ${{ env.GODOT_VERSION }}/\
            Godot_v${{ env.GODOT_VERSION }}_mono_linux_x86_64.zip"
          curl -sL "$GODOT_URL" -o godot.zip
          unzip -q godot.zip -d godot-bin

      - name: Export .pck
        run: |
          GODOT=$(find godot-bin -name "Godot*" -type f -executable | head -1)
          chmod +x "$GODOT"
          "$GODOT" --headless --export-pack "BasicExport" MyMod.pck
```

> 需要 `export_presets.cfg` 和 `project.godot` 在项目根目录，见 [export-presets.md](export-presets.md) 和 [project-godot.md](project-godot.md)。