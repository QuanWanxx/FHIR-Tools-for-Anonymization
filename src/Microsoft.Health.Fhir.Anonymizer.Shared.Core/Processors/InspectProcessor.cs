using System;
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
        struct StructData
        {
            public string Text { get; set; }
            public string Category { get; set; }
            public string InstanceType { get; set; }
        };

        private INamedEntityRecognizer _namedEntityRecognizer { get; set; }

        private StringBuilder _printInfo;

        private InspectSetting _inspectSetting;

        public InspectProcessor(RecognizerApi recognizerApi)
        {
            _printInfo = new StringBuilder();
            _namedEntityRecognizer = new TextAnalyticRecognizer(recognizerApi);
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

            _inspectSetting = InspectSetting.CreateFromRuleSettings(settings);
            var resourceNode = node;
            while (!resourceNode.IsFhirResource())
            {
                resourceNode = resourceNode.Parent;
            }

            var structDataList = new List<StructData>();
            // If no expressions in config, will collect all node under the source node.
            // Notice: The value from pending processed node will not be collected in any cases.
            if (_inspectSetting.Expressions.Count == 0)
            {
                CollectStructDataRecursive(resourceNode, node, structDataList);
            }
            else
            {
                foreach (var ruleExpression in _inspectSetting.Expressions)
                {
                    var matchNodes = resourceNode.Select(ruleExpression).Cast<ElementNode>();
                    foreach (var matchNode in matchNodes)
                    {
                        CollectStructDataRecursive(matchNode, node, structDataList);
                    }
                }
            }

            var entitiesStructMatch = InspectEntities(node.Value.ToString(), structDataList);
            entitiesStructMatch = EntityProcessUtility.PreprocessEntities(entitiesStructMatch);
            PrintEntities(entitiesStructMatch);

            var textSendToTA = HtmlTextUtility.StripTags(HttpUtility.HtmlDecode(node.Value.ToString()));
            var entitiesTA = _namedEntityRecognizer.RecognizeText(textSendToTA);
            PrintEntities(entitiesTA);

            var processedText = EntityProcessUtility.ProcessEntities(node.Value.ToString(), entitiesStructMatch);
            _printInfo.AppendLine(resourceNode.Name.ToString());
            _printInfo.AppendLine(node.Value.ToString());
            _printInfo.AppendLine(processedText);
            _printInfo.AppendLine(new string('-', 100));
            _printInfo.AppendLine();

            Console.WriteLine(_printInfo);
            node.Value = processedText;
            processResult.AddProcessRecord(AnonymizationOperations.Inspect, node);
            return processResult;
        }

        private void CollectStructDataRecursive(ElementNode node, ElementNode processingNode, List<StructData> structDataList)
        {
            var childs = node.Children().Cast<ElementNode>();
            if (node == processingNode)
            {
                return;
            }
            if (node.Value != null)
            {
                if (_inspectSetting.MathTypes.Contains(node.InstanceType))
                {
                    var combineName = node.Parent == null ? $"{node.Name}" : $"{node.Parent.Name}.{node.Name}";
                    structDataList.Add(new StructData()
                    {
                        Text = node.Value.ToString(),
                        Category = combineName,
                        InstanceType = node.InstanceType.ToString()
                    });
                }
            }

            // dfs
            if (childs.Any())
            {
                foreach (var child in childs)
                {
                    // Avoid the fhirResource Node
                    if (child.IsFhirResource())
                    {
                        // TODO: Not sure whether to ignore the resource Node, if ignore will cause some mis-matched issues
                        //return;
                    }
                    CollectStructDataRecursive(child, processingNode, structDataList);
                }
            }
            
            return;
        }

        private List<Entity> InspectEntities(string text, List<StructData> structDataList)
        {
            var entities = new List<Entity>();

            var end = text.Length;
            foreach (var structData in structDataList)
            {
                var start = 0;
                var at = 0;
                while ((start <= end) && (at > -1))
                {
                    at = text.IndexOf(structData.Text, start, end - start);
                    if (at == -1)
                    {
                        break;
                    }
                    start = at + 1;

                    entities.Add(new Entity()
                    {
                        Category = structData.Category,
                        SubCategory = structData.InstanceType,
                        ConfidenceScore = 1,
                        Length = structData.Text.Length,
                        Offset = at,
                        Text = structData.Text
                    });
                }
            }
            return entities;
        }

        private void PrintEntities(List<Entity> entities)
        {
            foreach (var entity in entities)
            {
                _printInfo.AppendLine($"{$"[{entity.Category}]",-20} {entity.SubCategory}:" +
                                      $"({$"[{entity.Offset}]",-6},{$"[{entity.Offset+entity.Length-1}]",-6})" +
                                      $" {entity.Text}");
            }
        }
    }
}
