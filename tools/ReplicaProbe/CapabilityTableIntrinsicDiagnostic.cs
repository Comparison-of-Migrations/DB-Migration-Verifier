using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Verifier.Desktop;
internal static class CapabilityTableIntrinsicDiagnostic
{
    private sealed record Metric(double Min,double Max);
    public static void Run(WebCapabilityTable table,Window window,string output)
    {
        var samples=new List<object>();
        static double Ceil(double n)=>Math.Ceiling(n*128)/128;
        static double Floor(double n)=>Math.Floor(n*128)/128;
        static Metric Text(string value,double size,int weight)
        {
            var t=WebTypography.CreateCandidate(value,size,size*1.6,weight:weight);t.Measure(Size.Infinity);var max=t.SystemLineWidths.DefaultIfEmpty(0).Max();t.Measure(new Size(1/128d,double.PositiveInfinity));return new(t.SystemLineWidths.DefaultIfEmpty(0).Max(),max);
        }
        foreach(var language in new[]{"ko","en"})
        {
            table.SetEnglish(language=="en");window.UpdateLayout();
            void Add(int index,double ceilMin,double floorMin,double max)=>samples.Add(new{id=language+"-"+index,minCeil=ceilMin+36,minFloor=floorMin+36,max=max+36});
            for(var i=0;i<4;i++){var t=Text(table.Header.Labels[i].Text??"",12,600);Add(i,Ceil(t.Min),Floor(t.Min),Ceil(t.Max));}
            for(var rowIndex=0;rowIndex<4;rowIndex++)
            {
                var row=table.Rows[rowIndex];var first=4+rowIndex*4;var title=Text(row.TitleText.Text??"",13,700);var description=Text(row.DescriptionText.Text??"",12,400);Add(first,Math.Max(Ceil(title.Min),Ceil(description.Min)),Math.Max(Floor(title.Min),Floor(description.Min)),Math.Max(Ceil(title.Max),Ceil(description.Max)));
                foreach(var p in new[]{(1,row.ConnectedPill),(2,row.CapabilityPill)}){var width=Ceil(Text(p.Item2.Text,12,400).Max)+28;Add(first+p.Item1,width,width,width);}
                var details=Text(row.DetailText.Text??"",12,400);if(row.Key=="access"){var button=Text(row.PermissionsButton.Content?.ToString()??"",12,600);Add(first+3,Ceil(details.Min)+Ceil(button.Min)+34,Floor(details.Min)+Floor(button.Min)+34,Ceil(details.Max)+Ceil(button.Max)+34);}else Add(first+3,Ceil(details.Min),Floor(details.Min),Ceil(details.Max));
            }
        }
        File.WriteAllText(Path.Combine(output,"capabilitytable-cell-intrinsic-diagnostic.json"),JsonSerializer.Serialize(new{samples}));
    }
}
