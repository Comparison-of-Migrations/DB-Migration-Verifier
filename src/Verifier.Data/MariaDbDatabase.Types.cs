using System.Text.Json;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MariaDbDatabase
{
    private static CatalogSection ReadColumnTypes(IReadOnlyList<string?[]> native,IReadOnlyList<CatalogFact> definitions)
    {
        // These declarations belong to columns. Preserve native member order,
        // charset and collation without parsing, rewriting or executing literals.
        // This section does not establish database-wide plugin/type completeness.
        var facts=new List<CatalogFact>();var gaps=new List<CatalogGap>();
        foreach(var row in native)
        {
            var name=row[0]!;var type=row[1]!;
            if(type is "enum" or "set")facts.Add(new(name,name,"UserTypeDefinitions",JsonSerializer.Serialize(definitions.Where(f=>f.Column==name&&f.Attribute is "ColumnType" or "ColumnCollation").Select(f=>new{f.Attribute,f.Value}))));
            else if(!Supported.Contains(type))gaps.Add(new(name,name,"UserTypeDefinitions","Native type implementation has not been verified.",Reason.NotImplemented));
        }
        return new("OBJECT-03",facts,gaps.Count==0?Reason.None:Reason.NotImplemented){Gaps=gaps,CompleteExceptGaps=gaps.Count>0};
    }
}
