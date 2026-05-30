using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HumbleEngine.Generators.DiagnosticDescriptors;

namespace HumbleEngine.Generators;

[Generator]
public class WidgetPropertyAttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("PrimitiveWidgetPropertyAttribute.g.cs",
                """
                    namespace HumbleEngine;

                    [AttributeUsage(AttributeTargets.Property)]
                    public class PrimitiveWidgetPropertyAttribute(WidgetRefreshFlag flag = WidgetRefreshFlag.NONE) : Attribute
                    {
                        public WidgetRefreshFlag Flag { get; init; } = flag;
                    }
                """);

            ctx.AddSource("CompositeWidgetPropertyAttribute.g.cs",
                """
                    namespace HumbleEngine;

                    [AttributeUsage(AttributeTargets.Property)]
                    public class CompositeWidgetPropertyAttribute : Attribute { }
                """);
        });

        // --- PrimitiveWidgetProperty ---

        IncrementalValuesProvider<(PropertyDeclarationSyntax syntax, IPropertySymbol symbol)> primitiveProvider
            = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "HumbleEngine.PrimitiveWidgetPropertyAttribute",
                    static (node, _) => node is PropertyDeclarationSyntax,
                    static (ctx, _)
                        => (ctx.TargetNode as PropertyDeclarationSyntax, ctx.TargetSymbol as IPropertySymbol));

        context.RegisterSourceOutput(primitiveProvider, static (spc, source) =>
        {
            RecordDeclarationSyntax? recordSyntax = source.syntax.Parent as RecordDeclarationSyntax;

            if (!Validate(spc, source.syntax, source.symbol, recordSyntax)) return;

            AttributeSyntax? attributeSyntax = source.syntax.AttributeLists
                .SelectMany(al => al.Attributes)
                .First(a => a.Name.ToString().Contains("PrimitiveWidgetProperty"));

            string flagExpression = attributeSyntax.ArgumentList?.Arguments.FirstOrDefault()?.ToString()
                                    ?? "WidgetRefreshFlag.NONE";

            spc.AddSource(
                $"{recordSyntax!.Identifier.Text}_{source.symbol.Name}.g.cs",
                GeneratePrimitiveSource(source.syntax, source.symbol, recordSyntax, flagExpression)
            );
        });

        // --- CompositeWidgetProperty ---

        IncrementalValuesProvider<(PropertyDeclarationSyntax syntax, IPropertySymbol symbol)> compositeProvider
            = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "HumbleEngine.CompositeWidgetPropertyAttribute",
                    static (node, _) => node is PropertyDeclarationSyntax,
                    static (ctx, _)
                        => (ctx.TargetNode as PropertyDeclarationSyntax, ctx.TargetSymbol as IPropertySymbol));

        context.RegisterSourceOutput(compositeProvider, static (spc, source) =>
        {
            RecordDeclarationSyntax? recordSyntax = source.syntax.Parent as RecordDeclarationSyntax;

            if (!Validate(spc, source.syntax, source.symbol, recordSyntax)) return;

            spc.AddSource(
                $"{recordSyntax!.Identifier.Text}_{source.symbol.Name}.g.cs",
                GenerateCompositeSource(source.syntax, source.symbol, recordSyntax)
            );
        });
    }

    static bool Validate(
        SourceProductionContext spc,
        PropertyDeclarationSyntax syntax,
        IPropertySymbol symbol,
        RecordDeclarationSyntax? recordSyntax)
    {
        if (recordSyntax is null || !recordSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                WidgetPropertyMustBeDeclaredInAPartialWidget,
                syntax.GetLocation(),
                symbol.ToDisplayString()
            ));
            return false;
        }

        if (!syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                WidgetPropertyMustBePartial,
                syntax.GetLocation(),
                symbol.ToDisplayString()
            ));
            return false;
        }

        INamedTypeSymbol typeSymbol = (INamedTypeSymbol)symbol.Type;
        if (typeSymbol.Name != "Property" && typeSymbol.Name != "ListProperty")
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                WidgetPropertyMustBeAPropertyType,
                syntax.GetLocation(),
                symbol.ToDisplayString()
            ));
            return false;
        }

        IMethodSymbol? getMethod = symbol.GetMethod;
        IMethodSymbol? setMethod = symbol.SetMethod;
        if (getMethod is null || setMethod is null || !setMethod.IsInitOnly)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                WidgetPropertyMustHaveBothAGetterAndAnInitSetter,
                syntax.GetLocation(),
                symbol.ToDisplayString()
            ));
            return false;
        }

        return true;
    }

    static string GeneratePrimitiveSource(
        PropertyDeclarationSyntax syntax,
        IPropertySymbol symbol,
        RecordDeclarationSyntax recordSyntax,
        string flagExpression)
    {
        INamedTypeSymbol typeSymbol  = (INamedTypeSymbol)symbol.Type;
        string typeArg               = typeSymbol.TypeArguments[0].ToDisplayString();
        bool isList                  = typeSymbol.Name == "ListProperty";
        string createMethod          = isList ? "CreateWidgetListProperty" : "CreateWidgetProperty";
        string connectMethod         = isList ? "ConnectWidgetListProperty" : "ConnectWidgetProperty";

        return BuildSource(
            syntax, symbol, recordSyntax,
            getter:   $"field ??= {createMethod}<{typeArg}>(default, {flagExpression})",
            initLine: $"{connectMethod}(field, {flagExpression})"
        );
    }

    static string GenerateCompositeSource(
        PropertyDeclarationSyntax syntax,
        IPropertySymbol symbol,
        RecordDeclarationSyntax recordSyntax)
    {
        INamedTypeSymbol typeSymbol  = (INamedTypeSymbol)symbol.Type;
        string typeArg               = typeSymbol.TypeArguments[0].ToDisplayString();
        bool isList                  = typeSymbol.Name == "ListProperty";
        string createMethod          = isList ? "CreateCompositeListProperty" : "CreateCompositeProperty";
        string connectMethod         = isList ? "ConnectCompositeListProperty" : "ConnectCompositeProperty";

        string getter = isList
            ? $"field ??= {createMethod}<{typeArg}>()"
            : $"field ??= {createMethod}<{typeArg}>(default)";

        return BuildSource(
            syntax, symbol, recordSyntax,
            getter:   getter,
            initLine: $"{connectMethod}(field)"
        );
    }

    static string BuildSource(
        PropertyDeclarationSyntax syntax,
        IPropertySymbol symbol,
        RecordDeclarationSyntax recordSyntax,
        string getter,
        string initLine)
    {
        string propModifiers     = syntax.Modifiers.ToString();
        string propType          = symbol.Type.ToDisplayString();
        string propName          = symbol.Name;

        SyntaxList<AccessorDeclarationSyntax>? accessors = syntax.AccessorList?.Accessors;
        string getModifiers  = accessors?
            .FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration))
            ?.Modifiers.ToString() ?? "";
        string initModifiers = accessors?
            .FirstOrDefault(a => a.IsKind(SyntaxKind.InitAccessorDeclaration))
            ?.Modifiers.ToString() ?? "";

        string namespaceName     = symbol.ContainingType.ContainingNamespace.ToDisplayString();
        string recordMods        = recordSyntax.Modifiers.ToString();
        string recordName        = recordSyntax.Identifier.Text;
        string? recordTypeParams = recordSyntax.TypeParameterList?.ToString();

        return $$"""
                 #nullable enable

                 namespace {{namespaceName}};

                 {{recordMods}} record {{recordName}}{{recordTypeParams}}
                 {
                     {{propModifiers}} {{propType}} {{propName}}
                     {
                         {{(getModifiers.Length > 0 ? getModifiers + " " : "")}}get => {{getter}};
                         {{(initModifiers.Length > 0 ? initModifiers + " " : "")}}init
                         {
                             if (field == value)
                                 return;

                             field = value;
                             {{initLine}};
                         }
                     }
                 }
                 """;
    }
}