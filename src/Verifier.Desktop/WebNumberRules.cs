using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Verifier.Desktop;

/// <summary>The source port input's editing value, normalized DOM value and integer stepping.</summary>
public static class WebNumberRules
{
    private static readonly Regex NumberSyntax=new(@"^-?(?:[0-9]+(?:\.[0-9]+)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?$",RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    public readonly record struct State(string Value,double? Number,bool BadInput,bool RangeUnderflow,bool RangeOverflow,bool StepMismatch)
    {public bool Valid=>!BadInput&&!RangeUnderflow&&!RangeOverflow&&!StepMismatch;}
    public static State Read(string raw)
    {
        var value=raw;
        // Chromium's localized editing conversion accepts a leading plus and
        // unfinished decimal separator, but leaves scientific notation untouched.
        if(!value.Contains('e')&&!value.Contains('E'))
        {if(value.StartsWith('+'))value=value[1..];if(value.EndsWith('.'))value=value[..^1];}
        double number=0;
        var parsed=NumberSyntax.IsMatch(value)&&double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out number)&&double.IsFinite(number);
        if(!parsed)return new("",null,raw.Length>0,false,false,false);
        var exact=DecimalValue(value);
        var under=exact is { } low?low<1:number<1;
        var over=exact is { } high?high>65535:number>65535;
        var mismatch=false;
        if(exact is { } d && decimal.Abs(d-1)<=9007199254740992m)
        {
            // Chromium's step=1 real-number rule tolerates step/2^24. This
            // belongs only to the source port editor, never DB data comparison.
            const decimal error=0.000000059604644775390625m;
            var distance=decimal.Abs(d-1);
            var remainder=decimal.Abs(distance-decimal.Round(distance,0,MidpointRounding.AwayFromZero));
            mismatch=error<remainder&&remainder<1-error;
        }
        return new(value,number,false,under,over,mismatch);
    }
    public static string SanitizeAssignedValue(string value)
    {return NumberSyntax.IsMatch(value)&&double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var n)&&double.IsFinite(n)?value:"";}
    public static string Step(string raw,int direction)
    {
        if(direction is not(1 or -1))throw new ArgumentOutOfRangeException(nameof(direction));
        var state=Read(raw);
        if(state.Number is not { } number)return "1";
        var exact=DecimalValue(state.Value);
        if(exact is not { } d)return direction>0&&number<1?"1":direction<0&&number>65535?"65535":raw;
        if(direction>0&&d>=65535 || direction<0&&d<=1)return raw;
        if(direction>0&&d<1)return "1";
        if(direction<0&&d>65535)return "65535";
        var next=state.StepMismatch?(direction>0?decimal.Ceiling(d):decimal.Floor(d)):decimal.Round(d,0,MidpointRounding.AwayFromZero)+direction;
        next=Math.Clamp(next,1m,65535m);
        return next.ToString("0",CultureInfo.InvariantCulture);
    }
    private static decimal? DecimalValue(string value)
    {
        // Blink parses number validity and stepping with an 18-digit decimal
        // coefficient. DOM valueAsNumber remains the independent double above.
        var negative=value.StartsWith('-');var unsigned=negative?value[1..]:value;
        var e=unsigned.IndexOfAny(['e','E']);var mantissa=e<0?unsigned:unsigned[..e];
        long exponent=0;
        if(e>=0)
        {
            var text=unsigned[(e+1)..];
            exponent=int.TryParse(text,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out var parsed)?parsed:text.StartsWith('-')?-1_000_000:1_000_000;
        }
        decimal coefficient=0;
        var kept=0;var fraction=0;var extra=0;var afterDot=false;
        foreach(var c in mantissa)
        {
            if(c=='.'){afterDot=true;continue;}
            // Leading integer zeroes are ignored; fractional zeroes consume
            // coefficient precision in Blink's Decimal::FromString.
            if(!afterDot&&kept==0&&c=='0')continue;
            if(kept<18){coefficient=coefficient*10+c-'0';kept++;if(afterDot)fraction++;}
            else if(!afterDot)extra++;
        }
        if(coefficient==0)return 0;
        var scale=exponent-fraction+extra;
        if(scale+coefficient.ToString("0",CultureInfo.InvariantCulture).Length>28)return null;
        if(scale<-46)return 0;
        while(scale>0){coefficient*=10;scale--;}
        while(scale<0){coefficient/=10;scale++;}
        return negative?-coefficient:coefficient;
    }
    public static string FilterInsertion(string raw,int selectionStart,int selectionEnd,string inserted)
    {
        var start=Math.Clamp(Math.Min(selectionStart,selectionEnd),0,raw.Length);
        var end=Math.Clamp(Math.Max(selectionStart,selectionEnd),0,raw.Length);
        var left=raw[..start];var right=raw[end..];var result=new StringBuilder();
        foreach(var input in inserted)
        {
            var c=input is >= '\uff10' and <= '\uff19'?(char)(input-'\uff10'+'0'):input is '\uff0d' or '\u30fc'?'-':input;
            if(c is not(>= '0' and <= '9') && c is not('.' or 'e' or 'E' or '+' or '-'))continue;
            var all=left+right;
            if(c=='.'&&(left.Contains('.')||right.Contains('.')||HasE(left)||HasSignNotAfterE(right)))continue;
            if(IsE(c)&&(HasE(left)||HasE(right)||right.Contains('.')))continue;
            if(IsSign(c)&&(all.Count(IsSign)>=2 || HasE(all)&&!(left.Length==0||IsE(left[^1])) || right.Length>0&&IsSign(right[0]) || c=='+'&&HasE(all)&&left.Length==0))continue;
            if(c is >= '0' and <= '9' && ((left.Length==0&&right.Length>0&&IsSign(right[0])) || left.Length>0&&IsE(left[^1])&&right.Length>0&&IsSign(right[0])))continue;
            left+=c;result.Append(c);
        }
        return result.ToString();
    }
    private static bool IsE(char c)=>c is 'e' or 'E';
    private static bool IsSign(char c)=>c is '+' or '-';
    private static bool HasE(string s)=>s.Any(IsE);
    private static bool HasSignNotAfterE(string s)
    {for(var i=0;i<s.Length;i++)if(IsSign(s[i])&&(i==0||!IsE(s[i-1])))return true;return false;}
}
