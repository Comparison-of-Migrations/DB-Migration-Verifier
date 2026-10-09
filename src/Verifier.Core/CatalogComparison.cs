namespace Verifier.Core;

public static class CatalogComparison
{
    public static readonly IReadOnlyDictionary<string,(string Check,string Korean,string English)> Fields = new Dictionary<string,(string,string,string)>(StringComparer.Ordinal)
    {
        ["TableKind"]=("SCHEMA-01","테이블 종류","Table kind"),["TablePersistence"]=("SCHEMA-01","테이블 지속성","Table persistence"),
        ["ColumnName"]=("SCHEMA-01","컬럼 이름","Column name"),["ColumnType"]=("SCHEMA-01","컬럼 자료형·길이·정밀도","Column type, length and precision"),
        ["ColumnPosition"]=("SCHEMA-01","컬럼 순서","Column order"),["ColumnNullable"]=("SCHEMA-01","NULL 허용","Nullability"),
        ["ColumnDefault"]=("SCHEMA-01","기본값·계산식","Default or generated expression"),["ColumnIdentity"]=("SCHEMA-01","identity 방식","Identity mode"),
        ["ColumnGenerated"]=("SCHEMA-01","계산 컬럼 방식","Generated column mode"),["ColumnCollation"]=("SCHEMA-01","컬럼 collation","Column collation"),
        ["ColumnStorage"]=("SCHEMA-01","컬럼 저장 방식","Column storage"),["ColumnCompression"]=("SCHEMA-01","컬럼 압축","Column compression"),
        ["ColumnOptions"]=("SCHEMA-01","컬럼 옵션","Column options"),
        ["ColumnDimensions"]=("SCHEMA-01","컬럼 선언 배열 차원","Declared column array dimensions"),["ColumnLocal"]=("SCHEMA-01","컬럼 로컬 정의","Local column definition"),["ColumnInherited"]=("SCHEMA-01","컬럼 상속","Column inheritance"),["ColumnStatisticsTarget"]=("SCHEMA-01","컬럼 통계 설정","Column statistics target"),
        ["ConstraintType"]=("SCHEMA-02","제약 종류·유무","Constraint kind or presence"),["ConstraintDefinition"]=("SCHEMA-02","제약 정의·참조·동작","Constraint definition, references and actions"),
        ["ConstraintDeferrable"]=("SCHEMA-02","제약 지연 가능","Constraint deferrability"),["ConstraintDeferred"]=("SCHEMA-02","제약 기본 지연","Initially deferred constraint"),
        ["ConstraintValidated"]=("SCHEMA-02","제약 검증 상태","Constraint validation state"),["ConstraintLocal"]=("SCHEMA-02","제약 로컬 정의","Local constraint definition"),
        ["ConstraintInherited"]=("SCHEMA-02","제약 상속","Constraint inheritance"),["ConstraintNoInherit"]=("SCHEMA-02","제약 상속 금지","Non-inheritable constraint"),
        ["ConstraintParent"]=("SCHEMA-02","제약 부모 연결","Parent constraint link"),
        ["ConstraintOperators"]=("SCHEMA-02","제약 비교·배제 연산자","Constraint comparison and exclusion operators"),
        ["IndexDefinition"]=("SCHEMA-03","인덱스 정의·키·포함·식·조건","Index definition, keys, includes, expressions and predicate"),
        ["IndexUnique"]=("SCHEMA-03","인덱스 유일성","Index uniqueness"),["IndexNulls"]=("SCHEMA-03","인덱스 NULL 동등성","Index null distinctness"),
        ["IndexPrimary"]=("SCHEMA-03","기본 키 인덱스","Primary-key index"),["IndexExclusion"]=("SCHEMA-03","배제 인덱스","Exclusion index"),
        ["IndexImmediate"]=("SCHEMA-03","인덱스 즉시 강제","Immediate index enforcement"),["IndexClustered"]=("SCHEMA-03","인덱스 cluster 상태","Clustered index state"),
        ["IndexValid"]=("SCHEMA-03","인덱스 유효 상태","Index validity"),["IndexReady"]=("SCHEMA-03","인덱스 준비 상태","Index readiness"),
        ["IndexLive"]=("SCHEMA-03","인덱스 생존 상태","Index live state"),["IndexReplicaIdentity"]=("SCHEMA-03","인덱스 복제 식별","Index replica identity"),
        ["IndexOptions"]=("SCHEMA-03","인덱스 옵션","Index options"),["IndexTablespace"]=("SCHEMA-03","인덱스 tablespace","Index tablespace"),
        ["IndexParent"]=("SCHEMA-03","인덱스 부모 연결","Parent index link"),
        ["IndexCheckXmin"]=("SCHEMA-03","인덱스 관측 가시성 플래그","Index observed visibility flag"),["IndexKeyCount"]=("SCHEMA-03","인덱스 키 수","Index key count"),["IndexColumnCount"]=("SCHEMA-03","인덱스 전체 컬럼 수","Index total column count"),["IndexPersistence"]=("SCHEMA-03","인덱스 지속성","Index persistence"),
        ["TableOptions"]=("SCHEMA-03","테이블 옵션","Table options"),["TableAccessMethod"]=("SCHEMA-03","테이블 접근 방식","Table access method"),
        ["TableTablespace"]=("SCHEMA-03","테이블 tablespace","Table tablespace"),["TableReplicaIdentity"]=("SCHEMA-03","테이블 복제 식별","Table replica identity"),
        ["PartitionKey"]=("SCHEMA-03","partition 키·방식","Partition key and strategy"),["PartitionBound"]=("SCHEMA-03","partition 경계","Partition bound"),
        ["PartitionParents"]=("SCHEMA-03","상속·partition 부모 연결","Inheritance and partition parents"),["PartitionDefault"]=("SCHEMA-03","기본 partition 연결","Default partition link")
        ,["PartitionChildren"]=("SCHEMA-03","상속·partition 하위 연결·경계","Inheritance and partition descendant links and bounds")
,["TableOwner"]=("ACCESS-01","테이블 소유자","Table owner")
,["SchemaAccess"]=("ACCESS-01","schema 소유자·권한","Schema ownership and grants")
,["TableAccess"]=("ACCESS-01","테이블 권한·부여 옵션","Table grants and grant options")
,["ColumnAccess"]=("ACCESS-01","컬럼 권한·부여 옵션","Column grants and grant options")
,["RelatedRoles"]=("ACCESS-01","관련 역할 속성·멤버십","Related role attributes and membership")
,["DefaultAccess"]=("ACCESS-01","기본 권한","Default grants")
,["RlsEnabled"]=("ACCESS-02","행 정책 활성","Row security enabled")
,["RlsForced"]=("ACCESS-02","행 정책 강제","Forced row security")
,["RowPolicies"]=("ACCESS-02","행 정책 정의·역할·명령","Policy definitions, roles and commands")
,["TriggerDefinitions"]=("OBJECT-01","trigger 정의·활성·결합","Trigger definitions, state and binding")
,["RelatedRoutines"]=("OBJECT-01","관련 함수·프로시저 정의·문맥","Related routine definitions and context")
,["RelatedViews"]=("OBJECT-01","관련 view 정의·참조","Related view definitions and references")
,["SequenceDefinitions"]=("OBJECT-02","연결된 sequence 정의·소유","Linked sequence definitions and ownership")
,["SequenceState"]=("OBJECT-02","연결된 sequence 관측 상태","Linked sequence observed state")
,["UserTypeDefinitions"]=("OBJECT-03","컬럼 사용자 타입 정의","Column custom type definitions")
    };

