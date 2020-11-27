﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Hl7.FhirPath;
using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility.Inspect
{
    public class StructMatchRecognizer
    {
        struct StructData
        {
            public string Text { get; set; }
            public string Category { get; set; }
            public string InstanceType { get; set; }
        };

        private HashSet<ElementNode> _ignoreNodes = new HashSet<ElementNode>();
        private InspectSetting _inspectSetting;
        private readonly bool _enableFuzzyMatch;

        public StructMatchRecognizer(StructMatchRecognizerParameters parameters)
        {
            _enableFuzzyMatch = parameters.EnableFuzzyMatch;
        }

        public List<Entity> RecognizeText(string strippedText, ElementNode node, Dictionary<string, object> settings = null)
        {
            _inspectSetting = InspectSetting.CreateFromRuleSettings(settings);
            var resourceNode = node;
            while (!resourceNode.IsFhirResource())
            {
                resourceNode = resourceNode.Parent;
            }

            foreach (var ignoreExpression in _inspectSetting.IgnoreExpressions)
            {
                var matchNodes = resourceNode.Select(ignoreExpression).Cast<ElementNode>();
                _ignoreNodes.UnionWith(matchNodes.ToHashSet());
            }

            var structDataListForExactMatch = new List<StructData>();
            // If no expressions in config, will collect all node under the source node.
            // Notice: The value from pending processed node will not be collected in any cases.
            // if (_inspectSetting.ExactMatchExpressions.Count == 0)
            // {
            //     CollectStructDataRecursive(resourceNode, node, structDataListForExactMatch);
            // }
            // else
            // {
            foreach (var ruleExpression in _inspectSetting.ExactMatchExpressions)
            {
                var matchNodes = resourceNode.Select(ruleExpression).Cast<ElementNode>();
                foreach (var matchNode in matchNodes)
                {
                    CollectStructDataRecursive(matchNode, node, structDataListForExactMatch);
                }
            }
            // }

            var structDataListForFuzzyMatch = new List<StructData>();
            foreach (var ruleExpression in _inspectSetting.FuzzyMatchExpressions)
            {
                var matchNodes = resourceNode.Select(ruleExpression).Cast<ElementNode>();
                foreach (var matchNode in matchNodes)
                {
                    // expressions for fuzzyMatch will also perform exactMatch
                    CollectStructDataRecursive(matchNode, node, structDataListForExactMatch);
                    CollectStructDataRecursive(matchNode, node, structDataListForFuzzyMatch);
                }
            }

            // var formattedText = HttpUtility.HtmlDecode(node.Value.ToString());
            //var formattedText = System.Xml.Linq.XElement.Parse(rawText).ToString();
            var entities = InspectEntities(strippedText, structDataListForExactMatch, structDataListForFuzzyMatch);
            entities = EntityProcessUtility.ResolveEntities(entities);
            return entities;
        }

        private void CollectStructDataRecursive(ElementNode node, ElementNode processingNode, List<StructData> structDataList)
        {
            var childs = node.Children().Cast<ElementNode>();
            if (node == processingNode || _ignoreNodes.Contains(node))
            {
                return;
            }
            if (node.Value != null)
            {
                if (_inspectSetting.MatchTypes.Contains(node.InstanceType))
                {
                    var combineName = TryFindDetailName(node);
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

        private List<Entity> InspectEntities(string text, List<StructData> structDataListForExactMatch, List<StructData> structDataListForFuzzyMatch)
        {
            var entities = new List<Entity>();
            var textUpper = text.ToUpper();
            var end = text.Length;
            foreach (var structData in structDataListForExactMatch)
            {
                var start = 0;
                var at = 0;
                while ((start <= end) && (at > -1))
                {
                    at = textUpper.IndexOf(structData.Text.ToUpper(), start, end - start);
                    if (at == -1)
                    {
                        break;
                    }
                    start = at + 1;

                    entities.Add(new Entity()
                    {
                        Category = structData.Category,
                        SubCategory = structData.InstanceType,
                        ConfidenceScore = 1.1,
                        Length = structData.Text.Length,
                        Offset = at,
                        Text = structData.Text,
                        Recognizer = "StructMatchRecognizer"
                    });
                }
            }

            foreach (var structData in structDataListForFuzzyMatch)
            {
                if (_enableFuzzyMatch)
                {
                    var entitiesFuzzyMatch = FuzzyMatchUtility.FuzzyMatch(text.ToUpper(), structData.Text.ToUpper());
                    foreach (var entity in entitiesFuzzyMatch)
                    {
                        entity.Category = structData.Category;
                        entity.SubCategory = structData.InstanceType;
                    }
                    entities.AddRange(entitiesFuzzyMatch);
                }
            }
            return entities;
        }

        private string TryFindDetailName(ElementNode node)
        {
            if (node.Parent == null)
            {
                return node.Name;
            }

            string combineName = node.Parent.Name switch
            {
                "identifier" => IdentifierCodeType(node.Parent),
                _ => $"{node.Parent.Name}.{node.Name}"
            };

            return combineName;
        }

        private string IdentifierCodeType(ElementNode identifierNode)
        {
            var codeExpression = "type.coding.code";
            var textExpression = "text";
            var matchNodes = identifierNode.Select(textExpression).Cast<ElementNode>();
            if (matchNodes.Count() == 0)
            {
                matchNodes = identifierNode.Select(codeExpression).Cast<ElementNode>();
                if (matchNodes.Count() == 0)
                {
                    return "identifier.value";
                }
                else
                {
                    return $"{matchNodes.First().Value.ToString()}.code";
                }
            }
            else
            {
                return $"{matchNodes.First().Value.ToString()}.code";
            }
        }
    }
}
