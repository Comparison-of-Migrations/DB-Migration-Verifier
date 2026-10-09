using System.Reflection;
using System.Reflection.Emit;
using Avalonia.Animation;

internal static class MotionEngineInspect
{
    public static void Run()
    {
        foreach(var ms in new[]{27d,27.5,800.8})Console.WriteLine($"TimeSpan input {ms}: ticks={TimeSpan.FromMilliseconds(ms).Ticks};milliseconds={TimeSpan.FromMilliseconds(ms).TotalMilliseconds}");
        var codes=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Select(f=>(OpCode)f.GetValue(null)!).ToDictionary(c=>unchecked((ushort)c.Value));
        foreach(var type in typeof(Animation).Assembly.GetTypes().Where(t=>t.Name.Contains("AnimationInstance")))
        foreach(var method in type.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
        {
            var il=method.GetMethodBody()?.GetILAsByteArray();if(il is null)continue;
            var output=new List<string>();
            for(var i=0;i<il.Length;)
            {
                var position=i;ushort code=il[i++];if(code==0xfe)code=(ushort)(0xfe00|il[i++]);var op=codes[code];
                var size=op.OperandType switch {OperandType.InlineNone=>0,OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar=>1,OperandType.InlineVar=>2,OperandType.InlineI8 or OperandType.InlineR=>8,OperandType.InlineSwitch=>4+4*BitConverter.ToInt32(il,i),_=>4};
                string operand="";
                if(op.OperandType is OperandType.InlineMethod or OperandType.InlineField or OperandType.InlineType or OperandType.InlineTok)
                {
                    try{var member=method.Module.ResolveMember(BitConverter.ToInt32(il,i),type.GetGenericArguments(),method.GetGenericArguments());operand=member?.DeclaringType+"."+member;}
                    catch(ArgumentException){operand="unresolved";}
                }
                else if(size==4&&op.OperandType!=OperandType.InlineSwitch)operand=BitConverter.ToInt32(il,i).ToString();
                else if(size==8&&op.OperandType==OperandType.InlineI8)operand=BitConverter.ToInt64(il,i).ToString();
                i+=size;output.Add($"{position:x4} {op.Name} {operand}");
            }
            if(output.Any(x=>x.Contains("TimeSpan")||x.Contains("Ticks")))Console.WriteLine(type.Name+"."+method.Name+"\n"+string.Join('\n',output));
        }
    }
}
