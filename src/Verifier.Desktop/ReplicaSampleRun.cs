using System.Text.Json;
using System.Text.RegularExpressions;

namespace Verifier.Desktop;

/// <summary>Source preview's bounded sample state. No database data or credentials are retained.</summary>
public sealed record ReplicaSampleGroup(string Key,int Total,int Done,int Equal,int Different,int Unknown,string Status);
public sealed record ReplicaSampleRun(string Id,IReadOnlyList<ReplicaSampleGroup> Groups,bool Interrupted,bool Complete,bool TrialSelected,IReadOnlyList<string> TableNames,IReadOnlyDictionary<string,string> Maps)
{
    public static readonly string[] CategoryKeys=["data","schema","access","objects","trial"];
    public static readonly string[] AllowedTables=["demo.customers","demo.orders","demo.line_items"];
    public static ReplicaSampleRun Create(IReadOnlyList<string> selected,IReadOnlyList<string> tables,IReadOnlyDictionary<string,string> maps)
    {
        var groups=CategoryKeys.Where(selected.Contains).Select(key=>new ReplicaSampleGroup(key,key switch{"data"=>tables.Count*4,"schema"=>tables.Count*6,"access"=>8,"objects"=>12,_=>4},0,0,0,0,"pending")).ToArray();
        return new("UI-"+Base36(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),groups,false,false,selected.Contains("trial"),tables.ToArray(),new Dictionary<string,string>(maps));
    }
    public ReplicaSampleGroup CompleteGroup(ReplicaSampleGroup group)
    {
        var hasOrders=TableNames.Contains("demo.orders");
        var different=group.Key switch{"data"=>hasOrders?1:0,"schema"=>(hasOrders?1:0)+TableNames.Count(name=>Maps.GetValueOrDefault(name)!=name),"access" or "trial"=>1,_=>0};
        var unknown=group.Key=="access"?2:0;
        return group with{Done=group.Total,Different=different,Unknown=unknown,Equal=group.Total-different-unknown,Status="completed"};
    }
    // The source completes the overall run on the tick after its final group.
    public ReplicaSampleRun Tick()
    {
        if(Interrupted||Complete)return this;
        var groups=Groups.ToArray();var index=Array.FindIndex(groups,g=>g.Status!="completed");
        if(index<0)return this with{Complete=true};
        var current=groups[index] with{Status="running",Done=Math.Min(groups[index].Done+1,groups[index].Total)};
        groups[index]=current.Done==current.Total?CompleteGroup(current):current;
        return this with{Groups=groups};
    }
    public ReplicaSampleRun Cancel()=>this with{Interrupted=true,Groups=Groups.Select(g=>g.Status=="running"?g with{Status="interrupted"}:g).ToArray()};
    public ReplicaSampleRun CompleteAll()=>this with{Groups=Groups.Select(CompleteGroup).ToArray(),Complete=true};
    public static ReplicaSampleRun Read(ReadOnlyMemory<byte> bytes)
    {
        if(bytes.Length>1024*1024)throw new InvalidDataException("Not a valid sample report");
        using var document=JsonDocument.Parse(bytes);var root=document.RootElement;
        if(root.GetProperty("kind").GetString()!="dmv-ui-sample"||root.GetProperty("version").ValueKind!=JsonValueKind.Number||!root.GetProperty("version").TryGetDouble(out var version)||version!=1||root.GetProperty("sample").ValueKind!=JsonValueKind.True)throw new InvalidDataException("Not a valid sample report");
        var run=root.GetProperty("run");var groupData=run.GetProperty("groups");if(groupData.ValueKind!=JsonValueKind.Array||groupData.GetArrayLength() is <1 or >5)throw new InvalidDataException("Invalid sample groups");
        var keys=new HashSet<string>();var groups=new List<ReplicaSampleGroup>();
        foreach(var g in groupData.EnumerateArray())
        {
            var key=g.GetProperty("key").GetString()!;var status=g.GetProperty("status").GetString()!;
            if(!CategoryKeys.Contains(key)||!keys.Add(key)||status is not("pending" or "running" or "interrupted" or "completed"))throw new InvalidDataException("Invalid sample group");
            int Number(string name){var n=g.GetProperty(name);if(n.ValueKind!=JsonValueKind.Number||!n.TryGetDouble(out var value)||!double.IsFinite(value)||value is <0 or >10000||Math.Truncate(value)!=value)throw new InvalidDataException("Invalid sample count");return (int)value;}
            var total=Number("total");var done=Number("done");var equal=Number("equal");var different=Number("different");var unknown=Number("unknown");
            if(done>total||equal+different+unknown>done)throw new InvalidDataException("Invalid sample totals");
            groups.Add(new(key,total,done,equal,different,unknown,status));
        }
        var id=run.GetProperty("id").GetString()!;
        if(id is null||!Regex.IsMatch(id,"^UI-[A-Z0-9]+$",RegexOptions.CultureInvariant))throw new InvalidDataException("Invalid sample identifier");
        static bool Flag(JsonElement e){if(e.ValueKind is not(JsonValueKind.True or JsonValueKind.False))throw new InvalidDataException("Invalid sample flag");return e.GetBoolean();}
        var interrupted=Flag(run.GetProperty("interrupted"));var complete=Flag(run.GetProperty("complete"));
        var tables=run.GetProperty("tables").EnumerateArray().Select(e=>e.GetString()!).ToArray();
        if(tables.Any(x=>!AllowedTables.Contains(x))||tables.Distinct().Count()!=tables.Length)throw new InvalidDataException("Invalid sample tables");
        var maps=new Dictionary<string,string>();var hasMatches=run.TryGetProperty("matches",out var match);
        foreach(var name in AllowedTables)
        {
            string? target=null;
            if(hasMatches&&match.ValueKind==JsonValueKind.Object&&match.TryGetProperty(name,out var field))
            {
                target=field.ValueKind switch
                {
                    JsonValueKind.String=>field.GetString(),
                    JsonValueKind.Null or JsonValueKind.False=>null,
                    JsonValueKind.Number when field.TryGetDouble(out var n)&&n==0=>null,
                    _=>throw new InvalidDataException("Invalid sample matching")
                };
            }
            if(!string.IsNullOrEmpty(target)&&!AllowedTables.Contains(target)&&target!="archive.orders")throw new InvalidDataException("Invalid sample matching");
            maps[name]=string.IsNullOrEmpty(target)?name:target;
        }
        if(maps.Values.Distinct().Count()!=AllowedTables.Length)throw new InvalidDataException("Duplicate sample matching");
        // As in the source reader, input findings and unknown extra properties are discarded.
        return new(id,groups,interrupted,complete,keys.Contains("trial"),tables,maps);
    }
    public byte[] WriteJson(object[] safeFindings)=>JsonSerializer.SerializeToUtf8Bytes(new{kind="dmv-ui-sample",version=1,sample=true,run=new{id=Id,interrupted=Interrupted,complete=Complete,trialSelected=TrialSelected,groups=Groups.Select(g=>new{key=g.Key,total=g.Total,done=g.Done,equal=g.Equal,different=g.Different,unknown=g.Unknown,status=g.Status}),tables=TableNames,matches=Maps},findings=safeFindings},new JsonSerializerOptions{WriteIndented=true});
    private static string Base36(long value){const string digits="0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";var result="";do{result=digits[(int)(value%36)]+result;value/=36;}while(value>0);return result;}
}
