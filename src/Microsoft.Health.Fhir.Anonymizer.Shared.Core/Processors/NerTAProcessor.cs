using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility.NerTAUtility;
using Hl7.Fhir.ElementModel;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class NerTAProcessor : IAnonymizerProcessor
    {
        private INamedEntityRecognizer _namedEntityRecognizer { get; set; }

        public NerTAProcessor(RecognizerApi recognizerApi)
        {
            var type = recognizerApi.Type;
            _namedEntityRecognizer = type switch
            {
                RecognizerType.MicrosoftNer => new TextAnalyticRecognizer(recognizerApi),
                _ => throw new NotImplementedException($"The named entity recognition method is not supported: {type}"),
            };
        }

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            var processResult = new ProcessResult();
            if (string.IsNullOrEmpty(node?.Value?.ToString()))
            {
                return processResult;
            }
            var originText = HttpUtility.HtmlDecode(node.Value.ToString());
            // TODO: Whether to use textStripTags as the input of processor
            var originTextStripTags = HtmlTextUtility.StripTags(originText);
            // Console.WriteLine($"{originText.Length}, {originTextStripTags.Length}");

            var recognitionResult = _namedEntityRecognizer.RecognizeText(originTextStripTags);
            node.Value = ProcessEntities(originText, originTextStripTags, recognitionResult);

            processResult.AddProcessRecord(AnonymizationOperations.Masked, node);
            return new ProcessResult();
        }

        private string ProcessEntities(string originText, string originTextStripTags, IEnumerable<Entity> textEntities)
        {
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
                Console.WriteLine("{0, -18}: {1}", $"[{entity.Category}]", entity.Text);
                result.Append(text.SubstringByTextElements(startIndex, entity.Offset - startIndex));
                result.Append($"[{entity.Category.ToUpperInvariant()}]");
                startIndex = entity.Offset + entity.Length;
            }
            if (startIndex < text.LengthInTextElements)
            {
                result.Append(text.SubstringByTextElements(startIndex));
            }
            Console.WriteLine(originTextStripTags);
            Console.WriteLine(originText);
            Console.WriteLine(result.ToString());
            Console.WriteLine(new string('-', 100));
            Console.WriteLine();
            return result.ToString();
        }
    }
}
