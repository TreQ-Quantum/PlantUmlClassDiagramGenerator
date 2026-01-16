using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Xml.Linq;

namespace PlantUmlClassDiagramGenerator.Library.ClassDiagramGenerator;

public partial class ClassDiagramGenerator
{
    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        if (attributeRequired && !node.AttributeLists.HasDiagramAttribute()) { return; }
        if (node.AttributeLists.HasIgnoreAttribute()) { return; }
        if (SkipInnerTypeDeclaration(node)) { return; }

        relationships.AddInnerclassRelationFrom(node);

        var type = $"{node.Identifier}";

        if (excludedTypePatterns.Any(type.Contains))
            return;

        types.Add(type);

        WriteLine($"{node.EnumKeyword} {type} {{");

        nestingDepth++;
        base.VisitEnumDeclaration(node);
        nestingDepth--;

        WriteLine("}");
    }
    
    public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
    {
        if (excludedMemberPatterns.Any(node.Identifier.Text.Contains))
            return;
        WriteLine($"{node.Identifier}{node.EqualsValue},");
    }
}