using System.Collections;
using Verifier.Core;

namespace Verifier.Desktop;

// One immutable plan plus explicit selection changes. Dense selections keep
// exclusions, sparse selections keep inclusions; neither copies every name.
internal sealed class WebScopeKeys:IReadOnlyList<string>
{
    private readonly IReadOnlyList<MatchedScope> scopes;
    private readonly bool all;
    private readonly HashSet<string> changes;
    public WebScopeKeys(IReadOnlyList<MatchedScope> scopes):this(scopes,true,new(StringComparer.Ordinal)){}
    private WebScopeKeys(IReadOnlyList<MatchedScope> scopes,bool all,HashSet<string> changes)
    {this.scopes=scopes;this.all=all;this.changes=changes;}
    internal bool AllSelected=>all&&changes.Count==0;
    public int Count=>all?scopes.Count-changes.Count:changes.Count;
    public string this[int index]
    {
        get
        {
            if((uint)index>=(uint)Count)throw new ArgumentOutOfRangeException(nameof(index));
            if(AllSelected)return scopes[index].Key;
            foreach(var key in this)if(index--==0)return key;
            throw new VerificationException(Reason.ObservationChanged);
        }
    }
    internal bool Contains(string key)=>ScopeMatching.Find(scopes,key)!=null&&(all!=changes.Contains(key));
    internal bool Covers(IReadOnlyList<MatchedScope> plan)=>ReferenceEquals(scopes,plan);
    internal WebScopeKeys WithSelected(string key,bool selected)
    {
        if(ScopeMatching.Find(scopes,key)==null||Contains(key)==selected)return this;
        var next=new HashSet<string>(changes,StringComparer.Ordinal);
        if(all!=selected)next.Add(key);else next.Remove(key);return Changed(all,next);
    }
    internal WebScopeKeys WithMatches(Func<string,bool> matches,bool selected,bool every)
    {
        if(every)return new(scopes,selected,new(StringComparer.Ordinal));
        var next=new HashSet<string>(changes,StringComparer.Ordinal);
        foreach(var scope in scopes)if(matches(scope.Key)){if(all!=selected)next.Add(scope.Key);else next.Remove(scope.Key);}
        return Changed(all,next);
    }
    internal WebScopeKeys Remap(IReadOnlyList<MatchedScope> plan,IReadOnlyDictionary<string,string> previous)
    {
        var next=changes.Where(key=>ScopeMatching.Find(plan,key)!=null).ToHashSet(StringComparer.Ordinal);
        // Newly introduced scopes were selected by the previous refresh rule.
        if(!all)foreach(var scope in plan)if(!previous.ContainsKey(scope.Key))next.Add(scope.Key);
        return new WebScopeKeys(plan,all,next).Changed(all,next);
    }
    private WebScopeKeys Changed(bool baseline,HashSet<string> next)
        =>next.Count==scopes.Count?new(scopes,!baseline,new(StringComparer.Ordinal)):new(scopes,baseline,next);
    public IEnumerator<string> GetEnumerator()
    {
        if(!all)
        {
            foreach(var key in changes.Order(StringComparer.Ordinal))
            {if(ScopeMatching.Find(scopes,key)==null)throw new VerificationException(Reason.ObservationChanged);yield return key;}
        }
        else for(var i=0;i<scopes.Count;i++){var key=scopes[i].Key;if(!changes.Contains(key))yield return key;}
    }
    IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
}
