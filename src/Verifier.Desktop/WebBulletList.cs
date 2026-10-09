using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;

namespace Verifier.Desktop;

/// <summary>The original default ul.muted, with its outside disc markers and 20px inline padding.</summary>
public sealed class WebBulletList : Panel
{
    private readonly List<Item> _items=[];
    private ListPeer? _peer;
    public IReadOnlyList<Item> Items=>_items;
    public WebBulletList(){UseLayoutRounding=false;Margin=new(0,14,0,14);}
    public void SetItems(IEnumerable<string> text)
    {
        Children.Clear();_items.Clear();foreach(var value in text){var item=new Item(value);_items.Add(item);Children.Add(item);}
        _peer?.Refresh();InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size available)
    {double h=0,w=0;foreach(var item in _items){item.Measure(new(Math.Max(0,available.Width-20),double.PositiveInfinity));h+=item.DesiredSize.Height;w=Math.Max(w,item.DesiredSize.Width);}return new(double.IsFinite(available.Width)?available.Width:w+20,h);}
    protected override Size ArrangeOverride(Size final)
    {double y=0;foreach(var item in _items){item.Arrange(new(20,y,Math.Max(0,final.Width-20),item.DesiredSize.Height));y+=item.DesiredSize.Height;}return final;}
    protected override AutomationPeer OnCreateAutomationPeer()=>_peer=new(this);
    private sealed class ListPeer(WebBulletList list):ControlAutomationPeer(list)
    {public void Refresh()=>InvalidateChildren();protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.List;}
    public sealed class Item:Panel
    {
        public WebSystemTextBlock Text{get;}
        public WebSystemTextBlock Marker{get;}=WebTypography.CreateCandidate("•",14,22.4,Brush.Parse("#b5bac1"));
        internal Item(string text)
        {UseLayoutRounding=false;Text=WebTypography.CreateCandidate(text,14,22.4,Brush.Parse("#b5bac1"));Children.Add(Text);Children.Add(Marker);AutomationProperties.SetAccessibilityView(Marker,AccessibilityView.Raw);}
        protected override Size MeasureOverride(Size available)
        {var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Text.LineHeight=Marker.LineHeight=Math.Floor(22.4*units)/units;Text.Measure(available);Marker.Measure(Size.Infinity);return new(double.IsFinite(available.Width)?available.Width:Text.DesiredSize.Width,Text.DesiredSize.Height);}
        protected override Size ArrangeOverride(Size final)
        {Text.Arrange(new Rect(final));Marker.Arrange(new(-7-Marker.DesiredSize.Width,0,Marker.DesiredSize.Width,Marker.DesiredSize.Height));return final;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new ItemPeer(this);
        private sealed class ItemPeer(Item item):ControlAutomationPeer(item)
        {protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.ListItem;}
    }
}
