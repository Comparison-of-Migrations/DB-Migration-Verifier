using System.Collections;

namespace Verifier.Desktop;

// Indexes existing report collections. Display strings are created only when requested;
// a segment represents a whole check, not one allocated object per finding.
internal sealed class WebResultItems<T> : IReadOnlyList<T>
{
    private readonly record struct Segment(int Start,int Count,string Tag,Func<int,T> Item);
    private readonly List<Segment> _segments=[];
    public int Count{get;private set;}
    public void Add(int count,Func<int,T> item,string tag="")
    {
        if(count<0)throw new ArgumentOutOfRangeException(nameof(count));
        if(count==0)return;
        var next=checked(Count+count);_segments.Add(new(Count,count,tag,item));Count=next;
    }
    public T this[int index]
    {
        get
        {
            if((uint)index>=(uint)Count)throw new ArgumentOutOfRangeException(nameof(index));
            var low=0;var high=_segments.Count-1;
            while(low<high){var middle=low+(high-low+1)/2;if(_segments[middle].Start<=index)low=middle;else high=middle-1;}
            var segment=_segments[low];return segment.Item(index-segment.Start);
        }
    }
    public WebResultItems<T> WithTag(string tag)
    {
        var result=new WebResultItems<T>();foreach(var s in _segments)if(s.Tag==tag)result.Add(s.Count,s.Item,s.Tag);return result;
    }
    public IEnumerator<T> GetEnumerator(){foreach(var s in _segments)for(var i=0;i<s.Count;i++)yield return s.Item(i);}
    IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
}
