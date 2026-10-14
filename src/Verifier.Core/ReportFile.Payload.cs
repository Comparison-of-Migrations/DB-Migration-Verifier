using System.Security.Cryptography;
using System.Text.Json;
namespace Verifier.Core;

public static partial class ReportFile
{
    // Keep the original envelope hash over its original bytes. Extract only the
    // potentially large Findings/Issues/PairingLimits and full mapping arrays while streaming; deserialize the rest
    // through the same strict serializer. The temporary skeleton is encrypted.
    private static async Task<RunReport> ReadPayload(Stream input)
    {
        var storage=new RunReportStorage();
        try
        {
            using var skeleton=new ProtectedRowStore();var extractor=new FindingExtractor(storage);
            var descriptor=await skeleton.AddAsync(async (output,token)=>
            {
                using var writer=new Utf8JsonWriter(output);var buffer=new byte[65536];var used=0;var state=new JsonReaderState();var first=true;
                try
                {
                    while(true)
                    {
                        if(used==buffer.Length)
                        {
                            // One native metadata record/token, never the whole array.
                            if(buffer.Length>=ExactRow.MaxRowBytes)throw new InvalidDataException();
                            var larger=new byte[buffer.Length*2];buffer.CopyTo(larger,0);CryptographicOperations.ZeroMemory(buffer);buffer=larger;
                        }
                        var read=await input.ReadAsync(buffer.AsMemory(used),token);used+=read;var final=read==0;
                        if(first)
                        {
                            if(used<3&&!final)continue;first=false;
                            if(used>=3&&buffer[0]==239&&buffer[1]==187&&buffer[2]==191){buffer.AsSpan(3,used-3).CopyTo(buffer);used-=3;}
                        }
                        var consumed=extractor.Pump(buffer.AsSpan(0,used),final,ref state,writer);
                        buffer.AsSpan(consumed,used-consumed).CopyTo(buffer);used-=consumed;writer.Flush();
                        if(final){if(used!=0||!extractor.Complete)throw new InvalidDataException();break;}
                    }
                }
                finally{CryptographicOperations.ZeroMemory(buffer);}
            },default);
            skeleton.Flush();using var stream=skeleton.OpenRead(descriptor);
            var report=await JsonSerializer.DeserializeAsync<RunReport>(stream,Options)??throw new InvalidDataException();
            var mappings=extractor.Mappings;
            if(report.Checks==null||report.Checks.Count!=extractor.Findings.Count||report.Mappings==null||report.Mappings.Count!=0||report.Tables==null||report.Tables.Count!=0||stream.ReadByte()!=-1)throw new InvalidDataException();
            return report with{Checks=report.Checks.Select((check,i)=>check with{Findings=extractor.Findings[i]?.Snapshot()??check.Findings,Issues=extractor.Issues[i]?.Snapshot()??check.Issues,PairingLimits=extractor.PairingLimits[i]?.Snapshot()??check.PairingLimits}).ToArray(),
                Tables=extractor.Tables,Mappings=mappings,LocalStorage=storage};
        }
        catch{storage.Dispose();throw;}
    }
    private sealed class FindingExtractor(RunReportStorage storage)
    {
        internal readonly List<FindingBuffer?> Findings=[];
        internal readonly List<IssueBuffer?> Issues=[],PairingLimits=[];
        private readonly MappingBuffer mappings=new(storage);
        private readonly TableNameBuffer tables=new(storage);
        internal IReadOnlyList<string> Tables=>tables.Snapshot();
        internal IReadOnlyList<TableMapping> Mappings=>mappings.Snapshot();
        private bool checksProperty,inChecks,mappingsProperty,inMappings,tablesProperty,inTables,inRecords,ended;
        private string? recordProperty,activeKind;
        private string? mappingProperty,mappingSource,mappingTarget;
        private bool mappingOpen,sourceSeen,targetSeen,columnsSeen;
        private FindingBuffer? activeFindings;
        private IssueBuffer? activeIssues;
        internal bool Complete=>ended&&!inRecords&&!inChecks&&!inMappings&&!inTables;
        internal int Pump(ReadOnlySpan<byte> data,bool final,ref JsonReaderState state,Utf8JsonWriter writer)
        {
            var reader=new Utf8JsonReader(data,final,state);
            while(true)
            {
                var before=reader;if(!reader.Read())break;
                var depth=reader.CurrentDepth;var type=reader.TokenType;
                if(inTables)
                {
                    if(type==JsonTokenType.EndArray&&depth==1){inTables=false;continue;}
                    if(type!=JsonTokenType.String||depth!=2)throw new InvalidDataException();
                    var name=JsonSerializer.Deserialize<string>(data.Slice(checked((int)reader.TokenStartIndex),checked((int)(reader.BytesConsumed-reader.TokenStartIndex))),Options);
                    if(string.IsNullOrWhiteSpace(name))throw new InvalidDataException();
                    tables.Add(name);continue;
                }
                if(inRecords)
                {
                    if(type==JsonTokenType.EndArray&&depth==3){inRecords=false;continue;}
                    if(type!=JsonTokenType.StartObject||depth!=4)throw new InvalidDataException();
                    var start=checked((int)reader.TokenStartIndex);
                    if(!reader.TrySkip()){reader=before;break;}
                    var record=data.Slice(start,checked((int)reader.BytesConsumed)-start);
                    if(activeKind=="Columns")
                    {
                        var column=JsonSerializer.Deserialize<ColumnMapping>(record,Options)??throw new InvalidDataException();
                        if(column.Source==null&&column.Target==null||column.Source!=null&&string.IsNullOrWhiteSpace(column.Source)||column.Target!=null&&string.IsNullOrWhiteSpace(column.Target))throw new InvalidDataException();
                        mappings.AddColumn(column);
                    }
                    else if(activeKind=="Findings")activeFindings!.Add(JsonSerializer.Deserialize<Finding>(record,Options)??throw new InvalidDataException());
                    else
                    {
                        var issue=JsonSerializer.Deserialize<ScopeIssue>(record,Options)??throw new InvalidDataException();
                        if(issue.Reason==Reason.None||!Enum.IsDefined(issue.Reason))throw new InvalidDataException();
                        activeIssues!.Add(issue);
                    }
                    continue;
                }
                if(inMappings)
                {
                    if(type==JsonTokenType.EndArray&&depth==1){if(mappingOpen)throw new InvalidDataException();inMappings=false;continue;}
                    if(type==JsonTokenType.StartObject&&depth==2)
                    {
                        if(mappingOpen)throw new InvalidDataException();mappingOpen=true;sourceSeen=targetSeen=columnsSeen=false;
                        mappingSource=mappingTarget=mappingProperty=null;mappings.Begin(null,null);continue;
                    }
                    if(type==JsonTokenType.EndObject&&depth==2)
                    {
                        if(!mappingOpen||mappingProperty!=null||!sourceSeen||!targetSeen||!columnsSeen||mappingSource==null&&mappingTarget==null)throw new InvalidDataException();
                        mappings.End(mappingSource,mappingTarget);mappingOpen=false;continue;
                    }
                    if(!mappingOpen||depth!=3)throw new InvalidDataException();
                    if(type==JsonTokenType.PropertyName)
                    {
                        if(mappingProperty!=null)throw new InvalidDataException();mappingProperty=reader.GetString();
                        switch(mappingProperty)
                        {
                            case "Source":if(sourceSeen)throw new InvalidDataException();sourceSeen=true;break;
                            case "Target":if(targetSeen)throw new InvalidDataException();targetSeen=true;break;
                            case "Columns":if(columnsSeen)throw new InvalidDataException();columnsSeen=true;break;
                            default:throw new InvalidDataException();
                        }
                        continue;
                    }
                    if(mappingProperty=="Columns")
                    {
                        if(type!=JsonTokenType.StartArray)throw new InvalidDataException();
                        mappingProperty=null;activeKind="Columns";inRecords=true;continue;
                    }
                    if(mappingProperty is not("Source" or "Target")||type is not(JsonTokenType.String or JsonTokenType.Null))throw new InvalidDataException();
                    var label=type==JsonTokenType.Null?null:JsonSerializer.Deserialize<string>(data.Slice(checked((int)reader.TokenStartIndex),checked((int)(reader.BytesConsumed-reader.TokenStartIndex))),Options);
                    if(label!=null&&string.IsNullOrWhiteSpace(label))throw new InvalidDataException();
                    if(mappingProperty=="Source")mappingSource=label;else mappingTarget=label;
                    mappingProperty=null;continue;
                }
                if(inChecks&&type==JsonTokenType.StartObject&&depth==2){Findings.Add(null);Issues.Add(null);PairingLimits.Add(null);}
                if(recordProperty!=null)
                {
                    activeKind=recordProperty;recordProperty=null;
                    if(type==JsonTokenType.StartArray&&depth==3)
                    {
                        if(activeKind=="Findings"){activeFindings=new FindingBuffer(storage);Findings[^1]=activeFindings;}
                        else if(activeKind!="Columns"){activeIssues=new IssueBuffer(storage);(activeKind=="Issues"?Issues:PairingLimits)[^1]=activeIssues;}
                        inRecords=true;writer.WriteStartArray();writer.WriteEndArray();continue;
                    }
                }
                if(checksProperty){checksProperty=false;if(type==JsonTokenType.StartArray&&depth==1)inChecks=true;}
                if(tablesProperty)
                {
                    tablesProperty=false;
                    if(type!=JsonTokenType.StartArray||depth!=1)throw new InvalidDataException();
                    inTables=true;writer.WriteStartArray();writer.WriteEndArray();continue;
                }
                if(mappingsProperty)
                {
                    mappingsProperty=false;
                    if(type!=JsonTokenType.StartArray||depth!=1)throw new InvalidDataException();
                    inMappings=true;writer.WriteStartArray();writer.WriteEndArray();continue;
                }
                if(type==JsonTokenType.PropertyName)
                {
                    var name=reader.GetString()!;checksProperty=depth==1&&name=="Checks";
                    mappingsProperty=depth==1&&name=="Mappings";
                    tablesProperty=depth==1&&name=="Tables";
                    recordProperty=inChecks&&depth==3&&name is "Findings" or "Issues" or "PairingLimits"?name:null;
                    writer.WritePropertyName(name);continue;
                }
                switch(type)
                {
                    case JsonTokenType.StartObject:writer.WriteStartObject();break;
                    case JsonTokenType.EndObject:writer.WriteEndObject();if(depth==0)ended=true;break;
                    case JsonTokenType.StartArray:writer.WriteStartArray();break;
                    case JsonTokenType.EndArray:writer.WriteEndArray();if(inChecks&&depth==1)inChecks=false;if(inMappings&&depth==1)inMappings=false;break;
                    // This token has been read and validated above. Copy its
                    // escapes without GetString replacing/rejecting lone units.
                    case JsonTokenType.String:writer.WriteRawValue(data.Slice(checked((int)reader.TokenStartIndex),checked((int)(reader.BytesConsumed-reader.TokenStartIndex))),skipInputValidation:true);break;
                    case JsonTokenType.Number:writer.WriteRawValue(reader.ValueSpan);break;
                    case JsonTokenType.True:writer.WriteBooleanValue(true);break;
                    case JsonTokenType.False:writer.WriteBooleanValue(false);break;
                    case JsonTokenType.Null:writer.WriteNullValue();break;
                    default:throw new InvalidDataException();
                }
            }
            state=reader.CurrentState;return checked((int)reader.BytesConsumed);
        }
    }
}
