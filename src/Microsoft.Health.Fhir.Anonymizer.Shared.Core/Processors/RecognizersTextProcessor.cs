using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;
using Hl7.Fhir.ElementModel;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class RecognizersTextProcessor : IAnonymizerProcessor
    {
        private INamedEntityRecognizer _namedEntityRecognizer { get; set; }

        private StringBuilder _printInfo;

        public RecognizersTextProcessor()
        {
            _namedEntityRecognizer = new TextRecognizer();
        }

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            var processResult = new ProcessResult();
            if (string.IsNullOrEmpty(node?.Value?.ToString()))
            {
                return processResult;
            }
            var originText = HttpUtility.HtmlDecode(node.Value.ToString());

            var originTextStripTags = HtmlTextUtility.StripTags(originText);

            var recognitionResult = _namedEntityRecognizer.RecognizeText(originTextStripTags);
            node.Value = ProcessEntities(originText, recognitionResult);

            processResult.AddProcessRecord(AnonymizationOperations.Masked, node);
            return new ProcessResult();
        }

        private string ProcessEntities(string originText, IEnumerable<Entity> textEntities)
        {
            _printInfo = new StringBuilder();

            if (string.IsNullOrWhiteSpace(originText))
            {
                return originText;
            }

            var result = new StringBuilder();
            // Use StringInfo to avoid offset issues https://docs.microsoft.com/en-us/azure/cognitive-services/text-analytics/concepts/text-offsets
            var text = new StringInfo(originText);
            var startIndex = 0;
            foreach (var entity in textEntities)
            {
                // TODO: Console just to show the recognization result, will be removed
                _printInfo.AppendLine($"{$"[{entity.Category}]",-15}: {entity.Text}");
                result.Append(text.SubstringByTextElements(startIndex, entity.Offset - startIndex));
                result.Append($"[{entity.Category.ToUpperInvariant()}]");
                startIndex = entity.Offset + entity.Length;
            }
            if (startIndex < text.LengthInTextElements)
            {
                result.Append(text.SubstringByTextElements(startIndex));
            }
            // TODO: Console just to show the recognization result, will be removed
            _printInfo.AppendLine(originText);
            _printInfo.AppendLine(result.ToString());
            _printInfo.AppendLine(new string('-', 100));
            _printInfo.AppendLine();
            Console.WriteLine(_printInfo);

            return result.ToString();
        }
    }
}