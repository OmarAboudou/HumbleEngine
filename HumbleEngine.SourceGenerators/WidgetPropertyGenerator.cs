using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text;

namespace HumbleEngine.SourceGenerators;

file static class Formats
{
    public static readonly SymbolDisplayFormat FullyQualifiedNullable =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
}

[Generator]
public sealed class WidgetPropertyGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor NotAWidget = new(
        id: "HE001",
        title: "[WidgetProperty] utilisé hors d'un Widget",
        messageFormat: "'{0}' n'hérite pas de HumbleEngine.Core.Widget. [WidgetProperty] ne peut être utilisé que dans un Widget.",
        category: "HumbleEngine",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var results = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "HumbleEngine.Core.WidgetPropertyAttribute",
                predicate: static (node, _) => node is PropertyDeclarationSyntax,
                transform: static (ctx, _) => GetResult(ctx));

        context.RegisterSourceOutput(results, static (ctx, result) =>
        {
            if (result.Diagnostic is not null)
                ctx.ReportDiagnostic(result.Diagnostic);
            else if (result.Info is not null)
                Generate(ctx, result.Info);
        });
    }

    private static GeneratorResult GetResult(GeneratorAttributeSyntaxContext ctx)
    {
        var propertySymbol = ctx.TargetSymbol as IPropertySymbol;
        if (propertySymbol is null) return GeneratorResult.Empty;

        if (!InheritsFromWidget(propertySymbol.ContainingType))
        {
            return GeneratorResult.WithError(Diagnostic.Create(
                NotAWidget,
                ctx.TargetNode.GetLocation(),
                propertySymbol.ContainingType.Name));
        }

        string propertyAccessibilityModifier = propertySymbol.DeclaredAccessibility switch
        {
            Accessibility.NotApplicable => "public",
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "public",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "public",
            Accessibility.Public => "public",
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var returnType = propertySymbol.Type as INamedTypeSymbol;
        if (returnType is null || returnType.TypeArguments.Length == 0) return GeneratorResult.Empty;

        string outerName = returnType.OriginalDefinition.Name;
        bool isList = outerName == "ListProperty";
        if (!isList && outerName != "Property") return GeneratorResult.Empty;

        var attribute = ctx.Attributes[0];
        int flagValue = attribute.ConstructorArguments.Length > 0
            ? (int)(attribute.ConstructorArguments[0].Value ?? 0)
            : 0;

        string innerType = returnType.TypeArguments[0]
            .ToDisplayString(Formats.FullyQualifiedNullable);
        string propertyType = returnType
            .ToDisplayString(Formats.FullyQualifiedNullable);

        var containingType = propertySymbol.ContainingType;

        var info = new PropertyInfo(
            Namespace: containingType.ContainingNamespace.ToDisplayString(),
            ContainingTypeName: containingType.Name,
            TypeDeclaration: BuildTypeDeclaration(containingType),
            PropertyName: propertySymbol.Name,
            PropertyType: propertyType,
            InnerType: innerType,
            PropertyAccessibilityModifier: propertyAccessibilityModifier,
            IsList: isList,
            FlagValue: flagValue);

        return GeneratorResult.WithInfo(info);
    }

    private static bool InheritsFromWidget(INamedTypeSymbol type)
    {
        var current = type;
        while (current is not null)
        {
            if (current.Name == "Widget" &&
                current.ContainingNamespace.ToDisplayString() == "HumbleEngine.Core")
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static string BuildTypeDeclaration(INamedTypeSymbol type)
    {
        var sb = new StringBuilder();

        sb.Append(type.DeclaredAccessibility switch
        {
            Accessibility.Public => "public ",
            Accessibility.Internal => "internal ",
            Accessibility.Private => "private ",
            _ => ""
        });

        if (type.IsAbstract && !type.IsSealed) sb.Append("abstract ");
        if (type.IsSealed && !type.IsAbstract) sb.Append("sealed ");
        sb.Append("partial ");

        if (type.IsRecord)
        {
            sb.Append("record ");
            if (type.IsValueType) sb.Append("struct ");
        }
        else if (type.IsValueType) sb.Append("struct ");
        else sb.Append("class ");

        sb.Append(type.Name);

        if (type.TypeParameters.Length > 0)
        {
            sb.Append('<');
            for (int i = 0; i < type.TypeParameters.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(type.TypeParameters[i].Name);
            }
            sb.Append('>');
        }

        foreach (var tp in type.TypeParameters)
        {
            var constraints = new List<string>();
            if (tp.HasReferenceTypeConstraint) constraints.Add("class");
            if (tp.HasValueTypeConstraint) constraints.Add("struct");
            if (tp.HasNotNullConstraint) constraints.Add("notnull");
            foreach (var ct in tp.ConstraintTypes)
                constraints.Add(ct.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            if (tp.HasConstructorConstraint) constraints.Add("new()");

            if (constraints.Count > 0)
                sb.Append($" where {tp.Name} : {string.Join(", ", constraints)}");
        }

        return sb.ToString();
    }

    private static void Generate(SourceProductionContext ctx, PropertyInfo info)
    {
        string flag = $"(global::HumbleEngine.Core.WidgetRefreshFlag){info.FlagValue}";

        string getter = info.IsList
            ? $"field ??= CreatePublicListProperty<{info.InnerType}>(flag: {flag})"
            : $"field ??= CreatePublicProperty<{info.InnerType}>(default!, {flag})";

        string source = $$"""
            #nullable enable
            namespace {{info.Namespace}};

            {{info.TypeDeclaration}}
            {
                {{info.PropertyAccessibilityModifier}} partial {{info.PropertyType}} {{info.PropertyName}}
                {
                    get => {{getter}};
                    init => field = ConnectFlag({{flag}}, value);
                }
            }
            """;

        ctx.AddSource($"{info.ContainingTypeName}_{info.PropertyName}.g.cs", source);
    }

    private sealed record GeneratorResult(PropertyInfo? Info, Diagnostic? Diagnostic)
    {
        public static readonly GeneratorResult Empty = new(null, null);
        public static GeneratorResult WithInfo(PropertyInfo info) => new(info, null);
        public static GeneratorResult WithError(Diagnostic d) => new(null, d);
    }

    private sealed record PropertyInfo(
        string Namespace,
        string ContainingTypeName,
        string TypeDeclaration,
        string PropertyName,
        string PropertyType,
        string InnerType,
        string PropertyAccessibilityModifier,
        bool IsList,
        int FlagValue);
}
