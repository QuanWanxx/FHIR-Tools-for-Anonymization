using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility
{
    public class HtmlTextUtility
    {
        public static StripInfo StripTags(string html)
        {
            var result = new StripInfo()
            {
                SkipPositions = new List<SkipPosition>()
            };
            HtmlDocument mainDoc = new HtmlDocument();
            mainDoc.LoadHtml(html);
            StringBuilder sb = new StringBuilder();
            var nodes = mainDoc.DocumentNode.Descendants().ToList();
            int startIndex = 0;
            foreach (var node in nodes)
            {
                if (!node.HasChildNodes)
                {
                    result.SkipPositions.Add(new SkipPosition() { Index = sb.Length, Length = node.StreamPosition - startIndex - 1 });
                    sb.Append(new string(' ', 1));
                    sb.Append(node.InnerText);
                    startIndex = node.StreamPosition + node.InnerLength;
                }
            }
            result.SkipPositions.Add(new SkipPosition() { Index = sb.Length, Length = html.Length - startIndex - 1 });
            sb.Append(new string(' ', 1));
            result.StrippedText = sb.ToString();
            return result;
        }

        public static IEnumerable<StripInfo> StripTagsForArray(IEnumerable<string> htmls)
        {
            return htmls.Select(html => StripTags(html));
        }
    }

    public class StripInfo
    {
        public List<SkipPosition> SkipPositions;
        
        public string StrippedText;
    }

    public class SkipPosition
    {
        public int Index;
        
        public int Length;
    }
}
