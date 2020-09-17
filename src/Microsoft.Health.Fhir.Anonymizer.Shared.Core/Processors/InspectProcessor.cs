using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using EnsureThat;
using Hl7.FhirPath;
using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Microsoft.Extensions.Primitives;

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

        private StringBuilder _printInfo;

        private InspectSetting _inspectSetting;

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            EnsureArg.IsNotNull(node);
            EnsureArg.IsNotNull(context?.VisitedNodes);
            EnsureArg.IsNotNull(settings);
            _printInfo = new StringBuilder();

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

            // TODO: Maybe use EntityProcessUtility in TA_processor branch
            structDataList.Sort((t1, t2) =>
            {
                return t2.Text.Length.CompareTo(t1.Text.Length);
            });

            var freeText = new StringBuilder(node.Value.ToString());
            MatchAndReplace(freeText, structDataList);

            _printInfo.AppendLine(resourceNode.Name.ToString());
            _printInfo.AppendLine(node.Value.ToString());
            _printInfo.AppendLine(freeText.ToString());
            _printInfo.AppendLine(new string('-', 100));
            _printInfo.AppendLine();

            Console.WriteLine(_printInfo);
            node.Value = freeText.ToString();
            processResult.AddProcessRecord(AnonymizationOperations.Masked, node);
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

        private void MatchAndReplace(StringBuilder text, List<StructData> structDataList) 
        {
            foreach (var structData in structDataList)
            {
                // TODO: Here Converter to string just for print the replace information, will be removed after finishing testing
                var freeTextString = text.ToString();
                if (freeTextString.IndexOf(structData.Text) > 0)
                {
                    _printInfo.AppendLine($"{$"[{structData.Category}]",-15} {structData.InstanceType}: {structData.Text}");
                }
                text.Replace(structData.Text, $"[{structData.Category}]");
            }
            return;
        }
    }
}
