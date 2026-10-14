namespace Verifier.Core;

public static partial class CatalogComparison
{
    public static readonly IReadOnlyDictionary<string,(string Check,string Korean,string English)> Fields = new Dictionary<string,(string,string,string)>(StringComparer.Ordinal)
    {
        ["SchemaPresence"]=("SCHEMA-01","스키마 존재","Schema presence"),
        ["ExtensionDefinition"]=("OBJECT-03","확장 정의·버전·설치 위치","Extension definition, version and schema"),
        ["ForeignDefinition"]=("OBJECT-03","외부 객체 정의·연결 속성","Foreign object definitions and connection attributes"),
        ["ForeignUserMapping"]=("OBJECT-03","외부 서버 사용자 매핑","Foreign server user mappings"),
        ["ForeignOptionsPresent"]=("OBJECT-03","외부 연결 옵션 유무","Foreign connection option presence"),
        ["UninspectedForeignOptions"]=("OBJECT-03","값을 수집하지 않은 외부 연결 옵션","Foreign connection options without collected values"),
        ["ForeignAccess"]=("ACCESS-01","외부 서버·래퍼 소유자·권한","Foreign server and wrapper ownership and grants"),
        ["TableKind"]=("SCHEMA-01","테이블 종류","Table kind"),["TablePersistence"]=("SCHEMA-01","테이블 지속성","Table persistence"),
        ["ColumnName"]=("SCHEMA-01","컬럼 이름","Column name"),["ColumnType"]=("SCHEMA-01","컬럼 자료형·길이·정밀도","Column type, length and precision"),
        ["ColumnPosition"]=("SCHEMA-01","컬럼 순서","Column order"),["ColumnNullable"]=("SCHEMA-01","NULL 허용","Nullability"),
        ["ColumnDefault"]=("SCHEMA-01","기본값·계산식","Default or generated expression"),["ColumnIdentity"]=("SCHEMA-01","identity 방식","Identity mode"),
        ["ColumnGenerated"]=("SCHEMA-01","계산 컬럼 방식","Generated column mode"),["ColumnCollation"]=("SCHEMA-01","컬럼 collation","Column collation"),
        ["ColumnStorage"]=("SCHEMA-01","컬럼 저장 방식","Column storage"),["ColumnCompression"]=("SCHEMA-01","컬럼 압축","Column compression"),
        ["ColumnOptions"]=("SCHEMA-01","컬럼 옵션","Column options"),
        ["ColumnComment"]=("SCHEMA-01","컬럼 설명","Column comment"),
        ["DatabaseDefinition"]=("SCHEMA-01","DB 문자셋·정렬·암호화 정의","Database character set, collation and encryption definition"),
        ["ColumnDimensions"]=("SCHEMA-01","컬럼 선언 배열 차원","Declared column array dimensions"),["ColumnLocal"]=("SCHEMA-01","컬럼 로컬 정의","Local column definition"),["ColumnInherited"]=("SCHEMA-01","컬럼 상속","Column inheritance"),["ColumnStatisticsTarget"]=("SCHEMA-01","컬럼 통계 설정","Column statistics target"),
        ["ConstraintType"]=("SCHEMA-02","제약 종류·유무","Constraint kind or presence"),["ConstraintDefinition"]=("SCHEMA-02","제약 정의·참조·동작","Constraint definition, references and actions"),
        ["PeriodDefinition"]=("SCHEMA-02","기간 이름·시작·끝 컬럼","Application-time period name and endpoints"),
        ["SystemTimePeriod"]=("SCHEMA-02","시스템 이력 기간·컬럼·자료형","System-history period columns and types"),
        ["ConstraintPeriod"]=("SCHEMA-02","기간 중첩 금지 키 결합","WITHOUT OVERLAPS period key binding"),
        ["ConstraintDeferrable"]=("SCHEMA-02","제약 지연 가능","Constraint deferrability"),["ConstraintDeferred"]=("SCHEMA-02","제약 기본 지연","Initially deferred constraint"),
        ["ConstraintValidated"]=("SCHEMA-02","제약 검증 상태","Constraint validation state"),["ConstraintLocal"]=("SCHEMA-02","제약 로컬 정의","Local constraint definition"),
        ["ConstraintInherited"]=("SCHEMA-02","제약 상속","Constraint inheritance"),["ConstraintNoInherit"]=("SCHEMA-02","제약 상속 금지","Non-inheritable constraint"),
        ["ConstraintParent"]=("SCHEMA-02","제약 부모 연결","Parent constraint link"),
        ["ConstraintEnforced"]=("SCHEMA-02","제약 강제 여부","Constraint enforcement"),
        ["ConstraintOperators"]=("SCHEMA-02","제약 비교·배제 연산자","Constraint comparison and exclusion operators"),
        ["IndexDefinition"]=("SCHEMA-03","인덱스 정의·키·포함·식·조건","Index definition, keys, includes, expressions and predicate"),
        ["IndexUnique"]=("SCHEMA-03","인덱스 유일성","Index uniqueness"),["IndexNulls"]=("SCHEMA-03","인덱스 NULL 동등성","Index null distinctness"),
        ["IndexPrimary"]=("SCHEMA-03","기본 키 인덱스","Primary-key index"),["IndexExclusion"]=("SCHEMA-03","배제 인덱스","Exclusion index"),
        ["IndexImmediate"]=("SCHEMA-03","인덱스 즉시 강제","Immediate index enforcement"),["IndexClustered"]=("SCHEMA-03","인덱스 cluster 상태","Clustered index state"),
        ["IndexValid"]=("SCHEMA-03","인덱스 유효 상태","Index validity"),["IndexReady"]=("SCHEMA-03","인덱스 준비 상태","Index readiness"),
        ["IndexLive"]=("SCHEMA-03","인덱스 생존 상태","Index live state"),["IndexReplicaIdentity"]=("SCHEMA-03","인덱스 복제 식별","Index replica identity"),
        ["IndexOptions"]=("SCHEMA-03","인덱스 옵션","Index options"),["IndexTablespace"]=("SCHEMA-03","인덱스 tablespace","Index tablespace"),
        ["IndexVisible"]=("SCHEMA-03","옵티마이저 인덱스 가시성","Optimizer index visibility"),
        ["TableCollation"]=("SCHEMA-03","테이블 기본 collation","Table default collation"),
        ["TableComment"]=("SCHEMA-03","테이블 설명","Table comment"),
        ["IndexParent"]=("SCHEMA-03","인덱스 부모 연결","Parent index link"),
        ["IndexCheckXmin"]=("SCHEMA-03","인덱스 관측 가시성 플래그","Index observed visibility flag"),["IndexKeyCount"]=("SCHEMA-03","인덱스 키 수","Index key count"),["IndexColumnCount"]=("SCHEMA-03","인덱스 전체 컬럼 수","Index total column count"),["IndexPersistence"]=("SCHEMA-03","인덱스 지속성","Index persistence"),
        ["TableOptions"]=("SCHEMA-03","테이블 옵션","Table options"),["TableAccessMethod"]=("SCHEMA-03","테이블 접근 방식","Table access method"),
        ["TableTablespace"]=("SCHEMA-03","테이블 tablespace","Table tablespace"),["TableReplicaIdentity"]=("SCHEMA-03","테이블 복제 식별","Table replica identity"),
        ["PartitionKey"]=("SCHEMA-03","partition 키·방식","Partition key and strategy"),["PartitionBound"]=("SCHEMA-03","partition 경계","Partition bound"),
        ["PartitionParents"]=("SCHEMA-03","상속·partition 부모 연결","Inheritance and partition parents"),["PartitionDefault"]=("SCHEMA-03","기본 partition 연결","Default partition link")
        ,["PartitionChildren"]=("SCHEMA-03","상속·partition 하위 연결·경계","Inheritance and partition descendant links and bounds")
,["TableOwner"]=("ACCESS-01","테이블 소유자","Table owner")
,["DefinerAccess"]=("ACCESS-01","객체 definer 계정·역할 연결","Object definer account and role bindings")
,["DatabaseAccess"]=("ACCESS-01","DB 소유자·권한","Database ownership and grants")
,["SchemaAccess"]=("ACCESS-01","schema 소유자·권한","Schema ownership and grants")
,["SchemaGrantPrecedence"]=("ACCESS-01","겹치는 DB 권한 규칙의 적용 순서","Overlapping database grant resolution")
,["TableAccess"]=("ACCESS-01","테이블 권한·부여 옵션","Table grants and grant options")
,["ColumnAccess"]=("ACCESS-01","컬럼 권한·부여 옵션","Column grants and grant options")
,["DatabaseColumnAccess"]=("ACCESS-01","DB 범위 컬럼 권한·부여 옵션","Database-scope column grants and grant options")
,["RoutineAccess"]=("ACCESS-01","함수·프로시저 권한·부여 옵션","Routine grants and grant options")
,["RelatedRoles"]=("ACCESS-01","관련 역할 속성·멤버십","Related role attributes and membership")
,["RoleConfiguration"]=("ACCESS-01","DB·역할 접속 기본 설정","Database and role connection defaults")
,["UninspectedConfiguration"]=("ACCESS-01","값을 수집하지 않은 추가 설정","Additional settings without collected values")
,["DefaultAccess"]=("ACCESS-01","기본 권한","Default grants")
,["RlsEnabled"]=("ACCESS-02","행 정책 활성","Row security enabled")
,["RlsForced"]=("ACCESS-02","행 정책 강제","Forced row security")
,["RowPolicies"]=("ACCESS-02","행 정책 정의·역할·명령","Policy definitions, roles and commands")
,["TriggerDefinitions"]=("OBJECT-01","trigger 정의·활성·결합","Trigger definitions, state and binding")
,["RelatedRoutines"]=("OBJECT-01","관련 함수·프로시저 정의·문맥","Related routine definitions and context")
,["RelatedViews"]=("OBJECT-01","관련 view 정의·참조","Related view definitions and references")
,["ViewDefinition"]=("OBJECT-01","view 본문 정의","View body definition")
,["ViewSignature"]=("OBJECT-01","view 반환 컬럼 정의","View result column definitions")
,["ViewUpdatability"]=("OBJECT-01","view 갱신 가능 상태","View updatability state")
,["ViewDependencies"]=("OBJECT-01","view의 native 참조 연결","Native view dependency links")
,["EventDefinitions"]=("OBJECT-01","예약 이벤트 정의·일정·상태","Scheduled event definitions, schedule and state")
,["ObjectDependencies"]=("OBJECT-01","객체 내부의 미확인 의존 관계","Uninspected dependencies inside objects")
,["SequenceDefinitions"]=("OBJECT-02","연결된 sequence 정의·소유","Linked sequence definitions and ownership")
,["SequenceState"]=("OBJECT-02","연결된 sequence 관측 상태","Linked sequence observed state")
,["SequenceReadLockAccess"]=("OBJECT-02","sequence 읽기 잠금 권한(SELECT·LOCK TABLES)","Sequence read-lock access (SELECT and LOCK TABLES)")
,["SequenceCachedReservation"]=("OBJECT-02","sequence 캐시 내 예약 번호 미관측","Unobserved reserved values inside sequence cache")
,["AutoIncrementDefinition"]=("OBJECT-02","자동 증가 컬럼 정의","Auto-increment column definition")
,["AutoIncrementState"]=("OBJECT-02","자동 증가 다음 번호 관측 상태","Observed auto-increment next counter")
,["SpatialReferenceDefinition"]=("OBJECT-03","공간 좌표계 정의·설명·결합","Spatial reference definition, description and binding")
,["UserTypeDefinitions"]=("OBJECT-03","컬럼 사용자 타입 정의","Column custom type definitions")
    };

