using Avalonia.Automation;
using Avalonia.Controls;
using Verifier.Desktop;

/// <summary>Read-only, source-shaped observation of the real application's preview route.</summary>
internal static class PageIntegrationFlowSnapshot
{
    internal static WebPageHeading? Heading(Control? page)=>page switch
    {
        WebHomePage p=>p.Heading,WebConnectionPage p=>p.Heading,WebPreflightPage p=>p.Heading,
        WebSelectionPage p=>p.Heading,WebTrialPage p=>p.Heading,WebReviewPage p=>p.Heading,
        WebProgressPage p=>p.Heading,WebResultsPage p=>p.Heading,WebSavePage p=>p.Heading,_=>null
    };

    internal static string Focus(ReplicaWorkspace workspace,Window window)
    {
        var control=window.FocusManager?.GetFocusedElement() as Control;
        if(control is null||control==workspace)return "BODY";
        if(control==Heading(workspace.CurrentPage)?.FocusTarget)return "screen-title";
        if(control==workspace.InfoDialog?.CloseButton||control==workspace.ResultDetails?.CloseButton||control==workspace.MatchingDialog?.CloseButton)return "modal-close";
        if(workspace.CurrentPage is WebPreflightPage preflight&&control==preflight.Table.Rows.Single(x=>x.Key=="access").PermissionsButton)return "permissions";
        if(workspace.CurrentPage is WebSelectionPage selection)
        {
            if(control==selection.BulkCheckbox)return "table-all";
            var mapping=selection.Rows.FirstOrDefault(x=>control==x.ChangeButton);
            if(mapping is not null)return "mapping:"+mapping.SourceName;
        }
        if(control is WebResultSummaryRow resultRow)return "detail:"+resultRow.CategoryKey;
        var id=AutomationProperties.GetAutomationId(control)??"";
        if(id.StartsWith("Footer-",StringComparison.Ordinal))return id[7..];
        if(id.StartsWith("Results-filter-",StringComparison.Ordinal))return "filter:"+id[15..];
        if(id.StartsWith("Selection-mapping-",StringComparison.Ordinal))return "mapping:"+id[18..];
        if(control is CheckBox)
        {
            if(workspace.CurrentPage is WebReviewPage p&&control==p.StoppedChoice.Checkbox)return "stopped";
            if(id=="table-all")return id;
            return "INPUT";
        }
        if(id.StartsWith("source-",StringComparison.Ordinal)||id.StartsWith("target-",StringComparison.Ordinal)||id.StartsWith("trial-",StringComparison.Ordinal))
            return id.Replace("-",".");
        return id.Length==0?"BODY":id;
    }

    internal static object Read(string id,ReplicaWorkspace workspace,Window window)
    {
        object? run=workspace.SampleRun is not{} r?null:new
        {
            id=r.Id,interrupted=r.Interrupted,complete=r.Complete,trialSelected=r.TrialSelected,
            groups=r.Groups.Select(g=>new{key=g.Key,total=g.Total,done=g.Done,status=g.Status,equal=g.Equal,different=g.Different,unknown=g.Unknown}).ToArray(),
            tables=r.TableNames,matches=r.Maps
        };
        var page=workspace.CurrentPage;
        var buttons=new List<WebActionButton>();
        if(workspace.Footer.BackButton is{} back)buttons.Add(back);
        buttons.AddRange(workspace.Footer.ActionButtons);
        return new
        {
            id,screen=workspace.Screen,english=workspace.English,connected=workspace.Connection.Connected,
            busy=workspace.Connection.Busy,error=workspace.Screen=="trial"?workspace.Trial.Error:workspace.Connection.Error,
            categories=workspace.Categories,tables=workspace.Tables,maps=workspace.Maps,expanded=workspace.ExpandedCategories,
            trial=new{host=workspace.Trial.Host,db=workspace.Trial.Database,validInput=workspace.Trial.ValidInput,invalidInput=workspace.Trial.InvalidInput,ready=workspace.Trial.Ready,consent=workspace.Trial.Consent},
            stopped=workspace.Stopped,visited=workspace.Visited.ToArray(),filter=workspace.ResultFilter,
            formats=workspace.Formats,saved=workspace.Saved,run,
            modal=workspace.MatchingDialog is not null?"mapping":workspace.ResultDetails is not null?"detail":workspace.InfoDialog is not null?workspace.Screen=="capabilities"?"permissions":"research":null,
            query=workspace.TableQuery,title=Heading(page)?.TitleText.Text??"",focus=Focus(workspace,window),
            announcement=workspace.Announcement,offset=workspace.Viewport.Offset,
            footer=buttons.Select(b=>new{action=(AutomationProperties.GetAutomationId(b)??"").Replace("Footer-",""),label=b.Content?.ToString()??"",enabled=b.IsEnabled}).ToArray()
        };
    }
}
