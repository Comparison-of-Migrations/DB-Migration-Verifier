using System.Text.Json;
using Verifier.Core;

namespace Verifier.Data;

// Native rows contain no password/hash columns. Names, SID, expressions and
// permission evidence remain local; CatalogComparison emits fixed kinds only.
internal static class SqlServerCatalog
{
    internal static string Key(params string?[] parts)=>SqlServerMetadata.Serialize(parts);
    internal static CatalogSection IndexDetails(string family,IReadOnlyList<string?[]> rows,int fields,
        IReadOnlySet<string> expected,bool visible,Func<string?[],bool> known)
    {
        if(fields<2||rows.Any(r=>r.Length!=fields||r[0]==null||!expected.Contains(r[0]!)))
            throw new VerificationException(Reason.ObservationChanged);
        var facts=new List<CatalogFact>();var gaps=new List<CatalogGap>();
        foreach(var name in expected.Order(StringComparer.Ordinal))
        {
            var selected=rows.Where(r=>r[0]==name).ToArray();var key=Key("index-detail",family,name);
            if(selected.Length==0||selected.Any(r=>!known(r)))
                gaps.Add(new(key,null,"IndexOptions","",Reason.VisibilityUnknown));
            else facts.Add(new(key,null,"IndexOptions",SqlServerMetadata.Serialize(selected.Select(r=>SqlServerMetadata.Serialize(r[1..])).Order(StringComparer.Ordinal))));
        }
        return Section("SCHEMA-03",facts,visible,gaps);
    }
    internal static CatalogSection Section(string check,IReadOnlyList<CatalogFact> facts,bool visible,IReadOnlyList<CatalogGap>? gaps=null)
    {
        gaps??=[];
        return new(check,facts,!visible?Reason.VisibilityUnknown:gaps.Count>0?gaps[0].Reason:Reason.None)
        {Gaps=gaps,CompleteExceptGaps=visible&&gaps.Count>0};
    }
    internal static CatalogSection TableType(string schema,string name,IReadOnlyList<string?[]> options,
        IReadOnlyList<string?[]> columns,IReadOnlyList<string?[]> checks,IReadOnlyList<string?[]> keys,
        IReadOnlyList<string?[]> indexes,bool visible)
    {
        if(options.Count!=1||options[0].Length!=1||options[0][0] is not("True" or "False")||columns.Count==0||
           columns.Any(r=>r.Length!=30||r[0]==null||r[1]==null)||checks.Any(r=>r.Length!=8)||
           keys.Any(r=>r.Length!=7||r[6]==null)||indexes.Any(r=>r.Length!=23||r[22]==null))throw new VerificationException(Reason.ObservationChanged);
        var facts=new List<CatalogFact>();var gaps=new List<CatalogGap>();
        void Add(string part,IReadOnlyList<string?[]> rows,bool hidden=false,bool grouped=false)
        {
            var key=Key("table-type",schema,name,part);
            if(hidden)gaps.Add(new(key,null,"UserTypeDefinitions","",Reason.VisibilityUnknown));
            else
            {
                var values=grouped?rows.GroupBy(r=>r[^1],StringComparer.Ordinal)
                    .Select(g=>SqlServerMetadata.Serialize(g.Select(r=>SqlServerMetadata.Serialize(r[..^1])).Order(StringComparer.Ordinal)))
                    :rows.Select(r=>SqlServerMetadata.Serialize(r));
                facts.Add(new(key,null,"UserTypeDefinitions",SqlServerMetadata.Serialize(values.Order(StringComparer.Ordinal))));
            }
        }
        Add("options",options);
        Add("columns",columns,columns.Any(r=>r[13]=="1"&&r[11]==null||r[15]=="1"&&r[14]==null||
            r[10]=="True"&&r[12]==null||r[9]=="True"&&(r[17]==null||r[18]==null)||r[2]==null||r[3]==null));
        Add("checks",checks,checks.Any(r=>r[2]==null));
        Add("keys",keys,keys.Any(r=>r[1]==null||r[3]==null),grouped:true);
        Add("indexes",indexes,indexes.Any(r=>r[6]=="True"&&r[7]==null),grouped:true);
        // Table types support rowstore and memory-optimized hash indexes. Do
        // not certify an unexpected native family by comparing a partial shape.
        if(indexes.Any(r=>r[1] is not("0" or "1" or "2" or "7")||r[1]=="7"&&r[19]==null))
            gaps.Add(new(Key("table-type",schema,name,"indexes"),null,"UserTypeDefinitions","",Reason.NotImplemented));
        return Section("OBJECT-03",facts,visible,gaps);
    }
    internal static CatalogSection XmlSchemas(IEnumerable<string?[]> rows,bool visible,SqlServerDatabase.CatalogFacts? facts=null,SqlServerDatabase.CatalogGaps? gaps=null)
    {
        facts??=new();var start=facts.Count;gaps??=new();var gapStart=gaps.Count;
        foreach(var row in rows)
        {
            if(row.Length!=4||row[0]==null||row[1]==null)throw new VerificationException(Reason.ObservationChanged);
            var key=Key("xml-schema",row[0],row[1]);
            // This is the stored XSD reconstructed by the native catalog API,
            // not a claim about discarded comments/prefixes in the import file.
            if(row[2]==null||row[3]==null)gaps.Add(new(key,null,"UserTypeDefinitions","",Reason.VisibilityUnknown));
            else facts.Add(new(key,null,"UserTypeDefinitions",SqlServerMetadata.Serialize(row[2..])));
        }
        return Section("OBJECT-03",facts.Snapshot(start),visible,gaps.Snapshot(gapStart));
    }
    internal static CatalogSection Credentials(IReadOnlyList<string?[]> rows,bool visible,SqlServerDatabase.CatalogFacts? facts=null,SqlServerDatabase.CatalogGaps? gaps=null)
    {
        facts??=new();var start=facts.Count;gaps??=new();var gapStart=gaps.Count;
        foreach(var row in rows)
        {
            if(row.Length!=11||row[0] is not("database" or "server")||row[9] is not("0" or "1"))
                throw new VerificationException(Reason.ObservationChanged);
            var key=Key("credential",row[0],row[1],row[2]);
            if(row[2]==null||row[0]=="server"&&row[1]==null)
            {visible=false;gaps.Add(new(key,null,"RoleConfiguration","",Reason.VisibilityUnknown));continue;}
            var providerComplete=row[9]=="0"||row[4]=="CRYPTOGRAPHIC PROVIDER"&&row[5..9].All(x=>x!=null);
            if(row[3]==null||!providerComplete)gaps.Add(new(key,null,"RoleConfiguration","",Reason.VisibilityUnknown));
            else facts.Add(new(key,null,"RoleConfiguration",SqlServerMetadata.Serialize(row[3..10])));
            if(row[0]=="database")
            {
                var ownerKey=Key("credential-owner",row[0],row[2]);
                if(row[10]==null)gaps.Add(new(ownerKey,null,"RoleConfiguration","",Reason.VisibilityUnknown));
                else facts.Add(new(ownerKey,null,"RoleConfiguration",row[10]));
            }
            // Public identity/provider equality cannot establish equality of a
            // stored authentication secret. All visible public login mappings
            // are compared separately by the native binding query.
            gaps.Add(new(Key("credential-private",row[0],row[1],row[2]),null,"UninspectedConfiguration","",Reason.NotImplemented));
        }
        return Section("ACCESS-01",facts.Snapshot(start),visible,gaps.Snapshot(gapStart));
    }
    internal static CatalogSection MergeAccess(CatalogSection a,CatalogSection b,SqlServerDatabase.CatalogFacts? facts=null,SqlServerDatabase.CatalogGaps? gaps=null,bool reuseFrozenFacts=false)
    {
        if(a.CheckId!="ACCESS-01"||b.CheckId!="ACCESS-01")throw new VerificationException(Reason.InvalidInput);
        facts??=new();var start=facts.Count;
        // These observer-owned views already authenticate every read. Joining
        // their fixed ranges avoids decoding and encrypting the same facts
        // again; it neither caches plaintext nor extends either owner's life.
        var joined=reuseFrozenFacts&&(a.Facts.Count==0||a.Facts is IFrozenMetadata<CatalogFact>)&&
            (b.Facts.Count==0||b.Facts is IFrozenMetadata<CatalogFact>)?new JoinedAccessFacts(
                a.Facts.Count==0?Array.Empty<CatalogFact>():a.Facts,b.Facts.Count==0?Array.Empty<CatalogFact>():b.Facts):null;
        if(joined==null){facts.AddRange(a.Facts);facts.AddRange(b.Facts);}
        gaps??=new();var gapStart=gaps.Count;gaps.AddRange(a.Gaps);gaps.AddRange(b.Gaps);
        foreach(var (section,side) in new[]{(a,"left"),(b,"right")})
            if(section.Reason!=Reason.None&&!section.CompleteExceptGaps)
                gaps.Add(new(Key("access-coverage",side),null,"RoleConfiguration","",section.Reason));
        var reason=a.Reason!=Reason.None&&!a.CompleteExceptGaps?a.Reason:
            b.Reason!=Reason.None&&!b.CompleteExceptGaps?b.Reason:a.Reason!=Reason.None?a.Reason:b.Reason;
        return new("ACCESS-01",joined??facts.Snapshot(start),reason)
        {Gaps=gaps.Snapshot(gapStart),CompleteExceptGaps=reason!=Reason.None&&gaps.Count>gapStart&&
            (a.Reason==Reason.None||a.CompleteExceptGaps)&&(b.Reason==Reason.None||b.CompleteExceptGaps)};
    }
    private sealed class JoinedAccessFacts(IReadOnlyList<CatalogFact> left,IReadOnlyList<CatalogFact> right):IFrozenMetadata<CatalogFact>
    {
        public int Count{get;}=checked(left.Count+right.Count);
        public CatalogFact this[int index]=>index<0||index>=Count?throw new ArgumentOutOfRangeException(nameof(index)):
            index<left.Count?left[index]:right[index-left.Count];
        public IEnumerator<CatalogFact> GetEnumerator(){foreach(var fact in left)yield return fact;foreach(var fact in right)yield return fact;}
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    internal static CatalogSection Dependencies(IEnumerable<string?[]> rows,bool visible,SqlServerDatabase.CatalogFacts? facts=null,SqlServerDatabase.CatalogGaps? gaps=null)
    {
        facts??=new();var start=facts.Count;gaps??=new();var gapStart=gaps.Count;
        foreach(var row in rows)
        {
            if(row.Length!=16||row[0]==null||row[3]==null||row[4]==null||row[5]==null||row[10]==null||row[11..14].Any(v=>v is not("True" or "False")))
                throw new VerificationException(Reason.ObservationChanged);
            var key=Key("dependency",row[2],row[3],row[14],row[6],row[7],row[8],row[9],row[15],row[10]);
            if(row[9]==null||row[4]!="0"&&row[14]==null||row[5]!="0"&&row[15]==null)
            {visible=false;gaps.Add(new(key,null,"ViewDependencies","",Reason.VisibilityUnknown));continue;}
            // Native IDs are database-local, so compare names/classes and flags.
            // An unresolved/outbound reference still has a comparable declaration.
            facts.Add(new(key,null,"ViewDependencies",SqlServerMetadata.Serialize(row[11..14])));
            if(row[10] is not("1" or "6" or "10"))gaps.Add(new(key,null,"ObjectDependencies","",Reason.NotImplemented));
            else if(row[1]==null||row[8]==null||row[12]=="True"||row[13]=="True"||row[6]!=null||row[7]!=null)
                gaps.Add(new(key,null,"ObjectDependencies","",Reason.MissingDependency));
        }
        return Section("OBJECT-01",facts.Snapshot(start),visible,gaps.Snapshot(gapStart));
    }
    internal static CatalogSection Synonyms(IEnumerable<string?[]> rows,bool visible,SqlServerDatabase.CatalogFacts? facts=null,SqlServerDatabase.CatalogGaps? gaps=null)
    {
        facts??=new();var start=facts.Count;gaps??=new();var gapStart=gaps.Count;
        foreach(var row in rows)
        {
            if(row.Length!=8||row[0]==null||row[1]==null||row[2]==null)throw new VerificationException(Reason.ObservationChanged);
            var key=Key("synonym",row[1],row[2]);
            if(row[3]==null||row[4]==null)gaps.Add(new(key,null,"RelatedRoutines","",Reason.VisibilityUnknown));
            else facts.Add(new(key,null,"RelatedRoutines",SqlServerMetadata.Serialize(new[]{row[3],row[4],row[6],row[7]})));
            if(row[5]==null||row[6]==null||row[7]==null)gaps.Add(new(key,null,"ObjectDependencies","",Reason.MissingDependency));
        }
        return Section("OBJECT-01",facts.Snapshot(start),visible,gaps.Snapshot(gapStart));
    }
    internal static CatalogSection Sequences(IEnumerable<string?[]> rows,bool visible,SqlServerDatabase.CatalogFacts? facts=null,SqlServerDatabase.CatalogGaps? gaps=null)
    {
        facts??=new();var start=facts.Count;gaps??=new();var gapStart=gaps.Count;
        foreach(var row in rows)
        {
            if(row.Length!=19||row[0]==null||row[1]==null)throw new VerificationException(Reason.ObservationChanged);
            var key=Key("sequence",row[0],row[1]);
            facts.Add(new(key,null,"SequenceDefinitions",SqlServerMetadata.Serialize(row[2..16])));
            // Native current_value is the last obligated value, not a promised
            // next number. last_used_value also distinguishes a never-used sequence.
            facts.Add(new(key,null,"SequenceState",SqlServerMetadata.Serialize(row[16..19])));
            if(row[2]==null||row[3]==null||row[14]==null||row[15]==null)gaps.Add(new(key,null,"SequenceDefinitions","",Reason.VisibilityUnknown));
        }
        return Section("OBJECT-02",facts.Snapshot(start),visible,gaps.Snapshot(gapStart));
    }
    internal static CatalogSection Principals(IReadOnlyList<string?[]> principals,IReadOnlyList<string?[]> memberships,
        IReadOnlyList<string?[]> grants,IReadOnlyList<string?[]> owners,bool visible,CatalogSection? server=null,SqlServerDatabase.CatalogFacts? facts=null,SqlServerDatabase.CatalogGaps? gaps=null)
    {
        facts??=new();var start=facts.Count;gaps??=new();var gapStart=gaps.Count;
        foreach(var row in principals)
        {
            if(row.Length!=9||row[0]==null)throw new VerificationException(Reason.ObservationChanged);
            facts.Add(new(Key("principal",row[0]),null,"RelatedRoles",SqlServerMetadata.Serialize(row[1..])));
        }
        foreach(var row in memberships)
        {
            if(row.Length!=2||row.Any(x=>x==null))throw new VerificationException(Reason.ObservationChanged);
            facts.Add(new(Key("membership",row[0],row[1]),null,"RelatedRoles","true"));
        }
        foreach(var row in owners)
        {
            if(row.Length!=3)throw new VerificationException(Reason.ObservationChanged);
            var key=Key("owner",row[0],row[1]);
            var attribute=row[0]=="database"?"DatabaseAccess":"SchemaAccess";
            if(row[2]==null)gaps.Add(new(key,null,attribute,"",Reason.VisibilityUnknown));
            else facts.Add(new(key,null,attribute,row[2]));
        }
        foreach(var row in grants)
        {
            // class, schema, object/type/principal, column, grantee, grantor,
            // permission and native G/W/D/R state. Never union column exceptions.
            if(row.Length!=8)throw new VerificationException(Reason.ObservationChanged);
            if(row[4]==null||row[5]==null||row[6]==null)visible=false;
            var attribute=row[0] switch{"0"=>"DatabaseAccess","3"=>"SchemaAccess","1" when row[3]!=null=>"DatabaseColumnAccess","1"=>"TableAccess",_=>"DatabaseAccess"};
            var key=Key("grant",row[0],row[1],row[2],row[3],row[4],row[5],row[6]);
            if(row[4]==null||row[5]==null||row[6]==null||row[7] is not("G" or "W" or "D" or "R")||row[0] is not("0" or "1" or "3" or "4" or "5" or "6" or "10" or "15" or "16" or "17" or "18" or "19" or "23" or "24" or "25" or "26" or "29" or "31" or "32" or "34"))
                gaps.Add(new(key,null,attribute,"",Reason.NotImplemented));
            else if(row[0]!="0"&&row[2]==null||row[3]=="")
            {
                // An unresolved identity cannot locate a gap at the other
                // database's named fact. It must not prove that fact absent.
                visible=false;gaps.Add(new(key,null,attribute,"",Reason.VisibilityUnknown));
            }
            else facts.Add(new(key,null,attribute,row[7]));
        }
        if(server==null)gaps.Add(new("server-security",null,"RelatedRoles","",Reason.NotImplemented));
        else{facts.AddRange(server.Facts);gaps.AddRange(server.Gaps);if(server.Reason!=Reason.None&&!server.CompleteExceptGaps)visible=false;}
        return Section("ACCESS-01",facts.Snapshot(start),visible,gaps.Snapshot(gapStart));
    }
    internal static CatalogSection ServerPrincipals(IReadOnlyList<string?[]> databasePrincipals,IReadOnlyList<string?[]> principals,
        IReadOnlyList<string?[]> memberships,IReadOnlyList<string?[]> grants,bool visible,SqlServerDatabase.CatalogFacts? facts=null,SqlServerDatabase.CatalogGaps? gaps=null)
    {
        facts??=new();var start=facts.Count;gaps??=new();var gapStart=gaps.Count;
        if(databasePrincipals.Any(x=>x.Length!=9)||principals.Any(x=>x.Length!=13)||memberships.Any(x=>x.Length!=2)||grants.Any(x=>x.Length!=7))throw new VerificationException(Reason.ObservationChanged);
        var sids=databasePrincipals.Where(x=>x[1] is "S" or "U" or "G" or "E" or "X" or "C" or "K")
            .Where(x=>x[2]=="1"||x[1] is "U" or "G" or "E" or "X" or "C" or "K").Select(x=>x[4]).Where(x=>x!=null).ToHashSet(StringComparer.Ordinal);
        var selected=principals.Where(x=>sids.Contains(x[2])||x[0]=="public").Select(x=>x[0]!).ToHashSet(StringComparer.Ordinal);
        bool changed;do{changed=false;foreach(var row in memberships)if(row[1]!=null&&selected.Contains(row[1]!)&&row[0]!=null&&selected.Add(row[0]!))changed=true;}while(changed);
        foreach(var row in databasePrincipals.Where(x=>x[2]=="1"&&x[1]=="S"))
        {
            var matches=principals.Where(x=>x[2]==row[4]&&x[1]=="S").Select(x=>x[0]).ToArray();
            var key=Key("login-binding",row[0]);
            if(!visible&&matches.Length==0)gaps.Add(new(key,null,"RelatedRoles","",Reason.VisibilityUnknown));
            else facts.Add(new(key,null,"RelatedRoles",SqlServerMetadata.Serialize(matches)));
        }
        foreach(var row in principals.Where(x=>selected.Contains(x[0]!)))
        {
            var key=Key("server-principal",row[0]);facts.Add(new(key,null,"RelatedRoles",SqlServerMetadata.Serialize(row[1..])));
            if(row[10]=="True")gaps.Add(new(key,null,"RoleConfiguration","",Reason.NotImplemented));
        }
        foreach(var row in memberships.Where(x=>x[1]!=null&&selected.Contains(x[1]!)))
            if(row[0]==null)gaps.Add(new(Key("server-membership",row[1]),null,"RelatedRoles","",Reason.VisibilityUnknown));
            else facts.Add(new(Key("server-membership",row[0],row[1]),null,"RelatedRoles","true"));
        if(grants.Any(x=>x[3]==null||x[4]==null||x[5]==null))visible=false;
        foreach(var row in grants.Where(x=>x[3]!=null&&selected.Contains(x[3]!)))
        {
            var key=Key("server-grant",row[0],row[1],row[2],row[3],row[4],row[5]);
            if(row[4]==null||row[6] is not("G" or "W" or "D" or "R")||row[0] is not("100" or "101" or "105"))gaps.Add(new(key,null,"DatabaseAccess","",Reason.NotImplemented));
            else if(row[0]!="100"&&row[2]==null)
            {visible=false;gaps.Add(new(key,null,"DatabaseAccess","",Reason.VisibilityUnknown));}
            else facts.Add(new(key,null,"DatabaseAccess",row[6]));
        }
        return Section("ACCESS-01",facts.Snapshot(start),visible,gaps.Snapshot(gapStart));
    }
    internal static CatalogSection Policies(IReadOnlyList<string?[]> rows,bool visible,SqlServerDatabase.CatalogFacts? facts=null,SqlServerDatabase.CatalogGaps? gaps=null)
    {
        facts??=new();var start=facts.Count;gaps??=new();var gapStart=gaps.Count;
        foreach(var row in rows)
        {
            if(row.Length!=9||row[0]==null||row[1]==null||row[2]==null)throw new VerificationException(Reason.ObservationChanged);
            var key=Key("policy",row[0],row[1],row[2]);
            facts.Add(new(key,null,"RlsEnabled",row[3]));
            if(row[6]==null)gaps.Add(new(key,null,"RowPolicies","",Reason.VisibilityUnknown));
            else facts.Add(new(key,null,"RowPolicies",SqlServerMetadata.Serialize(row[4..9])));
        }
        return Section("ACCESS-02",facts.Snapshot(start),visible,gaps.Snapshot(gapStart));
    }
}
