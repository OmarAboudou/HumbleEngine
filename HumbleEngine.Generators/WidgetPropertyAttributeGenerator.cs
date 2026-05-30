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
            ctx.AddSource("WidgetPropertyAttribute.g.cs",
                """
                    namespace HumbleEngine;
                    
                    [AttributeUsage(AttributeTargets.Property)]
                    public class WidgetPropertyAttribute(WidgetRefreshFlag flag = WidgetRefreshFlag.NONE) : Attribute
                    {
                        public WidgetRefreshFlag Flag { get; init; } = flag;
                    }
                """
            );
        });

        IncrementalValuesProvider<(PropertyDeclarationSyntax syntax, IPropertySymbol symbol)> propertyProvider 
            = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "HumbleEngine.WidgetPropertyAttribute",
                static (node, _) => node is PropertyDeclarationSyntax,
                static (ctx, _) 
                    => (ctx.TargetNode as PropertyDeclarationSyntax, ctx.TargetSymbol as IPropertySymbol));

        context.RegisterSourceOutput(propertyProvider, static (spc, source) =>
        {
            RecordDeclarationSyntax recordSyntax = source.syntax.Parent as RecordDeclarationSyntax;

            // Validation : doit être dans un partial record
            if (recordSyntax is null || !recordSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    WidgetPropertyMustBeDeclaredInAPartialWidget,
                    source.syntax.GetLocation(),
                    source.symbol.ToDisplayString()
                ));
                return;
            }

            // Validation : property doit être partial
            if (!source.syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    WidgetPropertyMustBePartial,
                    source.syntax.GetLocation(),
                    source.symbol.ToDisplayString()
                ));
                return;
            }
            
            // Validation : doit être Property<T> ou ListProperty<T>
            INamedTypeSymbol typeSymbol = (INamedTypeSymbol)source.symbol.Type;
            if (typeSymbol.Name != "Property" && typeSymbol.Name != "ListProperty")
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    WidgetPropertyMustBeAPropertyType,
                    source.syntax.GetLocation(),
                    source.symbol.ToDisplayString()
                ));
                return;
            }

            // Validation : doit avoir un getter et un setter init
            IMethodSymbol? getMethod = source.symbol.GetMethod;
            IMethodSymbol? setMethod = source.symbol.SetMethod;
            if (getMethod is null || setMethod is null || !setMethod.IsInitOnly)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    WidgetPropertyMustHaveBothAGetterAndAnInitSetter,
                    source.syntax.GetLocation(),
                    source.symbol.ToDisplayString())
                );
                return;
            }
            
            // Récupérer l'expression du flag directement depuis le Syntax Tree
            // pour conserver "WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT" tel quel
            AttributeSyntax? attributeSyntax = source.syntax.AttributeLists
                .SelectMany(al => al.Attributes)
                .First(a => a.Name.ToString().Contains("WidgetProperty"));

            string flagExpression = attributeSyntax.ArgumentList?.Arguments.FirstOrDefault()?.ToString()
                                    ?? "WidgetRefreshFlag.NONE";

            string recordName = recordSyntax.Identifier.Text;
            string propName   = source.symbol.Name;

            spc.AddSource(
                $"{recordName}_{propName}.g.cs",
                GenerateSource(source.syntax, source.symbol, recordSyntax, flagExpression)
            );
        });
        
    }
    
    static string GenerateSource(
        PropertyDeclarationSyntax syntax,
        IPropertySymbol symbol,
        RecordDeclarationSyntax recordSyntax,
        string flagExpression)
    {
        INamedTypeSymbol typeSymbol    = (INamedTypeSymbol)symbol.Type;
        string typeArg       = typeSymbol.TypeArguments[0].ToDisplayString(); // "int"
        bool isList        = typeSymbol.Name == "ListProperty";

        string createMethod  = isList ? "CreateWidgetListProperty" : "CreateWidgetProperty";
        string connectMethod = isList ? "ConnectWidgetListProperty" : "ConnectWidgetProperty";

        string propModifiers = syntax.Modifiers.ToString();           // "internal"
        string propType      = symbol.Type.ToDisplayString();         // "Property<int>"
        string propName      = symbol.Name;                           // "ExampleProperty"
        
        SyntaxList<AccessorDeclarationSyntax>? accessors = syntax.AccessorList?.Accessors;

        string getModifiers  = accessors?
            .FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration))
            ?.Modifiers.ToString() ?? "";

        string initModifiers = accessors?
            .FirstOrDefault(a => a.IsKind(SyntaxKind.InitAccessorDeclaration))
            ?.Modifiers.ToString() ?? "";
            
        string namespaceName = symbol.ContainingType.ContainingNamespace.ToDisplayString();
        string recordMods    = recordSyntax.Modifiers.ToString();     // "public partial"
        string recordName    = recordSyntax.Identifier.Text;
        string? recordAttributes = recordSyntax.TypeParameterList?.ToString();

        return $$"""
                 namespace {{namespaceName}};

                 {{recordMods}} record {{recordName}}{{recordAttributes}}
                 {
                     {{propModifiers}} {{propType}} {{propName}}
                     {
                         {{(getModifiers.Length > 0 ? getModifiers + " " : "")}}get => field ??= {{createMethod}}<{{typeArg}}>(default, {{flagExpression}});
                         {{(initModifiers.Length > 0 ? initModifiers + " " : "")}}init
                         {
                             if (field == value)
                                 return;
                             
                             field = value;
                             {{connectMethod}}(field, {{flagExpression}});
                         }
                     }
                 }
                 """;
    }
}