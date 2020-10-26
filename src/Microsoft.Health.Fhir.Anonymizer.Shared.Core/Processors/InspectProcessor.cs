using System;
using System.Collections.Generic;
using System.Globalization;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using System.Text.RegularExpressions;
using EnsureThat;
using Hl7.FhirPath;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility.Inspect;
using Microsoft.Extensions.Primitives;
using System.Threading;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class InspectProcessor : IAnonymizerProcessor
    {
        private Dictionary<string, string> _colorMapper = new Dictionary<string, string>() {
            { "TextAnalyticRecognizer", "danger" },
            { "RuleBasedRecognizer", "success" },
            { "StructMatchRecognizer", "" }
        };
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

            // var rawText = HttpUtility.HtmlDecode(node.Value.ToString());
            var rawText = HttpUtility.HtmlDecode(node.Value.ToString());
            // var formattedText = System.Xml.Linq.XElement.Parse(rawText).ToString();
            var stripInfo = HtmlTextUtility.StripTags(rawText);
            var strippedText = stripInfo.StrippedText;
            // Console.WriteLine(strippedText);

            Stopwatch stopWatch = new Stopwatch();

            stopWatch.Start();
            // Structuerd fields match recognizer results
            _structMatchRecognizer = new StructMatchRecognizer();
            var entitiesStructMatch = _structMatchRecognizer.RecognizeText(strippedText, node, settings);
            stopWatch.Stop();
            Console.WriteLine($"StructMatch: {stopWatch.Elapsed}");

            stopWatch.Reset();
            stopWatch.Start();
            // TA recognizer results
            var entitiesTA = _textAnalyticRecognizer.RecognizeText(strippedText);
            stopWatch.Stop();
            Console.WriteLine($"TA: {stopWatch.Elapsed}");

            stopWatch.Reset();
            stopWatch.Start();
            // Rule-based (Recognizers.Text) recognizer results
            var entitiesRuleBased = _ruleBasedRecognizer.RecognizeText(strippedText);
            stopWatch.Stop();
            Console.WriteLine($"RT: {stopWatch.Elapsed}");

            // Combined entities
            var entities = entitiesTA.Concat(entitiesStructMatch).Concat(entitiesRuleBased).ToList<Entity>();
            // var entities = entitiesStructMatch.Concat(entitiesRuleBased).ToList<Entity>();

            entities = EntityProcessUtility.PreprocessEntities(entities);
            entities = EntityProcessUtility.PostprocessEntities(entities, stripInfo);
            var processedText = EntityProcessUtility.ProcessEntities(rawText, entities);
            node.Value = processedText;

            //if (false) 
            //{
            //    SaveEntities(entitiesStructMatch, node, rawText, formattedText, strippedText, "structMatch");
            //    SaveEntities(entitiesTA, node, rawText, formattedText, strippedText, "textAnalytic");
            //    SaveEntities(entitiesRuleBased, node, rawText, formattedText, strippedText, "ruleBased");
            //    SaveEntities(entities, node, rawText, formattedText, strippedText, "merged");
            //}

            //if (false)
            //{
            //    PrintEntities(entitiesStructMatch, "StructMatch recognizer");
            //    PrintEntities(entitiesTA, "TA recognizer");
            //    PrintEntities(entitiesRuleBased, "RuleBased recognizer");
            //    PrintEntities(entities, "Combined Results");
            //    _printInfo.AppendLine(processedText);
            //    _printInfo.AppendLine(new string('=', 100));
            //    Console.WriteLine(_printInfo);
            //}

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

        private void SaveEntities(List<Entity> entities, ElementNode node, string rawText, string formattedText, string strippedText, string tag)
        {
            Console.WriteLine(formattedText);
            Console.WriteLine(strippedText);
            node.Value = "<PLACEHOLDER>";
            var resourceNode = node;
            while (resourceNode.Parent != null)
            {
                resourceNode = resourceNode.Parent;
            }
            var nodeId = resourceNode.GetNodeId();
            var resource = resourceNode.ToPoco<Resource>();
            var resourceType = resource.ResourceType.ToString();
            var json = resource.ToJson(new FhirJsonSerializationSettings { Pretty = true });
            var baseIndex = json.IndexOf(node.Value.ToString());
            var jsonBeforeText = json.Substring(0, baseIndex);
            var jsonAfterText = json.Substring(baseIndex + node.Value.ToString().Length);
            var segments = new List<Segment>();
            int startIndex = 0;
            foreach (var entity in entities)
            {
                AddNonEntitySegments(segments, formattedText, strippedText, startIndex, entity.Offset);
                segments.Add(new Segment() {
                    text = formattedText.Substring(entity.Offset, entity.Length),
                    color = _colorMapper[entity.Recognizer],
                    category = $"[{entity.Category}]"
                });
                startIndex = entity.Offset + entity.Length;
            }
            AddNonEntitySegments(segments, formattedText, strippedText, startIndex, formattedText.Length);
            // segments.Insert(0, new Segment() {
            //     text = jsonBeforeText,
            //     color = "black",
            //     category = ""
            // });
            // segments.Add(new Segment() {
            //     text = jsonAfterText,
            //     color = "black",
            //     category = ""
            // });
            var segmentsJson = JsonConvert.SerializeObject(segments);
            File.WriteAllText("/Users/v-zhexji/FHIR-Tools-for-Anonymization-InspectProcessor/samples/json/" + resourceType + '-' + nodeId + "-" + tag + ".json", segmentsJson);
        }

        private void AddNonEntitySegments(List<Segment> segments, string formattedText, string strippedText, int startIndex, int endIndex)
        {
            var lastIndex = startIndex;
            bool isDiff = formattedText[startIndex] != strippedText[startIndex];
            for (var i = startIndex; i < endIndex; i++)
            {
                if (formattedText[i] == ' ' || formattedText[i] == '\n')
                {
                    continue;
                }
                if (isDiff != (formattedText[i] != strippedText[i]))
                {
                    segments.Add(new Segment() {
                        text = formattedText.Substring(lastIndex, i - lastIndex),
                        color = isDiff ? "lightgray" : "black",
                        category = ""
                    });
                    lastIndex = i;
                }
                isDiff = (formattedText[i] != strippedText[i]);
            }
            segments.Add(new Segment() {
                text = formattedText.Substring(lastIndex, endIndex - lastIndex),
                color = isDiff ? "lightgray" : "black",
                category = ""
            });
        }
    }

    public class Segment
    {
        public string text;
        public string color;
        public string category;
    }
}
