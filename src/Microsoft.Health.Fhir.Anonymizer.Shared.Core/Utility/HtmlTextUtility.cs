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
        public static string StripTags(string html)
        {
            HtmlDocument mainDoc = new HtmlDocument();
            mainDoc.LoadHtml(html);
            StringBuilder sb = new StringBuilder();
            var nodes = mainDoc.DocumentNode.Descendants().ToList();
            int startIndex = 0;
            foreach (var node in nodes)
            {
                if (!node.HasChildNodes)
                {
                    sb.Append(new string(' ', node.StreamPosition - startIndex));
                    sb.Append(node.InnerText);
                    startIndex = node.StreamPosition + node.InnerLength;
                }
            }
            sb.Append(new string(' ', html.Length - startIndex));
            return sb.ToString();
        }

        public static IEnumerable<string> StripTagsForArray(IEnumerable<string> htmls)
        {
            return htmls.Select(html => StripTags(html));
        }
    }
}
