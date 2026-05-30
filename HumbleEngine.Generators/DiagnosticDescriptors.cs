using Microsoft.CodeAnalysis;

namespace HumbleEngine.Generators;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor WidgetPropertyMustBeDeclaredInAPartialWidget =
        new(
            id: "HE001",
            title: "Widget property must be declared in a partial Widget",
            messageFormat: "The property '{0}' must be declared inside a 'HumbleEngine.Widget' derived partial record",
            category: "HumbleEngine",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

    public static readonly DiagnosticDescriptor WidgetPropertyMustBePartial =
        new(
            id: "HE002",
            title: "Widget property must be partial",
            messageFormat: "The property '{0}' must be partial in order to use the WidgetPropertyAttribute",
            category: "HumbleEngine",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    
    public static readonly DiagnosticDescriptor WidgetPropertyMustBeAPropertyType =
        new(
            id: "HE003",
            title: "Widget property must be of type Property or ListProperty",
            messageFormat: "The property '{0}' must be of type Property or ListProperty",
            category: "HumbleEngine",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

    public static readonly DiagnosticDescriptor WidgetPropertyMustHaveBothAGetterAndAnInitSetter =
        new(
            id: "HE004",
            title: "Widget property must have both a getter and an init setter",
            messageFormat: "The property '{0}' must have both a getter and an init setter",
            category: "HumbleEngine",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

}