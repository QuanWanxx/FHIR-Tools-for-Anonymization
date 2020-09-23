﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using EnsureThat;
using Hl7.FhirPath;
using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility.Inspect;
using Microsoft.Extensions.Primitives;
using System.Threading;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class InspectProcessor : IAnonymizerProcessor
    {
        private INamedEntityRecognizer _textAnalyticRecognizer { get; set; }
        private INamedEntityRecognizer _ruleBasedRecognizer { get; set; }
        private StructMatchRecognizer _structMatchRecognizer { get; set; }

        private StringBuilder _printInfo;

        public InspectProcessor(RecognizerApi recognizerApi)
        {
            _printInfo = new StringBuilder();
            _textAnalyticRecognizer = new TextAnalyticRecognizer(recognizerApi);
            _ruleBasedRecognizer = new RuleBasedRecognizer();
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
            PrintResourceType(node);
            _printInfo.AppendLine(node.Value.ToString());

            var rawText = HttpUtility.HtmlDecode(node.Value.ToString());
            var strippedText = HtmlTextUtility.StripTags(rawText);
            // TA recognizer results
            var entitiesTA = _textAnalyticRecognizer.RecognizeText(strippedText);
            PrintEntities(entitiesTA, "TA recognizer");
            // Rule-based (Recognizers.Text) recognizer results
            var entitiesRuleBased = _ruleBasedRecognizer.RecognizeText(strippedText);
            PrintEntities(entitiesRuleBased, "RuleBased recognizer");

            // Structuerd fields match recognizer results
            _structMatchRecognizer = new StructMatchRecognizer();
            var entitiesStructMatch = _structMatchRecognizer.RecognizeText(node, settings);
            PrintEntities(entitiesStructMatch, "StructMatch recognizer");

            // Combined entities
            var entities = entitiesTA.Concat(entitiesStructMatch).Concat(entitiesRuleBased).ToList<Entity>();
            entities = EntityProcessUtility.PreprocessEntities(entities);
            PrintEntities(entities, "Combined Results");

            var processedText = EntityProcessUtility.ProcessEntities(rawText, entities);
            _printInfo.AppendLine(processedText);
            _printInfo.AppendLine(new string('=', 100));

            Console.WriteLine(_printInfo);
            node.Value = processedText;
            processResult.AddProcessRecord(AnonymizationOperations.Inspect, node);
            return processResult;
        }

        private void PrintResourceType(ElementNode node)
        {
            var resourceNode = node;
            while (!resourceNode.IsFhirResource())
            {
                resourceNode = resourceNode.Parent;
            }
            _printInfo.AppendLine(resourceNode.Name.ToString());
        }

        private void PrintEntities(List<Entity> entities, string recognizerType = "")
        {
            _printInfo.AppendLine(new string('-', 12) + $"{recognizerType, -25}" + new string('-', 12));
            foreach (var entity in entities)
            {
                _printInfo.AppendLine($"{$"[{entity.Category}]",-20} {entity.SubCategory,-10} -{entity.Offset, -4}:" +
                                      $" {entity.Text}");
            }
            _printInfo.AppendLine();
        }
    }
}