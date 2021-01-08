using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility.Inspect;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class InspectProcessor : IAnonymizerProcessor
    {
        private INamedEntityRecognizer _textAnalyticRecognizer { get; set; }
        private INamedEntityRecognizer _ruleBasedRecognizer { get; set; }
        private StructMatchRecognizer _structMatchRecognizer { get; set; }

        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<InspectProcessor>();

        private InspectParameters _inspectParameters { get; set; }


        public InspectProcessor(InspectParameters inspectParameters)
        {
            _structMatchRecognizer = new StructMatchRecognizer(inspectParameters?.StructMatchRecognizerParameters);
            _textAnalyticRecognizer = new TextAnalyticRecognizer(inspectParameters?.TextAnalyticRecognizerParameters);
            _ruleBasedRecognizer = new RuleBasedRecognizer(inspectParameters?.RuleBasedRecognizerParameters);
            _inspectParameters = inspectParameters;
        }

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            EnsureArg.IsNotNull(node);
            EnsureArg.IsNotNull(context?.VisitedNodes);
            EnsureArg.IsNotNull(settings);

            var processResult = new ProcessResult();
            if (string.IsNullOrEmpty(node?.Value?.ToString()))
            {
                return processResult;
            }

            // Preprocess node.Value
            var input = node.Value.ToString();
            var rawText = HttpUtility.HtmlDecode(input);
            // Replace each HTML tag with a single space
            // This will change the original indexes of characters, so stripInfo will be used to revert indexes.
            var stripInfo = HtmlTextUtility.StripTags(rawText);
            var strippedText = stripInfo.StrippedText;

            // StructMatch
            var entitiesStructMatch = new List<Entity>();
            if (_inspectParameters.StructMatchRecognizerParameters.EnableStructMatchRecognizer)
            {
                entitiesStructMatch = _structMatchRecognizer.RecognizeText(strippedText, node, settings);
            }

            // Text Analytics API
            var entitiesTA = new List<Entity>();
            if (_inspectParameters.TextAnalyticRecognizerParameters.EnableTextAnalyticRecognizer)
            {
                try
                {
                    entitiesTA = _textAnalyticRecognizer.RecognizeText(strippedText);
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning($"TextAnalyticRecognizer failed.");
                    return new RedactProcessor(false, false, false, null).Process(node);
                }
            }
            
            // Recognizer.Text & self-designed rules
            var entitiesRuleBased = new List<Entity>();
            if (_inspectParameters.RuleBasedRecognizerParameters.EnableRuleBasedRecognizer)
            {
                try
                {
                    entitiesRuleBased = _ruleBasedRecognizer.RecognizeText(strippedText);
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning($"RuleBasedRecognizer failed.");
                    return new RedactProcessor(false, false, false, null).Process(node);
                }
            }

            // Combined entities
            var entities = entitiesTA.Concat(entitiesStructMatch).Concat(entitiesRuleBased).ToList<Entity>();

            // Anonymize recognized entities in node.Value
            node.Value = EntityProcessUtility.Postprocess(rawText, entities, stripInfo);

            _logger.LogDebug($"Fhir value '{input}' at '{node.Location}' is de-identified to '{node.Value}'.");
            processResult.AddProcessRecord(AnonymizationOperations.Inspect, node);
            return processResult;
        }
    }
}
