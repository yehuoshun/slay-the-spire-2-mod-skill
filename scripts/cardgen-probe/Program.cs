using System.Reflection;
using System.Runtime.Loader;
using System.Text;

// cardgen 文档核对探针：把已安装 AutoAnthony.dll 的对外 API 面机械 dump 出来，
// 供与 references/cardgen/*.md 里写的签名/版本矩阵逐条对照（diff snapshot 即漂移）。
// 只枚举、不执行：不触发 mod 的任何初始化逻辑。

var gameDir = Arg("--game") ?? Environment.GetEnvironmentVariable("STS2_DATA_DIR")
    ?? @"E:\SteamLibrary\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64";
var anthonyDir = Arg("--anthony") ?? Environment.GetEnvironmentVariable("AUTOANTHONY_DIR")
    ?? @"E:\SteamLibrary\steamapps\workshop\content\2868840\3786611028";
var outPath = Arg("--out");

var modDll = Path.Combine(anthonyDir, "AutoAnthony.dll");
if (!File.Exists(modDll))
{
    Console.Error.WriteLine($"[probe] 找不到 {modDll}");
    Console.Error.WriteLine("[probe] 请用 --anthony <目录> 或环境变量 AUTOANTHONY_DIR 指向已订阅的 AutoAnthony 工坊目录。");
    return 2;
}

AssemblyLoadContext.Default.Resolving += (ctx, name) =>
{
    foreach (var dir in new[] { anthonyDir, gameDir })
    {
        var candidate = Path.Combine(dir, name.Name + ".dll");
        if (File.Exists(candidate)) return ctx.LoadFromAssemblyPath(candidate);
    }
    return null;
};

var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(modDll);
var sb = new StringBuilder();

sb.AppendLine($"# assembly: {asm.GetName().Name}");
Type[] types;
try { types = asm.GetTypes(); }
catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray(); }
sb.AppendLine($"# types: {types.Length}");

// 文档 cardgen-api.md「API 版本矩阵」列的 20 个面 + 1 个无版本号老接口。
string[] faces =
[
    "ComponentApi", "ComponentPackageApi", "ComponentLocalizationApi", "ComponentValuationApi",
    "ComponentKeywordApi", "CardTinkeringApi", "NativeCardDecompositionApi",
    "ExternalComponentCharacterApi", "ComponentPresentationApi", "ComponentRuntimeApi",
    "ComponentKeywordRuntimeApi", "ComponentRunSettingsApi", "ComponentGenerationProgressApi",
    "ComponentTriggerApi", "ComponentSurpriseApi", "AutoAnthonySettingsApi",
    "AutoAnthonyEditorApi", "AutoAnthonyFreeformCardApi", "AutoAnthonyNativeCardApi",
    "AutoAnthonyBalanceAdjustmentApi", "CardNameGenerator",
];

sb.AppendLine();
sb.AppendLine("## ApiVersion matrix");
foreach (var face in faces)
{
    var hit = types.Where(t => t.Name == face && t.IsPublic).ToList();
    if (hit.Count == 0) { sb.AppendLine($"{face}\tMISSING"); continue; }
    foreach (var t in hit)
    {
        var versionField = t.GetField("ApiVersion", BindingFlags.Public | BindingFlags.Static);
        var version = versionField?.IsLiteral == true ? versionField.GetRawConstantValue()?.ToString() : "none";
        sb.AppendLine($"{face}\t{version}\t{t.FullName}");
    }
}

sb.AppendLine();
sb.AppendLine("## Public static members");
foreach (var face in faces)
{
    foreach (var t in types.Where(t => t.Name == face && t.IsPublic))
    {
        sb.AppendLine();
        sb.AppendLine($"### {t.FullName}");
        foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static).OrderBy(f => f.Name))
            sb.AppendLine($"  field {Nice(f.FieldType)} {f.Name}");
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Static).OrderBy(p => p.Name))
            sb.AppendLine($"  prop {Nice(p.PropertyType)} {p.Name}");
        var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)   // 跳过 get_/set_ 访问器
            .OrderBy(m => m.Name);
        foreach (var m in methods)
        {
            var ps = string.Join(", ", m.GetParameters().Select(Param));
            sb.AppendLine($"  method {Nice(m.ReturnType)} {m.Name}({ps})");
        }
    }
}

var text = sb.ToString();
if (outPath is not null)
{
    File.WriteAllText(outPath, text, new UTF8Encoding(false));
    Console.WriteLine($"[probe] 已写入 {outPath}（{text.Length} 字符）");
}
else
{
    Console.Out.Write(text);
}
return 0;

string? Arg(string flag)
{
    var i = Array.IndexOf(args, flag);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static string Param(ParameterInfo p)
{
    var suffix = p.HasDefaultValue ? " = " + (p.DefaultValue is null ? "null" : p.DefaultValue) : "";
    return $"{Nice(p.ParameterType)} {p.Name}{suffix}";
}

static string Nice(Type t)
{
    if (t.IsByRef) return Nice(t.GetElementType()!) + "&";
    if (t.IsArray) return Nice(t.GetElementType()!) + "[]";
    if (!t.IsGenericType) return t.Name;
    var name = t.Name[..t.Name.IndexOf('`')];
    return name + "<" + string.Join(", ", t.GetGenericArguments().Select(Nice)) + ">";
}