    public static (IReadOnlyList<Finding> Findings,Reason Reason) Compare(string check,MatchedScope scope,bool visible)
    {
        var findings=new List<Finding>();var left=scope.Source?.Catalog.Where(s=>s.CheckId==check).ToArray()??[];
        var right=scope.Target?.Catalog.Where(s=>s.CheckId==check).ToArray()??[];
        if(left.Length>1||right.Length>1)return(findings,Reason.AmbiguousScope);
        var reason=!visible?Reason.VisibilityUnknown:left.Length==0||right.Length==0?Reason.NotImplemented:left[0].Reason!=Reason.None?left[0].Reason:right[0].Reason;
        var ambiguous=new HashSet<(string Key,string Attribute)>();
        var a=Build(left.SingleOrDefault(),false);var b=Build(right.SingleOrDefault(),true);
        foreach(var key in a.Keys.Union(b.Keys).OrderBy(k=>k.Key,StringComparer.Ordinal).ThenBy(k=>k.Attribute,StringComparer.Ordinal))
        {
            if(ambiguous.Contains(key))continue;
            a.TryGetValue(key,out var x);b.TryGetValue(key,out var y);
            // Missing sections are an observation limit, never proof of absence.
            if(x==null&&(left.Length==0||left[0].Reason!=Reason.None||!visible)||y==null&&(right.Length==0||right[0].Reason!=Reason.None||!visible))continue;
            if(x==null||y==null||x.Value!=y.Value)findings.Add(new(Guid.NewGuid().ToString("N"),scope.Location,x?.Column??y?.Column,key.Attribute,1));
        }
        return(findings,reason);
        Dictionary<(string Key,string Attribute),CatalogFact> Build(CatalogSection? section,bool target)
        {
            var result=new Dictionary<(string,string),CatalogFact>();if(section==null)return result;
            if(section.Facts==null){reason=Reason.AmbiguousScope;return result;}
            foreach(var fact in section.Facts)
            {
                if(fact==null||string.IsNullOrEmpty(fact.Key)||!Fields.TryGetValue(fact.Attribute,out var field)||field.Check!=check){reason=Reason.NotImplemented;continue;}
                var key=fact.Key;var column=fact.Column;
                if(column!=null)
                {
                    var source=target?scope.Columns.SingleOrDefault(c=>c.Target==column)?.Source:column;
                    key=(source==null?"target-column:":"source-column:")+(source??column);column=source??column;
                }
                if(!result.TryAdd((key,fact.Attribute),fact with { Column=column })){reason=Reason.AmbiguousScope;ambiguous.Add((key,fact.Attribute));}
            }
            return result;
        }
    }
}
