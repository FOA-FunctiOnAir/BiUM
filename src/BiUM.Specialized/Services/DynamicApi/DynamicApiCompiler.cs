using BiUM.Contract.Models.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public sealed class DynamicApiCompileResult
{
    public bool Success { get; init; }
    public byte[]? AssemblyBytes { get; init; }
    public string? EntryPointTypeName { get; init; }
    public string? AssemblyHash { get; init; }
    public string? Error { get; init; }
}

public sealed class DynamicApiCompileRequest
{
    public required string HandlerSourceCode { get; init; }

    public required string TypeNameSeed { get; init; }

    public bool UsesDynamicTables { get; init; }

    public IReadOnlyList<string> AdditionalSourceFiles { get; init; } = [];
}

public static class DynamicApiCompiler
{
    public static DynamicApiCompileResult Compile(string sourceCode, string typeNameSeed) =>
        Compile(new DynamicApiCompileRequest
        {
            HandlerSourceCode = sourceCode,
            TypeNameSeed = typeNameSeed,
            UsesDynamicTables = false
        });

    public static DynamicApiCompileResult Compile(DynamicApiCompileRequest request)
    {
        var className = $"Handler_{SanitizeTypeSeed(request.TypeNameSeed)}";
        var entryPointTypeName = $"BiUM.DynamicApi.Generated.{className}";
        var handlerSource = BuildHandlerSource(className, request.HandlerSourceCode, request.UsesDynamicTables);
        var syntaxTrees = new List<SyntaxTree> { CSharpSyntaxTree.ParseText(handlerSource) };

        foreach (var additionalSource in request.AdditionalSourceFiles)
        {
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(additionalSource));
        }

        var references = CollectReferences();
        var compilation = CSharpCompilation.Create(
            assemblyName: $"DynamicApi_{Guid.NewGuid():N}",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithOptimizationLevel(OptimizationLevel.Release));

        using var ms = new MemoryStream();
        var emitResult = compilation.Emit(ms);

        if (!emitResult.Success)
        {
            var errors = emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString());

            return new DynamicApiCompileResult
            {
                Success = false,
                Error = string.Join(Environment.NewLine, errors)
            };
        }

        var bytes = ms.ToArray();

        return new DynamicApiCompileResult
        {
            Success = true,
            AssemblyBytes = bytes,
            EntryPointTypeName = entryPointTypeName,
            AssemblyHash = Convert.ToHexString(SHA256.HashData(bytes))
        };
    }

    private static string SanitizeTypeSeed(string seed)
    {
        var sb = new StringBuilder(seed.Length);

        foreach (var ch in seed)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else
            {
                sb.Append('_');
            }
        }

        return sb.Length > 0 ? sb.ToString() : "Handler";
    }

    private static string BuildHandlerSource(string className, string userSource, bool usesDynamicTables)
    {
        var preamble = usesDynamicTables
            ? """
                await using var __dynamicTables = DynamicTableDbContextFactory.Create(ctx);
            """
            : string.Empty;

        return $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            using BiUM.Contract.Models.Api;
            using BiUM.Specialized.Database;
            using BiUM.Specialized.Services.DynamicApi;
            using Microsoft.EntityFrameworkCore;

            namespace BiUM.DynamicApi.Generated;

            public sealed class {{className}} : IDynamicApiHandler
            {
                public async Task<object> ExecuteAsync(IDynamicApiExecutionContext ctx, CancellationToken cancellationToken)
                {
                    {{preamble}}
                    {{userSource}}
                }
            }
            """;
    }

    private static IEnumerable<MetadataReference> CollectReferences()
    {
        var assemblies = new HashSet<Assembly>(AppDomain.CurrentDomain.GetAssemblies())
        {
            typeof(object).Assembly,
            typeof(Task).Assembly,
            typeof(ApiResponse).Assembly,
            typeof(IDynamicApiHandler).Assembly,
            typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly,
            typeof(System.Linq.Enumerable).Assembly,
            typeof(DynamicApiDbContextOptions).Assembly,
            typeof(System.ComponentModel.DataAnnotations.Schema.TableAttribute).Assembly,
            typeof(Microsoft.EntityFrameworkCore.RelationalEntityTypeBuilderExtensions).Assembly,
            typeof(Microsoft.EntityFrameworkCore.SqliteDbContextOptionsBuilderExtensions).Assembly
        };

        foreach (var assembly in assemblies)
        {
            if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
            {
                continue;
            }

            yield return MetadataReference.CreateFromFile(assembly.Location);
        }
    }
}