using System.Text;

namespace Verifier.Desktop;

internal static class WebTextWhitespace
{
    // CSS white-space:normal collapses ASCII whitespace; NBSP remains significant.
    public static string Collapse(string text)
    {
        var result = new StringBuilder(text.Length);
        var space = false;
        foreach (var c in text)
        {
            if (c is ' ' or '\t' or '\r' or '\n' or '\f') { space = result.Length > 0; continue; }
            if (space) result.Append(' ');
            result.Append(c);
            space = false;
        }
        return result.ToString();
    }
}