    public static readonly string[] DatabaseChecks=["SCHEMA-01","ACCESS-01","OBJECT-01","OBJECT-02","OBJECT-03"];
    public static (IReadOnlyList<Finding> Findings,Reason Reason,IReadOnlyList<ScopeIssue> Issues) Compare(string check,MatchedScope scope,bool visible)
        =>CompareSections(check,scope.Source?.Catalog??[],scope.Target?.Catalog??[],scope.Location,scope.Columns,visible);
    public static (IReadOnlyList<Finding> Findings,Reason Reason,IReadOnlyList<ScopeIssue> Issues) CompareDatabase(string check,Inventory source,Inventory target)
        =>CompareSections(check,source.DatabaseCatalog,target.DatabaseCatalog,null,[],source.Complete&&target.Complete);
    internal static (IReadOnlyList<Finding> Findings,Reason Reason,IReadOnlyList<ScopeIssue> Issues) CompareForRun(string check,MatchedScope scope,bool visible,RunReportStorage storage,CancellationToken token,CatalogColumnMap? columnMap=null)
        =>CompareSections(check,scope.Source?.Catalog??[],scope.Target?.Catalog??[],scope.Location,scope.Columns,visible,storage,token,columnMap);
    internal static (IReadOnlyList<Finding> Findings,Reason Reason,IReadOnlyList<ScopeIssue> Issues) CompareDatabaseForRun(string check,Inventory source,Inventory target,RunReportStorage storage,CancellationToken token)
        =>CompareSections(check,source.DatabaseCatalog,target.DatabaseCatalog,null,[],source.Complete&&target.Complete,storage,token);
    private static (IReadOnlyList<Finding> Findings,Reason Reason,IReadOnlyList<ScopeIssue> Issues) CompareSections(string check,IReadOnlyList<CatalogSection> sourceSections,IReadOnlyList<CatalogSection> targetSections,string? location,IReadOnlyList<ColumnMapping> columns,bool visible,RunReportStorage? storage=null,CancellationToken token=default,CatalogColumnMap? columnMap=null)
    {
        IReadOnlyList<Finding> findings;Action<Finding> addFinding;
        if(storage==null){var list=new List<Finding>();findings=list;addFinding=list.Add;}
        else{var buffer=new FindingBuffer(storage);findings=buffer;addFinding=buffer.Add;}var left=sourceSections.Where(s=>s.CheckId==check).ToArray();
        var right=targetSections.Where(s=>s.CheckId==check).ToArray();
        if(left.Length>1||right.Length>1)return(findings,Reason.AmbiguousScope,[new(location,null,Reason.AmbiguousScope)]);
        IReadOnlyList<ScopeIssue> issues;Action<ScopeIssue> addIssue;
        if(storage==null){var list=new List<ScopeIssue>();issues=list;addIssue=list.Add;}
        else{var buffer=new IssueBuffer(storage);issues=buffer;addIssue=buffer.Add;}
        var reason=!visible?Reason.VisibilityUnknown:left.Length==0||right.Length==0?Reason.NotImplemented:left[0].Reason!=Reason.None?left[0].Reason:right[0].Reason;
        using var localMap=columnMap==null?new CatalogColumnMap(columns,token):null;
        var sourceColumns=columnMap??localMap!;
        var untrustedGaps=new HashSet<CatalogSection>();
        // Normalize both sides before any absence decision; gap keys share the same
        // bounded ordinal sort as facts, without retaining a whole blocking set.
        using var gapIndex=new CatalogFactIndex(ReadGaps(left.SingleOrDefault(),false).Concat(ReadGaps(right.SingleOrDefault(),true)),
            storage!=null&&(long)(left.SingleOrDefault()?.Gaps?.Count??0)+(right.SingleOrDefault()?.Gaps?.Count??0)>4096,token);
        // Constructors consume/validate both inputs before absence is considered.
        // Large run catalogs use the existing authenticated external sort, and
        // run-owned findings spill instead of allocating a second full result list.
        using var leftIndex=new CatalogFactIndex(Build(left.SingleOrDefault(),false),storage!=null&&left.SingleOrDefault()?.Facts?.Count>4096,token);
        using var rightIndex=new CatalogFactIndex(Build(right.SingleOrDefault(),true),storage!=null&&right.SingleOrDefault()?.Facts?.Count>4096,token);
        using var a=leftIndex.Entries(token).GetEnumerator();using var b=rightIndex.Entries(token).GetEnumerator();
        using var gaps=gapIndex.Entries(token).GetEnumerator();
        var hasA=a.MoveNext();var hasB=b.MoveNext();var hasGap=gaps.MoveNext();
        while(hasA||hasB)
        {
            token.ThrowIfCancellationRequested();
            var order=!hasA?1:!hasB?-1:CatalogFactIndex.CompareKey(a.Current.Fact,b.Current.Fact);
            var x=hasA&&order<=0?a.Current:null;var y=hasB&&order>=0?b.Current:null;
            var fact=x?.Fact??y!.Fact;var key=(fact.Key,fact.Attribute);
            while(hasGap&&CatalogFactIndex.CompareKey(gaps.Current.Fact,fact)<0)hasGap=gaps.MoveNext();
            var blocked=hasGap&&CatalogFactIndex.CompareKey(gaps.Current.Fact,fact)==0;
            if(x?.Duplicate==true||y?.Duplicate==true)reason=Reason.AmbiguousScope;
            else if(!blocked)
            {
                // Missing sections and unread attributes never prove absence.
                var missingBlocked=x==null&&(left.Length==0||!CompleteFacts(left[0])||!visible)
                    ||y==null&&(right.Length==0||!CompleteFacts(right[0])||!visible);
                if(!missingBlocked&&(x==null||y==null||!x.Fact.ValueEquals(y.Fact,token)))
                    addFinding(new(Guid.NewGuid().ToString("N"),location,x?.Fact.Column??y?.Fact.Column,key.Attribute,1));
            }
            if(order<=0)hasA=a.MoveNext();if(order>=0)hasB=b.MoveNext();
        }
        // Even gaps beyond the last visible fact must authenticate their full
        // temporary stream before a verdict can be finalized.
        while(hasGap)hasGap=gaps.MoveNext();
        if(reason!=Reason.None&&(issues.Count==0||!visible||untrustedGaps.Count>0))addIssue(new(location,null,reason));
        return(findings,reason,DeduplicateIssues(issues,storage,token));
        bool CompleteFacts(CatalogSection section)=>!untrustedGaps.Contains(section)&&(section.Reason==Reason.None||section.CompleteExceptGaps);
        IEnumerable<CatalogFact> ReadGaps(CatalogSection? section,bool target)
        {
            if(section==null)yield break;
            if(section.Reason!=Reason.None&&!section.CompleteExceptGaps)addIssue(new(location,null,section.Reason));
            if(section.Gaps==null||section.CompleteExceptGaps&&(section.Gaps.Count==0||section.Reason==Reason.None)||section.Gaps.Count>0&&section.Reason==Reason.None)
            {reason=Reason.AmbiguousScope;untrustedGaps.Add(section);if(section.Gaps==null)yield break;}
            foreach(var gap in section.Gaps)
            {
                if(gap==null||string.IsNullOrEmpty(gap.Key)||!Fields.TryGetValue(gap.Attribute,out var field)||field.Check!=check||gap.Reason==Reason.None||!Enum.IsDefined(gap.Reason))
                {reason=Reason.AmbiguousScope;untrustedGaps.Add(section);continue;}
                var key=gap.Key;var column=gap.Column;
                if(gap.Column!=null)
                {
                    var source=target?sourceColumns.Source(gap.Column):gap.Column;
                    key=(source==null?"target-column:":"source-column:")+(source??gap.Column);
                    column=source??gap.Column;
                }
                addIssue(new(location,column,gap.Reason){Attribute=gap.Attribute});
                if(reason==Reason.None)reason=gap.Reason;
                yield return new(key,column,gap.Attribute,null);
            }
        }
        IEnumerable<CatalogFact> Build(CatalogSection? section,bool target)
        {
            if(section==null)yield break;
            if(section.Facts==null){reason=Reason.AmbiguousScope;untrustedGaps.Add(section);yield break;}
            foreach(var fact in section.Facts)
            {
                if(fact==null||string.IsNullOrEmpty(fact.Key)||!Fields.TryGetValue(fact.Attribute,out var field)||field.Check!=check){reason=Reason.NotImplemented;untrustedGaps.Add(section);continue;}
                var key=fact.Key;var column=fact.Column;
                if(column!=null)
                {
                    var source=target?sourceColumns.Source(column):column;
                    key=(source==null?"target-column:":"source-column:")+(source??column);column=source??column;
                }
                yield return fact with { Key=key,Column=column };
            }
        }
    }
}
