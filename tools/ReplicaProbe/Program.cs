using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.Input;
using Verifier.Desktop;

internal static class Program
{
    private static void WriteTextEvidence(string directory, string name, string json)
    {
        File.WriteAllText(Path.Combine(directory,name),json);
        var endpointFile=Path.Combine(directory,"evidence-endpoint.txt");
        if(!File.Exists(endpointFile))return;
        // Opt-in development fixture endpoint. Only generated typography diagnostics are posted.
        var endpoint=File.ReadAllText(endpointFile).Trim();
        _=Task.Run(async ()=>
        {
            try
            {
                using var client=new System.Net.Http.HttpClient();
                using var content=new System.Net.Http.StringContent(json,System.Text.Encoding.UTF8,"application/json");
                using var response=await client.PostAsync(endpoint+"/"+name,content);
                response.EnsureSuccessStatusCode();
            }
            catch(Exception error) {File.WriteAllText(Path.Combine(directory,"typography-evidence-error.txt"),error.Message);}
        });
    }
    [STAThread]
    private static void Main(string[] args)
    {
        if(args.Length>0 && (args[0]=="--selectionpage"||args[0]=="--selectionpage-window")) {ReplicaPlatform.Configure<SelectionPageProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--trialpage"||args[0]=="--trialpage-window")) {ReplicaPlatform.Configure<TrialPageProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--capabilitytable"||args[0]=="--capabilitytable-window")) {ReplicaPlatform.Configure<CapabilityTableProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--resultspage"||args[0]=="--resultspage-window")) {ReplicaPlatform.Configure<ResultsPageProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--homepage"||args[0]=="--homepage-window")) {ReplicaPlatform.Configure<HomePageProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--pageintegration"||args[0]=="--pageintegration-window")) {PageIntegrationProbe.Run(args);return;}
        if(args.Length>0 && (args[0]=="--welcomeentryhint" || args[0]=="--welcomeentryhint-window")) {ReplicaPlatform.Configure<WelcomeEntryHintProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--welcomepaper" || args[0]=="--welcomepaper-window")) {ReplicaPlatform.Configure<WelcomePaperProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--runningdot" || args[0]=="--runningdot-window")) {ReplicaPlatform.Configure<RunningDotProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--progressmeter" || args[0]=="--progressmeter-window")) {ReplicaPlatform.Configure<ProgressMeterProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--preflightpage" || args[0]=="--preflightpage-window")) {ReplicaPlatform.Configure<PreflightPageProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--progresspage" || args[0]=="--progresspage-window")) {ReplicaPlatform.Configure<ProgressPageProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--metrics" || args[0]=="--metrics-window")) {ReplicaPlatform.Configure<MetricsProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--metricitem" || args[0]=="--metricitem-window")) {ReplicaPlatform.Configure<MetricItemProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--infodialog" || args[0]=="--infodialog-window")) {ReplicaPlatform.Configure<InfoDialogProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--capabilityviewport" || args[0]=="--capabilityviewport-window")) {ReplicaPlatform.Configure<CapabilityViewportProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--capabilityheader" || args[0]=="--capabilityheader-window")) {ReplicaPlatform.Configure<CapabilityHeaderProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--welcomepapercontent") {ReplicaPlatform.Configure<WelcomePaperContentProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--findingarticle" || args[0]=="--findingarticle-window")) {ReplicaPlatform.Configure<FindingArticleProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--welcomesteps") {ReplicaPlatform.Configure<WelcomeStepsProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--welcomestep") {ReplicaPlatform.Configure<WelcomeStepProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--infodialogbody") {ReplicaPlatform.Configure<InfoDialogBodyProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--samplereport") {ReplicaPlatform.Configure<SampleReportProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--capabilityrow"||args[0]=="--capabilityrow-window")) {ReplicaPlatform.Configure<CapabilityRowProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--connectionpage"||args[0]=="--connectionpage-window"||args[0]=="--connectionpage-input-headless")) {ReplicaPlatform.Configure<ConnectionPageProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--advancedconnection"||args[0]=="--advancedconnection-window")) {ReplicaPlatform.Configure<AdvancedConnectionProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--dbmspicker"||args[0]=="--dbmspicker-window")) {ReplicaPlatform.Configure<DbmsPickerProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--reviewpage"||args[0]=="--reviewpage-window")) {ReplicaPlatform.Configure<ReviewPageProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--choice"||args[0]=="--choice-window")) {ReplicaPlatform.Configure<ChoiceProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--savepage"||args[0]=="--savepage-window")) {ReplicaPlatform.Configure<SavePageProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--summarylist") {ReplicaPlatform.Configure<SummaryListProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--summaryitem") {ReplicaPlatform.Configure<SummaryItemProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--inlinetext") {ReplicaPlatform.Configure<InlineTextProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--pageheading"||args[0]=="--pageheading-window")) {ReplicaPlatform.Configure<PageHeadingProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--pageheadingsemantic"||args[0]=="--pageheadingsemantic-window")) {ReplicaPlatform.Configure<PageHeadingSemanticsProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--actionbuttonsemantic"||args[0]=="--actionbuttonsemantic-window")) {ReplicaPlatform.Configure<ActionButtonSemanticsProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--headingsemantics"||args[0]=="--headingsemantics-window")) {ReplicaPlatform.Configure<HeadingSemanticsProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--saveoptions"||args[0]=="--saveoptions-window")) {ReplicaPlatform.Configure<SaveOptionsProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--downloadoption"||args[0]=="--downloadoption-window")) {ReplicaPlatform.Configure<DownloadOptionProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--resultdetails"||args[0]=="--resultdetails-window")) {ReplicaPlatform.Configure<ResultDetailsProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--detailsbody") {ReplicaPlatform.Configure<ResultDetailsBodyProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--bulletlist") {ReplicaPlatform.Configure<BulletListProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--trialpreparation"||args[0]=="--trialpreparation-window")) {ReplicaPlatform.Configure<TrialPreparationProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--prepareddb") {ReplicaPlatform.Configure<PreparedDbProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--matchingdialog"||args[0]=="--matchingdialog-window")) {ReplicaPlatform.Configure<MatchingDialogProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--matchoption-timing") {MatchOptionTimingProbe.Run(args[1]);return;}
        if(args.Length>0 && (args[0]=="--matchoption"||args[0]=="--matchoption-window")) {ReplicaPlatform.Configure<MatchOptionProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--categorysection-timing") {CategorySectionTimingProbe.Run(args[1],args[2]);return;}
        if(args.Length>0 && (args[0]=="--categorysection"||args[0]=="--categorysection-window")) {ReplicaPlatform.Configure<CategorySectionProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--connectionpanel"||args[0]=="--connectionpanel-window")) {ReplicaPlatform.Configure<ConnectionPanelProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--topbar"||args[0]=="--topbar-window")) {ReplicaPlatform.Configure<TopBarProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--sidebar"||args[0]=="--sidebar-window")) {ReplicaPlatform.Configure<SidebarProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--actionfooter"||args[0]=="--actionfooter-window")) {ReplicaPlatform.Configure<ActionFooterProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--scroll-glyph-fit") {ScrollGlyphFit.Run(args[1]);return;}
        if(args.Length>0 && (args[0]=="--scrollviewport"||args[0]=="--scrollviewport-window"||args[0]=="--scrollviewport-boundaries"||args[0]=="--scrollviewport-minimum")) {ReplicaPlatform.Configure<ScrollViewportProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--resultchart-lifecycle"||args[0]=="--resultchart-window")){ReplicaPlatform.Configure<ResultChartLifecycleApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--resultchart") {ReplicaPlatform.Configure<ResultChartProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--resultrow"||args[0]=="--resultrow-window"||args[0]=="--resultrow-lifecycle"))
        {ReplicaPlatform.Configure<ResultRowProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--resultcount")
        {ReplicaPlatform.Configure<ResultCountProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--tablerow-timing"){TableRowTimingProbe.Run(args[1]);return;}
        if(args.Length>0 && (args[0]=="--tablerow"||args[0]=="--tablerow-window"))
        {ReplicaPlatform.Configure<TableMappingRowProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--resultrow-timing") {ResultRowTimingProbe.Run(args[1]);return;}
        if(args.Length>0 && args[0]=="--categoryheader-timing") {CategoryHeaderTimingProbe.Run(args[1]);return;}
        if(args.Length>0 && (args[0]=="--categoryheader"||args[0]=="--categoryheader-window"))
        {ReplicaPlatform.Configure<CategoryHeaderProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--field"||args[0]=="--field-window"))
        {ReplicaPlatform.Configure<FieldProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--dbmsoption-lifecycle")
        {ReplicaPlatform.Configure<DbmsOptionLifecycleProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--dbmsoption-window")
        {ReplicaPlatform.Configure<DbmsOptionInputProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--dbmsoption-timing")
        {DbmsOptionTimingProbe.Run(args[1]);return;}
        if(args.Length>0 && args[0]=="--dbmsoption")
        {ReplicaPlatform.Configure<DbmsOptionProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--notice-lifecycle")
        {ReplicaPlatform.Configure<NoticeLifecycleProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--notice")
        {ReplicaPlatform.Configure<NoticeProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--sidebaritem-timing")
        {SidebarItemTimingProbe.Run(args[1]);return;}
        if(args.Length>0 && (args[0]=="--sidebaritem"||args[0]=="--sidebaritem-window"))
        {ReplicaPlatform.Configure<SidebarItemProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--stepbadge")
        {ReplicaPlatform.Configure<StepBadgeProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--searchinput" || args[0]=="--searchinput-window"))
        {ReplicaPlatform.Configure<SearchInputProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && (args[0]=="--iconbutton" || args[0]=="--iconbutton-window"))
        {ReplicaPlatform.Configure<IconButtonProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--statuspill")
        {ReplicaPlatform.Configure<StatusPillProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--number-glyph-fit")
        {NumberGlyphFit.Run(args[1]);return;}
        if(args.Length>0 && args[0]=="--number-rules")
        {NumberRulesProbe.Run(args[1],args[2]);return;}
        if(args.Length>0 && args[0]=="--number-window")
        {ReplicaPlatform.Configure<NumberInputProbeApplication>().StartWithClassicDesktopLifetime(args);return;}
        if(args.Length>0 && args[0]=="--textinput-window")
        { ReplicaPlatform.Configure<TextInputProbeApplication>().StartWithClassicDesktopLifetime(args);return; }
        if(args.Length>0 && args[0]=="--radio-window")
        { ReplicaPlatform.Configure<RadioProbeApplication>().StartWithClassicDesktopLifetime(args);return; }
        if(args.Length>0 && args[0]=="--checkbox-window")
        { ReplicaPlatform.Configure<CheckboxProbeApplication>().StartWithClassicDesktopLifetime(args);return; }
        if(args.Length>0 && args[0]=="--spinner-window")
        { ReplicaPlatform.Configure<SpinnerProbeApplication>().StartWithClassicDesktopLifetime(args);return; }
        if(args.Length>0 && (args[0]=="--action-window" || args[0]=="--action-busy-window"))
        {
            ReplicaPlatform.Configure<ActionButtonProbeApplication>().StartWithClassicDesktopLifetime(args);return;
        }
        if(args.Length==0 && File.Exists(Path.Combine(AppContext.BaseDirectory,"motion-window.txt")))
            args=new[]{"--motion-window",AppContext.BaseDirectory};
        if(args.Length>0 && args[0]=="--motion-window")
        {
            ReplicaPlatform.Configure<MotionProbeApplication>().StartWithClassicDesktopLifetime(args);
            return;
        }
        if(args.Length==0 && File.Exists(Path.Combine(AppContext.BaseDirectory,"typography-cases.json")))
            args=new[]{"--text-window",Path.Combine(AppContext.BaseDirectory,"typography-cases.json"),AppContext.BaseDirectory};
        if(args.Length==0 && File.Exists(Path.Combine(AppContext.BaseDirectory,"surface-cases.json")))
            args=new[]{"--surface-window",Path.Combine(AppContext.BaseDirectory,"surface-cases.json"),AppContext.BaseDirectory};
        if(args[0]=="--surface-window")
        {
            ReplicaPlatform.Configure<SurfaceProbeApplication>().StartWithClassicDesktopLifetime(args);
            return;
        }
        if(args[0]=="--text-window")
        {
            ReplicaPlatform.Configure<TextProbeApplication>().StartWithClassicDesktopLifetime(args);
            return;
        }
        ReplicaPlatform.Configure<Application>().SetupWithoutStarting();
        if(args[0]=="--unicode-breaks")
        {
            var inputs=JsonSerializer.Deserialize<TextStateCase[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
            var unicodeRows=new List<object>();foreach(var c in inputs){var breaks=new List<int>();var it=new Avalonia.Media.TextFormatting.Unicode.LineBreakEnumerator(c.Text.AsSpan());while(it.MoveNext(out var b))breaks.Add(b.PositionWrap);unicodeRows.Add(new{id=c.Id,c.Text,breaks});}
            File.WriteAllText(args[2],JsonSerializer.Serialize(unicodeRows,new JsonSerializerOptions{WriteIndented=true}));return;
        }
        if(args[0]=="--number-spin-font")
        {
            var face=new Typeface(new FontFamily("Segoe Fluent Icons")).GlyphTypeface;
            var glyph=face.CharacterToGlyphMap.GetGlyph('\ueddb');
            using var sk=SkiaSharp.SKTypeface.FromFamilyName("Segoe Fluent Icons");
            Console.WriteLine(JsonSerializer.Serialize(new{family=face.FamilyName,glyph,metrics=face.Metrics,skiaFamily=sk.FamilyName,skiaGlyph=sk.GetGlyph('\ueddb')}));
            if(args.Length>1)
            {
                Directory.CreateDirectory(args[1]);var samples=new List<object>();var index=0;
                foreach(var size in new[]{5d,5.25,5.5,5.75,6})
                foreach(var baseline in new[]{7.5,8,8.25,8.5,8.75})
                {
                    using var bitmap=new SkiaSharp.SKBitmap(30,22);using var canvas=new SkiaSharp.SKCanvas(bitmap);canvas.Clear(new SkiaSharp.SKColor(44,44,44));canvas.Scale(2);
                    using var font=new SkiaSharp.SKFont(sk,(float)size){Edging=SkiaSharp.SKFontEdging.Antialias,Subpixel=true};
                    using var paint=new SkiaSharp.SKPaint{IsAntialias=true,Color=new SkiaSharp.SKColor(209,209,209)};
                    using var blob=SkiaSharp.SKTextBlob.Create("\ueddb",font);canvas.DrawText(blob,(float)((15-size)/2),(float)baseline,paint);
                    using var image=SkiaSharp.SKImage.FromBitmap(bitmap);using var data=image.Encode(SkiaSharp.SKEncodedImageFormat.Png,100);var name="fluent-sample-"+index+++".png";using(var file=File.Create(Path.Combine(args[1],name)))data.SaveTo(file);
                    samples.Add(new{name,size,baseline});
                }
                File.WriteAllText(Path.Combine(args[1],"fluent-samples.json"),JsonSerializer.Serialize(samples));
            }
            return;
        }
        if(args[0]=="--textinput-timing")
        {TextInputTimingProbe.Run(args[1]);return;}
        if(args[0]=="--editor-font-inspect")
        {
            foreach(var family in new[]{".AppleSystemUIFont",".SF NS","Helvetica"})
            {
                var face=new Typeface(new FontFamily(family)).GlyphTypeface;
                var layout=new TextLayout("source.example.invalid",new Typeface(new FontFamily(family)),14,Brushes.White);
                using var sk=SkiaSharp.SKTypeface.FromFamilyName(family);using var f=new SkiaSharp.SKFont(sk,14);
                Console.WriteLine(JsonSerializer.Serialize(new{family,resolved=face.FamilyName,avaloniaWidth=layout.WidthIncludingTrailingWhitespace,skiaWidth=f.MeasureText("source.example.invalid")}));
            }
            var collection=new Avalonia.Media.Fonts.EmbeddedFontCollection(new Uri("fonts:EditorDiagnostic"),new Uri("file:///System/Library/Fonts/SFNS.ttf"));
            FontManager.Current.AddFontCollection(collection);
            foreach(var family in collection)
            {
                var font=new FontFamily("fonts:EditorDiagnostic#"+family.Name);
                var layout=new TextLayout("source.example.invalid",new Typeface(font),14,Brushes.White);
                Console.WriteLine(JsonSerializer.Serialize(new{family=family.Name,resolved=new Typeface(font).GlyphTypeface.FamilyName,avaloniaWidth=layout.WidthIncludingTrailingWhitespace}));
            }
            return;
        }
        if(args[0]=="--textinput") {TextInputProbe.Run(args[1],args[2]);return;}
        if(args[0]=="--checkbox-timing") { CheckboxProbe.Timing(args[1]);return; }
        if(args[0]=="--radio-timing") { RadioProbe.Timing(args[1]);return; }
        if(args[0]=="--radio") { RadioProbe.Run(args[1],args[2]);return; }
        if(args[0]=="--checkbox") { CheckboxProbe.Run(args[1],args[2]);return; }
        if(args[0]=="--spinner") { SpinnerProbe.Run(args[1],args[2]);return; }
        if(args[0]=="--action-buttons") { ActionButtonProbe.Run(args[1],args[2]);return; }
        if(args[0]=="--action-states") { ActionButtonProbe.States(args[1],args[2]);return; }
        if(args[0]=="--action-timing") { ActionButtonProbe.Timing(args[1]);return; }
        if(args[0]=="--motion") { MotionProbe.Run(args[1],args.Length>2&&args[2]=="integer");return; }
        if(args[0]=="--motion-playback") { MotionPlaybackProbe.Run(args[1]);return; }
        if(args[0]=="--motion-time-api") { MotionEngineInspect.Run();return; }
        if(args[0]=="--motion-curves")
        {
            var curves=new[]{("custom",WebMotion.SourceEase()),("ease",WebMotion.CssEase()),("ease-out",WebMotion.EaseOut()),("ease-in-out",WebMotion.EaseInOut())};
            File.WriteAllText(args[1],JsonSerializer.Serialize(curves.Select(c=>new {name=c.Item1,samples=Enumerable.Range(0,101).Select(i=>new {progress=i/100d,value=c.Item2.Ease(i/100d)})}),new JsonSerializerOptions{WriteIndented=true}));return;
        }
        if(args[0]=="--text-phase") { TextPhaseProbe.Run(args[1],args[2]);return; }
        if(args[0]=="--motion-api")
        {
            foreach(var type in new[]{typeof(Avalonia.Animation.Animation).Assembly.GetType("Avalonia.Animation.IClock")!,typeof(Avalonia.Animation.Animation).Assembly.GetType("Avalonia.Animation.ClockBase")!,typeof(Avalonia.Animation.Animation).Assembly.GetType("Avalonia.Animation.Clock")!,typeof(Avalonia.Animation.Easings.SplineEasing),typeof(Avalonia.Media.Transformation.TransformOperations)})
                Console.WriteLine(type+"\n"+string.Join('\n',type.GetMembers(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)));
            return;
        }
        if(args[0] is "--coretext" or "--coretext-render" or "--coretext-tables")
        {
            CoreTextMeasure.Run(args[1],args[2],args[0]=="--coretext-render",args[0]=="--coretext-tables");return;
        }
        if(args[0]=="--harfbuzz-text")
        {
            HarfBuzzMeasure.Run(args[1],args[2]);return;
        }
        if(args[0]=="--skia-system")
        {
            foreach(var name in new[]{".AppleSystemUIFont",".SFNS-Bold",".SF NS","SF Pro Text","SF Pro Display","system-ui"})
            {
                using var face=SkiaSharp.SKTypeface.FromFamilyName(name,SkiaSharp.SKFontStyle.Bold);
                using var font=new SkiaSharp.SKFont(face,18);
                Console.WriteLine(JsonSerializer.Serialize(new {name,resolved=face.FamilyName,width=font.MeasureText("Source DB")}));
            }
            return;
        }
        if(args[0]=="--font-api")
        {
            foreach(var type in new[]{typeof(SkiaSharp.SKTypeface),typeof(Avalonia.Skia.SkiaPlatform).Assembly.GetType("Avalonia.Skia.SkiaTypeface")!})
                Console.WriteLine(type+" public="+type.IsPublic+"\n"+string.Join('\n',type.GetMembers().Where(m=>m.Name.Contains("Variation")||m.Name.Contains("Clone")||m.Name.Contains("Create")||m.MemberType==System.Reflection.MemberTypes.Constructor)));
            return;
        }
        if(args[0]=="--hb-api")
        {
            Console.WriteLine(typeof(HarfBuzzSharp.GetTableDelegate).GetMethod("Invoke"));
            foreach(var type in new[]{typeof(HarfBuzzSharp.Font),typeof(HarfBuzzSharp.Face),typeof(HarfBuzzSharp.Blob),typeof(HarfBuzzSharp.Variation),typeof(HarfBuzzSharp.Tag)})
                Console.WriteLine(type+"\n"+string.Join('\n',type.GetMembers().Where(m=>m.Name.Contains("Variat")||m.Name.Contains("Scale")||m.Name.Contains("Point")||m.MemberType==System.Reflection.MemberTypes.Constructor)));
            return;
        }
        if(args[0]=="--font-metrics")
        {
            var fontCases=JsonSerializer.Deserialize<TextCase[]>(File.ReadAllText(args[1]),new JsonSerializerOptions {PropertyNameCaseInsensitive=true})!;
            var metrics=fontCases.Select(c=>
            {
                var family=c.Family=="mono"?WebTypography.MonospaceFontFamily:WebTypography.SystemFontFamily;
                var face=new Typeface(family,weight:(FontWeight)c.Weight).GlyphTypeface;
                using var skFace=SkiaSharp.SKTypeface.FromFamilyName(family.Name,new SkiaSharp.SKFontStyle(c.Weight,5,SkiaSharp.SKFontStyleSlant.Upright));
                using var skFont=new SkiaSharp.SKFont(skFace,(float)c.Size);
                return new {c.Id,c.Size,c.LineHeight,c.Weight,requested=family.Name,resolved=face.FamilyName,metrics=face.Metrics,skia=skFont.Metrics};
            });
            Console.WriteLine(JsonSerializer.Serialize(metrics,new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));return;
        }
        if(args[0]=="--fonts")
        {
            var names=new[]{FontFamily.Default.Name,".AppleSystemUIFont",".SFNS-Regular",".SFNS-Bold",".SF NS",".SF NS Text",".SF NS Display","SF Pro Text","SF Pro Display","Arial","Segoe UI","Apple SD Gothic Neo"};
            var fonts=names.SelectMany(name=>new[]{400,600,650,700}.Select(weight=>
            {
                var face=new Typeface(new FontFamily(name),weight:(FontWeight)weight).GlyphTypeface;
                return new { requested=name,weight,resolved=face.FamilyName,resolvedWeight=(int)face.Weight,simulations=face.FontSimulations.ToString(),glyphCount=face.GlyphCount,latin=face.CharacterToGlyphMap.TryGetGlyph('A',out var latin),latinGlyph=latin,korean=face.CharacterToGlyphMap.TryGetGlyph('가',out var korean),koreanGlyph=korean };
            })).ToArray();
            Console.WriteLine(JsonSerializer.Serialize(new { defaultFont=FontManager.Current.DefaultFontFamily.Name, installed=FontManager.Current.SystemFonts.Select(f=>f.Name).Where(n=>n.Contains("SF")||n.Contains("System")||n.Contains("Gothic")), fonts },new JsonSerializerOptions { WriteIndented=true }));
            return;
        }
        if(args[0]=="--surface-children")
        {
            RenderSurfaceChildren(args[1],args[2]);return;
        }
        if(args[0] is "--text" or "--text-candidate")
        {
            RenderText(args[1],args[2],args[0]=="--text-candidate");
            return;
        }
        if(args[0]=="--text-states")
        {
            RenderTextStates(args[1],args[2]);return;
        }
        if(args[0] is "--surface" or "--surface-window")
        {
            RenderSurface(args[1],args[2],args[0]=="--surface-window");
            return;
        }
        var cases=JsonSerializer.Deserialize<IconCase[]>(File.ReadAllText(args[0]),
            new JsonSerializerOptions { PropertyNameCaseInsensitive=true })!;
        var rows=(cases.Length+9)/10;
        var grid=new Grid { Width=640,Height=rows*64,Background=Brush.Parse("#313338"),
            ColumnDefinitions=new(string.Join(',',Enumerable.Repeat("64",10))),
            RowDefinitions=new(string.Join(',',Enumerable.Repeat("64",rows))) };
        foreach(var (item,index) in cases.Select((c,i)=>(c,i)))
        {
            var icon=new SvgIcon { IconName=item.Name,Width=item.Width,Height=item.Height,
                StrokeWidth=item.StrokeWidth,HorizontalAlignment=HorizontalAlignment.Center,
                VerticalAlignment=VerticalAlignment.Center,IsVisible=item.Visible };
            if(!item.Inherited)icon.Stroke=Brush.Parse(item.Color);
            if(item.Rotation!=0)icon.RenderTransform=new RotateTransform(item.Rotation);
            var parent=new Border { Child=icon,Opacity=item.Opacity };
            TextElement.SetForeground(parent,Brush.Parse(item.Color));
            Grid.SetRow(parent,index/10);Grid.SetColumn(parent,index%10);grid.Children.Add(parent);
        }
        grid.Measure(new(640,rows*64));grid.Arrange(new(0,0,640,rows*64));
        var measured=grid.Children.OfType<Border>().Select((parent,index)=>
        {
            var icon=(SvgIcon)parent.Child!;
            var corners=new[]{default(Point),new Point(icon.Bounds.Width,0),new Point(0,icon.Bounds.Height),new Point(icon.Bounds.Width,icon.Bounds.Height)}
                .Select(point=>icon.TranslatePoint(point,grid)??default).ToArray();
            var origin=new Point(corners.Min(p=>p.X),corners.Min(p=>p.Y));
            return new { id=cases[index].Id, x=origin.X, y=origin.Y, width=icon.Bounds.Width,
                height=icon.Bounds.Height, visible=icon.IsVisible, foreground=TextElement.GetForeground(icon)?.ToString(),
                stroke=icon.Stroke?.ToString(), strokeWidth=icon.StrokeWidth, focusable=icon.Focusable, hitTest=icon.IsHitTestVisible };
        });
        File.WriteAllText(Path.Combine(args[1],"svg-native-layout.json"),JsonSerializer.Serialize(measured,new JsonSerializerOptions { WriteIndented=true }));
        foreach(var scale in new[]{1,2})
        {
            using var bitmap=new RenderTargetBitmap(new PixelSize(640*scale,rows*64*scale),new Vector(96*scale,96*scale));
            bitmap.Render(grid);using var output=File.Create(Path.Combine(args[1],$"svg-atlas-native-{scale}x.png"));
            bitmap.Save(output, new PngBitmapEncoderOptions());
        }
        Console.WriteLine($"Rendered {cases.Length} cases using actual Avalonia SvgIcon at 1× and 2×.");
    }
    private static Grid RenderText(string input,string outputDirectory,bool candidate=false)
    {
        var cases=JsonSerializer.Deserialize<TextCase[]>(File.ReadAllText(input),new JsonSerializerOptions { PropertyNameCaseInsensitive=true })!;
        var height=((cases.Length+1)/2)*130;
        var grid=new Grid { Width=1000,Height=height,Background=Brush.Parse("#313338"),
            ColumnDefinitions=new("500,500"),RowDefinitions=new(string.Join(',',Enumerable.Repeat("130",height/130))) };
        foreach(var (item,index) in cases.Select((c,i)=>(c,i)))
        {
            Control text=candidate?WebTypography.CreateCandidate(item.Text,item.Size,item.LineHeight,Brush.Parse(item.Color),item.Weight,item.Spacing,item.Family=="mono",item.Tabular):WebTypography.Create(item.Text,item.Size,item.LineHeight,Brush.Parse(item.Color),item.Weight,item.Spacing);
            if(text is TextBlock block && Environment.GetEnvironmentVariable("REPLICA_FONT") is { Length: >0 } family)block.FontFamily=new FontFamily(family);
            text.Width=480;
            if(text is TextBlock original) _ = new Typeface(original.FontFamily, weight:original.FontWeight).GlyphTypeface;
            var slot=new Border { Padding=new Thickness(10),Child=text };
            Grid.SetColumn(slot,index%2);Grid.SetRow(slot,index/2);grid.Children.Add(slot);
        }
        grid.Measure(new(1000,height));grid.Arrange(new(0,0,1000,height));
        if(candidate)
        {
            var measurements=grid.Children.OfType<Border>().Select((parent,index)=>
            {
                var text=(WebSystemTextBlock)parent.Child!;var p=text.TranslatePoint(default,grid)??default;
                return new {id=cases[index].Id,x=p.X,y=p.Y,width=text.Bounds.Width,height=text.Bounds.Height,baseline=text.SystemBaseline,lines=text.SystemLineWidths};
            });
            WriteTextEvidence(outputDirectory,"typography-candidate-layout.json",JsonSerializer.Serialize(measurements,new JsonSerializerOptions {WriteIndented=true}));
            foreach(var scale in new[]{1,2})
            {
                using var raster=new RenderTargetBitmap(new PixelSize(1000*scale,height*scale),new Vector(96*scale,96*scale));
                raster.Render(grid);using var file=File.Create(Path.Combine(outputDirectory,$"typography-candidate-{scale}x.png"));raster.Save(file,new PngBitmapEncoderOptions());
            }
            Console.WriteLine("Rendered actual Avalonia typography candidate at 1x/2x.");return grid;
        }
        var measured=grid.Children.OfType<Border>().Select((parent,index)=>
        {
            var text=(TextBlock)parent.Child!;var origin=text.TranslatePoint(default,grid)??default;
            return new { id=cases[index].Id,x=origin.X,y=origin.Y,width=text.Bounds.Width,height=text.Bounds.Height,
                fontFamily=text.FontFamily.Name,fontSize=text.FontSize,lineHeight=text.LineHeight,weight=(int)text.FontWeight,spacing=text.LetterSpacing,
                lines=text.TextLayout.TextLines.Select(line=>new {line.Length,line.Width,line.WidthIncludingTrailingWhitespace,line.Height,line.Baseline,
                    runs=line.TextRuns.OfType<ShapedTextRun>().Select(run=>new { text=run.Text.ToString(),requested=run.Properties.Typeface.FontFamily.Name,requestedWeight=(int)run.Properties.Typeface.Weight,family=run.GlyphRun.GlyphTypeface.FamilyName,weight=(int)run.GlyphRun.GlyphTypeface.Weight,simulations=run.GlyphRun.GlyphTypeface.FontSimulations.ToString() })}) };
        });
        File.WriteAllText(Path.Combine(outputDirectory,"typography-native-layout.json"),JsonSerializer.Serialize(measured,new JsonSerializerOptions { WriteIndented=true }));
        using var bitmap=new RenderTargetBitmap(new PixelSize(1000,height),new Vector(96,96));
        bitmap.Render(grid);using var output=File.Create(Path.Combine(outputDirectory,"typography-native.png"));bitmap.Save(output,new PngBitmapEncoderOptions());
        Console.WriteLine($"Rendered {cases.Length} source-metric typography cases.");
        return grid;
    }
    private sealed record TextCase(string Id,string Text,double Size,double LineHeight,int Weight,double Spacing,string Color,string Family="system",bool Tabular=false);
    private static void RenderTextStates(string input,string outputDirectory)
    {
        var cases=JsonSerializer.Deserialize<TextStateCase[]>(File.ReadAllText(input),new JsonSerializerOptions {PropertyNameCaseInsensitive=true})!;
        var text=WebTypography.CreateCandidate("",14,22.4);
        var cell=new Border {Width=500,Height=220,Padding=new Thickness(10),Child=text,Background=Brush.Parse("#313338"),UseLayoutRounding=false};
        text.HorizontalAlignment=HorizontalAlignment.Left;
        var measurements=new List<object>();
        foreach(var c in cases)
        {
            // Reuse one control to verify property invalidation/cache replacement, not just initial creation.
            text.Text=c.Text;text.FontSize=c.Size;text.LineHeight=c.LineHeight;text.FontWeight=(FontWeight)c.Weight;
            text.LetterSpacing=c.Spacing;text.Foreground=Brush.Parse(c.Color);text.Width=c.Width;
            text.FontFamily=c.Family=="mono"?WebTypography.MonospaceFontFamily:WebTypography.SystemFontFamily;
            text.TabularNumbers=c.Tabular;
            text.TextWrapping=c.Wrap switch {"anywhere"=>TextWrapping.Wrap,"nowrap"=>TextWrapping.NoWrap,_=>TextWrapping.WrapWithOverflow};
            text.TextAlignment=c.Align switch {"center"=>TextAlignment.Center,"right"=>TextAlignment.Right,_=>TextAlignment.Left};
            var invalidated=!text.IsMeasureValid;
            text.Measure(new(c.Width,double.PositiveInfinity));
            cell.Height=Math.Max(220,text.DesiredSize.Height+20);
            // The detached probe has no LayoutManager to schedule a new root pass.
            cell.InvalidateMeasure();
            cell.Measure(new(500,cell.Height));cell.Arrange(new(0,0,500,cell.Height));
            var origin=text.TranslatePoint(default,cell)??default;
            measurements.Add(new {c.Id,invalidated,x=origin.X,y=origin.Y,width=text.Bounds.Width,height=text.Bounds.Height,lines=text.SystemLineWidths,foreground=text.Foreground?.ToString(),alignment=text.TextAlignment.ToString(),
                nativeRuns=(text.Child as TextBlock)?.TextLayout.TextLines.Select(line=>line.TextRuns.OfType<ShapedTextRun>().Select(run=>new {text=run.Text.ToString(),requested=run.Properties.Typeface.FontFamily.Name,requestedWeight=(int)run.Properties.Typeface.Weight,family=run.GlyphRun.GlyphTypeface.FamilyName,weight=(int)run.GlyphRun.GlyphTypeface.Weight,simulations=run.GlyphRun.GlyphTypeface.FontSimulations.ToString()}))});
            using var bitmap=new RenderTargetBitmap(new PixelSize(1000,440),new Vector(192,192));bitmap.Render(cell);
            using var file=File.Create(Path.Combine(outputDirectory,$"typography-{c.Id}-2x.png"));bitmap.Save(file,new PngBitmapEncoderOptions());
        }
        File.WriteAllText(Path.Combine(outputDirectory,"typography-state-native.json"),JsonSerializer.Serialize(measurements,new JsonSerializerOptions {WriteIndented=true}));
        Console.WriteLine($"Rendered {cases.Length} property states on the same Avalonia candidate.");
    }
    private sealed record TextStateCase(string Id,string Text,double Size,double LineHeight,int Weight,double Spacing,string Color,double Width,string Align,string Family="system",bool Tabular=false,string Wrap="normal");
    private static void RenderSurface(string input,string outputDirectory,bool showWindow=false)
    {
        var cases=JsonSerializer.Deserialize<SurfaceCase[]>(File.ReadAllText(input),new JsonSerializerOptions { PropertyNameCaseInsensitive=true })!;
        var height=((cases.Length+2)/3)*100;
        var grid=new Grid { Width=720,Height=height,Background=Brush.Parse("#313338"),ColumnDefinitions=new("240,240,240"),RowDefinitions=new(string.Join(',',Enumerable.Repeat("100",height/100))) };
        foreach(var (c,index) in cases.Select((c,i)=>(c,i)))
        {
            var shadow=new BoxShadow {Color=Color.Parse(c.ShadowColor),OffsetX=c.ShadowX,OffsetY=c.ShadowY,Blur=c.Blur,Spread=c.Spread,IsInset=c.Inset};
            var paint=WebSurface.Create(Brush.Parse(c.Background),Brush.Parse(c.Border),new(c.Left,c.Top,c.Right,c.Bottom),new(c.Radius[0],c.Radius[1],c.Radius[2],c.Radius[3]),new BoxShadows(shadow));
            paint.Width=c.Width;paint.Height=c.Height;paint.HorizontalAlignment=HorizontalAlignment.Center;paint.VerticalAlignment=VerticalAlignment.Center;
            var slot=new Border { Child=paint,UseLayoutRounding=false };
            Grid.SetRow(slot,index/3);Grid.SetColumn(slot,index%3);grid.Children.Add(slot);
        }
        grid.Measure(new(720,height));grid.Arrange(new(0,0,720,height));
        var measured=grid.Children.OfType<Border>().Select((slot,index)=>
        {
            var paint=(WebSurfaceBox)slot.Child!;var p=paint.TranslatePoint(default,grid)??default;
            return new {id=cases[index].Id,x=p.X,y=p.Y,width=paint.Bounds.Width,height=paint.Bounds.Height,
                background=paint.Background?.ToString(),border=paint.BorderBrush?.ToString(),thickness=paint.BorderThickness.ToString(),radius=paint.CornerRadius.ToString(),shadow=paint.BoxShadow.ToString()};
        });
        File.WriteAllText(Path.Combine(outputDirectory,"surface-native-layout.json"),JsonSerializer.Serialize(measured,new JsonSerializerOptions { WriteIndented=true }));
        foreach(var scale in new[]{1,2})
        {
            using var bitmap=new RenderTargetBitmap(new PixelSize(720*scale,height*scale),new Vector(96*scale,96*scale));
            bitmap.Render(grid);using var output=File.Create(Path.Combine(outputDirectory,$"surface-native-{scale}x.png"));bitmap.Save(output,new PngBitmapEncoderOptions());
        }
        Console.WriteLine($"Rendered {cases.Length} observed Surface paint cases.");
        if(showWindow)
        {
            var window=new Window { Title="Replica Surface verification",Width=720,Height=height,CanResize=false,Content=grid,UseLayoutRounding=false };
            window.Opened+=(_,_)=>{ WebSurfaceColorSpace.Apply(window);Dispatcher.UIThread.Post(()=>
                File.WriteAllText(Path.Combine(outputDirectory,"surface-native-window.json"),JsonSerializer.Serialize(new {clientWidth=window.ClientSize.Width,clientHeight=window.ClientSize.Height,scaling=window.RenderScaling,gridWidth=grid.Bounds.Width,gridHeight=grid.Bounds.Height},new JsonSerializerOptions {WriteIndented=true})),DispatcherPriority.Background);};
            ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow=window;
        }
    }
    private static void RenderSurfaceChildren(string input,string outputDirectory)
    {
        var cases=JsonSerializer.Deserialize<SurfaceChildCase[]>(File.ReadAllText(input),new JsonSerializerOptions { PropertyNameCaseInsensitive=true })!;
        var parent=new StackPanel();
        foreach(var c in cases)
        {
            var surface=WebSurface.Create(Brushes.Transparent,Brushes.Black,new(c.Border[3],c.Border[0],c.Border[1],c.Border[2]),default);
            surface.Width=c.Width;surface.Height=c.Height;surface.HorizontalAlignment=HorizontalAlignment.Left;
            surface.Padding=new(c.Padding[3],c.Padding[0],c.Padding[1],c.Padding[2]);
            surface.Child=new Border { Width=18,Height=18,HorizontalAlignment=c.Center?HorizontalAlignment.Center:HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center };
            parent.Children.Add(surface);
        }
        parent.Measure(new(1200,1000));parent.Arrange(new(0,0,1200,1000));
        var result=parent.Children.Cast<WebSurfaceBox>().Select((p,i)=>{var child=p.Child!;var point=child.TranslatePoint(default,parent)??default;return new{id=cases[i].Id,x=point.X,y=point.Y,width=child.Bounds.Width,height=child.Bounds.Height};});
        File.WriteAllText(Path.Combine(outputDirectory,"surface-child-native.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions {WriteIndented=true}));
    }
    private sealed record SurfaceChildCase(string Id,double Width,double Height,double[] Padding,double[] Border,bool Center);
    private sealed class SurfaceProbeApplication : Application
    {
        public override void OnFrameworkInitializationCompleted()
        {
            var lifetime=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
            RenderSurface(lifetime.Args![1],lifetime.Args[2],true);
            base.OnFrameworkInitializationCompleted();
        }
    }
    private sealed class TextProbeApplication : Application
    {
        public override void OnFrameworkInitializationCompleted()
        {
            var lifetime=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
            var args=lifetime.Args!;
            var grid=RenderText(args[1],args[2],true);
            // Accessible keyboard target belongs to this development fixture only.
            ((Border)grid.Children[0]).Child!.Focusable=true;
            Avalonia.Automation.AutomationProperties.SetAutomationId(((Border)grid.Children[0]).Child!,"ReplicaKeyboardTarget");
            var canvas=new Canvas {Width=1000,Height=Math.Max(1040,grid.Height),Background=Brush.Parse("#313338"),UseLayoutRounding=false,VerticalAlignment=VerticalAlignment.Top,HorizontalAlignment=HorizontalAlignment.Left};
            canvas.Children.Add(grid);
            // This unthemed fixture shows the first atlas viewport directly.
            // A template-less ScrollViewer had centered the tall canvas instead
            // of showing its beginning; that was not a text rendering result.
            var window=new Window {Title="Replica Typography verification",Width=1000,Height=1040,CanResize=true,WindowStartupLocation=WindowStartupLocation.CenterScreen,Content=canvas,UseLayoutRounding=false};
            var cases=JsonSerializer.Deserialize<TextCase[]>(File.ReadAllText(args[1]),new JsonSerializerOptions {PropertyNameCaseInsensitive=true})!;
            window.KeyDown+=(_,e)=>
            {
                if(e.Key is not (Key.N or Key.R or Key.P or Key.D))return;
                var slot=(Border)grid.Children[0];
                var text=(WebSystemTextBlock)slot.Child!;
                if(e.Key==Key.P)
                {
                    Canvas.SetLeft(grid,0.125);Canvas.SetTop(grid,0.125);
                    window.Title="Replica Typography verification · parent-phase";
                    return;
                }
                // D observes a previous painted frame; it does not force a render or modify the control.
                if(e.Key==Key.D)
                {
                    var key=typeof(WebSystemTextBlock).GetField("_rasterKey",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(text);
                    var phase=key?.GetType().GetProperty("Phase")?.GetValue(key);
                    File.WriteAllText(Path.Combine(args[2],"typography-live-phase.json"),JsonSerializer.Serialize(new {parentX=grid.Bounds.X,parentY=grid.Bounds.Y,childX=text.Bounds.X,childY=text.Bounds.Y,paintedPhase=phase},new JsonSerializerOptions {WriteIndented=true}));
                    return;
                }
                if(e.Key==Key.R) {Canvas.SetLeft(grid,0);Canvas.SetTop(grid,0);}
                var c=e.Key==Key.N?cases.FirstOrDefault(c=>c.Id=="kr-3") ?? cases.FirstOrDefault(c=>c.Family=="mono") ?? cases[0]:cases[0];
                text.Text=c.Text;text.FontSize=c.Size;text.LineHeight=c.LineHeight;text.FontWeight=(FontWeight)c.Weight;
                text.LetterSpacing=c.Spacing;text.Foreground=Brush.Parse(c.Color);text.Width=e.Key==Key.N?240:480;
                text.FontFamily=c.Family=="mono"?WebTypography.MonospaceFontFamily:WebTypography.SystemFontFamily;
                text.TabularNumbers=c.Tabular;
                text.HorizontalAlignment=HorizontalAlignment.Left;
                // Narrow-text fixture uses the source state oracle's 220px parent row, not the 130px atlas row.
                slot.Height=e.Key==Key.N?220:double.NaN;
                grid.RowDefinitions[0].Height=new GridLength(e.Key==Key.N?220:130);
                var state=e.Key==Key.N?"narrow":"restored";
                window.Title="Replica Typography verification · "+state;
                Dispatcher.UIThread.Post(()=>WriteTextEvidence(args[2],$"typography-live-{state}.json",
                    JsonSerializer.Serialize(new {text.Text,family=text.FontFamily.Name,text.TabularNumbers,width=text.Bounds.Width,height=text.Bounds.Height,desiredHeight=text.DesiredSize.Height,lines=text.SystemLineWidths},new JsonSerializerOptions {WriteIndented=true})),DispatcherPriority.Background);
            };
            window.Opened+=(_,_)=>
            {
                WebSurfaceColorSpace.Apply(window);
                WriteTextEvidence(args[2],"typography-candidate-window.json",JsonSerializer.Serialize(new {width=window.ClientSize.Width,height=window.ClientSize.Height,scale=window.RenderScaling},new JsonSerializerOptions {WriteIndented=true}));
            };
            lifetime.MainWindow=window;
            base.OnFrameworkInitializationCompleted();
        }
    }
    private sealed record SurfaceCase(string Id,double Width,double Height,string Background,string Border,double Left,double Top,double Right,double Bottom,double[] Radius,string ShadowColor,double ShadowX,double ShadowY,double Blur,double Spread,bool Inset);
    private sealed record IconCase(string Id,string Name,double Width,double Height,string Color,
        double StrokeWidth=1.7,int Rotation=0,bool Visible=true,bool Inherited=false,double Opacity=1);
}
