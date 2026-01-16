using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PlantUmlClassDiagramGenerator.Library.ClassDiagramGenerator;

public partial class ClassDiagramGenerator
{
    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (node.AttributeLists.HasIgnoreAttribute()) { return; }
        if (IsIgnoreMember(node.Modifiers)) { return; }

        bool isInterfaceMember = node.Parent.IsKind(SyntaxKind.InterfaceDeclaration);
        if (!IsPublicMember(node.Modifiers, isInterfaceMember))
            return;

        var name = node.Identifier.ToString();
        if (excludedMemberPatterns.Any(name.Contains))
            return;

        var propertyType = node.Type;

        var parentClass = (node.Parent as TypeDeclarationSyntax);
        var isTypeParameterProp = parentClass?.TypeParameterList?.Parameters
            .Any(t => t.Identifier.Text == propertyType.ToString()) ?? false;

        TypeSyntax basePropertyType =
            propertyType is NullableTypeSyntax nullableTypeSyntax ? nullableTypeSyntax.ElementType : propertyType;
        basePropertyType =
            basePropertyType is ArrayTypeSyntax arrayTypeSyntax ? arrayTypeSyntax.ElementType : basePropertyType;

        var associationAttrSyntax = node.AttributeLists.GetAssociationAttributeSyntax();
        if (associationAttrSyntax is not null)
        {
            var associationAttr = CreateAssociationAttribute(associationAttrSyntax);
            relationships.AddAssociationFrom(node, associationAttr);
        }
        else if (!createAssociation
            || node.AttributeLists.HasIgnoreAssociationAttribute()
            || basePropertyType is PredefinedTypeSyntax
            || isTypeParameterProp)
        {
            var modifiers = GetMemberModifiersText(node.Modifiers, isInterfaceMember);
            //Property does not have an accessor is an expression-bodied property. (get only)
            var useLiteralInit = node.Initializer?.Value?.Kind().ToString().EndsWith("LiteralExpression") ?? false;
            var initValue = useLiteralInit
                ? (" = " + escapeDictionary.Aggregate(node.Initializer.Value.ToString(),
                    (n, e) => Regex.Replace(n, e.Key, e.Value)))
                : "";

            WriteLine($"{modifiers}{name} : {propertyType} {initValue}");
            relationships.AddAssociationFrom(node, basePropertyType);
        }
        else
        {
            if (propertyType.GetType() == typeof(GenericNameSyntax))
            {
                additionalTypeDeclarationNodes.Add(propertyType);
            }
            relationships.AddAssociationFrom(node, basePropertyType);
        }
    }
}