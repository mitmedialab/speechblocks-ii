public class TextUtil
{
    public static int FirstIndexOf(string text, string target)
    {
        int i = text.IndexOf(target);
        if (i >= 0) return i;
        return text.Length;
    }
}
