# 项目骨架：进阶构建 Target（学自 ModTemplate-StS2）

> 官方模板在基础 csproj 之上加的 5 个 Target + 2 个校验，纯原生同样可用。

## 进阶 Target 清单

```xml
<!-- ① 初始校验：游戏数据目录必须存在，否则构建直接报错提示 -->
<Target Name="CheckDependencyPaths">
    <Error Text="Slay the Spire 2 data not found at '$(Sts2DataDir)'"
           Condition="'$(Sts2DataDir)' == '' or !Exists('$(Sts2DataDir)')" />
</Target>

<!-- ② 构建后自动拷贝 dll/pdb/json 到游戏 mods 目录 -->
<Target Name="CopyToModsFolderOnBuild" AfterTargets="PostBuildEvent">
    <Copy SourceFiles="$(TargetPath)" DestinationFolder="$(ModsPath)$(MSBuildProjectName)/" />
    <Copy SourceFiles="$(AssemblyName).json" DestinationFolder="$(ModsPath)$(MSBuildProjectName)/" />
    <Copy SourceFiles="$(TargetDir)$(TargetName).pdb" DestinationFolder="$(ModsPath)$(MSBuildProjectName)/" />
</Target>

<!-- ③ Publish 前校验 Godot 路径（发布必须能导出 PCK） -->
<Target Name="NeedGodotForPublish" BeforeTargets="Publish">
    <Error Text="Godot path must be set; check Directory.Build.props"
           Condition="'$(GodotPath)' == '' or !Exists('$(GodotPath)')" />
</Target>

<!-- ④ Publish 后自动 export-pack 到 mods 目录 -->
<Target Name="GodotPublish" AfterTargets="Publish"
        Condition="'$(GodotPath)' != '' and Exists('$(GodotPath)') and '$(IsInnerGodotExport)' != 'true'">
    <Exec Command="&quot;$(GodotPath)&quot; --headless --export-pack &quot;BasicExport&quot; &quot;$(ModsPath)$(MSBuildProjectName)/$(MSBuildProjectName).pck&quot;"
          EnvironmentVariables="IsInnerGodotExport=true;MSBUILDDISABLENODEREUSE=1"
          ContinueOnError="WarnAndContinue"/>
</Target>

<!-- ⑤ PckPacker 产出自动拷贝（若启用了 PckPacker 包） -->
<Target Name="CopyQuickPck" AfterTargets="PackPck"
        Condition="Exists('$(PckPackerOutputPath)') And '$(PckPackerSkipped)' != 'true'">
    <Copy SourceFiles="$(PckPackerOutputPath)" DestinationFolder="$(ModsPath)/$(MSBuildProjectName)" />
</Target>
```

## 可选：Publicizer 与静态分析器

```xml
<!-- Publicize：把 sts2.dll 的 private/protected 成员公开（默认关，能不用就不用） -->
<PropertyGroup>
    <Publicize>false</Publicize>
</PropertyGroup>
<ItemGroup Condition="$(Publicize)">
    <PackageReference Include="Krafs.Publicizer" Version="2.3.0" PrivateAssets="All"/>
    <Publicize Include="sts2" IncludeVirtualMembers="false" IncludeCompilerGeneratedMembers="false" />
</ItemGroup>

<!-- ModAnalyzers：官方静态分析器，检查模组 API 误用（推荐开） -->
<ItemGroup>
    <PackageReference Include="Alchyr.Sts2.ModAnalyzers" Version="*" />
    <!-- 让分析器读本地化 JSON，校验 key 是否完整 -->
    <AdditionalFiles Include="ContentMod/localization/**/*.json"/>
</ItemGroup>

<!-- 资源目录排除：ContentMod/ images/ 等只作资源，不参与编译 -->
<ItemGroup>
    <Compile Remove="ContentMod/**" />
    <EmbeddedResource Remove="ContentMod/**" />
    <None Include="ContentMod.json" />
    <None Include="project.godot"/>
    <None Include="ContentMod/**" />
</ItemGroup>
```

## 注意

- ⚠️ ④ 的 `IsInnerGodotExport` 环境变量防递归（Godot 导出时内部再触发 Publish）。纯原生方案把 `BasicExport` 换成自己 `export_presets.cfg` 里的预设名。
- ⚠️ 若 manifest 里的 `dependencies` 引用 BaseLib，官方模板还有 `UpdateDependencyVersions` Target 自动把 `min_version` 同步为 NuGet 实际版本——纯原生无此依赖，不需要。
