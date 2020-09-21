﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using Hl7.FhirPath;
using Hl7.Fhir.ElementModel;
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

        InspectSetting _inspectSetting;

        public List<Entity> RecognizeText(ElementNode node, Dictionary<string, object> settings = null)
        {
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

            var entities = InspectEntities(node.Value.ToString(), structDataList);
            entities = EntityProcessUtility.PreprocessEntities(entities);
            return entities;
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
    }
}
