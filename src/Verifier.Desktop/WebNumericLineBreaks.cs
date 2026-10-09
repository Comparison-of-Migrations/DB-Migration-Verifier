namespace Verifier.Desktop;

internal static class WebNumericLineBreaks
{
    // Chromium's numeric ASCII tailoring: identifiers can break after their
    // hyphen; a leading minus and a slash immediately before digits cannot.
    public static void Apply(string text,List<int> opportunities)
    {
        for(var i=1;i<text.Length;i++)
        {
            if(!char.IsAsciiDigit(text[i]))continue;
            if(text[i-1]=='/' || text[i-1]=='-' && (i<2 || !char.IsAsciiLetterOrDigit(text[i-2])))
                opportunities.Remove(i);
            else if(text[i-1]=='-' && !opportunities.Contains(i))opportunities.Add(i);
        }
        opportunities.Sort();
    }
}
