using System.Linq;
using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PlantUmlClassDiagramGenerator.Library.ClassDiagramGenerator;

public partial class ClassDiagramGenerator
{
    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        if (attributeRequired && !node.AttributeLists.HasDiagramAttribute()) { return; }
        if (node.AttributeLists.HasIgnoreAttribute()) { return; }
        if (SkipInnerTypeDeclaration(node)) { return; }

        var typeName = TypeNameText.From(node);
        var name = typeName.Identifier;
        var typeParam = typeName.TypeArguments;
        var type = $"{name}{typeParam}";
        var typeParams = typeParam.TrimStart('<').TrimEnd('>').Split([','], StringSplitOptions.RemoveEmptyEntries);

        if (excludedTypePatterns.Any(name.Contains))
            return;

        relationships.AddInnerclassRelationFrom(node);
        relationships.AddInheritanceFrom(node, excludedTypePatterns);
        var modifiers = GetTypeModifiersText(node.Modifiers);
        var abstractKeyword = (node.Modifiers.Any(SyntaxKind.AbstractKeyword) ? "abstract " : "");

        types.Add(name);

        var typeKeyword = (node.Kind() == SyntaxKind.RecordStructDeclaration) ? "struct" : "class";
        WriteLine($"{abstractKeyword}{typeKeyword} {type} {modifiers} {{");

        nestingDepth++;
        var parameters = node.ParameterList?.Parameters ?? Enumerable.Empty<ParameterSyntax>();
        foreach (var parameter in parameters)
        {
            VisitRecordParameter(node, type, typeParams, parameter);
        }
        base.VisitRecordDeclaration(node);
        nestingDepth--;

        WriteLine("}");
    }

    private void VisitRecordParameter(RecordDeclarationSyntax node, string recordType, string[] typeParams, ParameterSyntax parameter)
    {
        var parameterType = parameter.Type;
        TypeSyntax baseParameterType =
            parameterType is NullableTypeSyntax nullableTypeSyntax ? nullableTypeSyntax.ElementType : parameterType;
        baseParameterType =
            baseParameterType is ArrayTypeSyntax arrayTypeSyntax ? arrayTypeSyntax.ElementType : baseParameterType;

        var isTypeParameterProp = typeParams.Contains(parameterType.ToString());
        var associationAttrSyntax = parameter.AttributeLists.GetAssociationAttributeSyntax();
        if (associationAttrSyntax is not null)
        {
            var associationAttr = CreateAssociationAttribute(associationAttrSyntax);
            relationships.AddAssociationFrom(node, parameter, associationAttr);
        }
        else if (!createAssociation
                 || parameter.AttributeLists.HasIgnoreAssociationAttribute()
                 || baseParameterType is PredefinedTypeSyntax
                 || isTypeParameterProp)
        {
            // ParameterList-Property: always public
            var parameterModifiers = "+ ";
            var parameterName = parameter.Identifier.ToString();

            var useLiteralInit = parameter.Default?.Value is not null;
            var initValue = useLiteralInit
                ? (" = " + escapeDictionary.Aggregate(parameter.Default.Value.ToString(),
                    (n, e) => Regex.Replace(n, e.Key, e.Value)))
                : "";
            WriteLine($"{parameterModifiers}{parameterName} : {parameterType} {initValue}");
            relationships.AddAssociationFrom(parameter, node);
        }
        else
        {
            if (recordType.GetType() == typeof(GenericNameSyntax))
            {
                additionalTypeDeclarationNodes.Add(parameterType);
            }
            relationships.AddAssociationFrom(parameter, node);
        }
    }
}