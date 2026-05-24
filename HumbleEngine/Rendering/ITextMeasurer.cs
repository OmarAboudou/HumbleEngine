namespace HumbleEngine;

public interface ITextMeasurer
{
    float MeasureText(string text, Font font);
}

public static class TextMeasurerExtensions
{
    public static string[] BreakLines(this ITextMeasurer measurer, string text, Font font, float maxWidth)
    {
        var result = new List<string>();

        foreach (var paragraph in text.Split('\n'))
        {
            if (maxWidth <= 0f || measurer.MeasureText(paragraph, font) <= maxWidth)
            {
                result.Add(paragraph);
                continue;
            }

            var line  = new System.Text.StringBuilder();
            foreach (var word in paragraph.Split(' '))
            {
                var candidate = line.Length == 0 ? word : line + " " + word;
                if (measurer.MeasureText(candidate, font) <= maxWidth)
                {
                    line.Clear();
                    line.Append(candidate);
                }
                else
                {
                    if (line.Length > 0) result.Add(line.ToString());
                    line.Clear();
                    line.Append(word);
                }
            }
            if (line.Length > 0) result.Add(line.ToString());
        }

        return [.. result];
    }
}
