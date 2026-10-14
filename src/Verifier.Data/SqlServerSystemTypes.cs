namespace Verifier.Data;

internal static class SqlServerSystemTypes
{
    internal static bool Trusted(string name,string schema,bool userDefined,bool assemblyType,string? assembly,bool? assemblyUserDefined,string? managedClass)
        =>schema=="sys"&&!userDefined&&assemblyType&&assembly=="Microsoft.SqlServer.Types"&&assemblyUserDefined==false&&
          managedClass==(name switch{"geometry"=>"Microsoft.SqlServer.Types.SqlGeometry","geography"=>"Microsoft.SqlServer.Types.SqlGeography","hierarchyid"=>"Microsoft.SqlServer.Types.SqlHierarchyId",_=>null})&&managedClass!=null;
}
