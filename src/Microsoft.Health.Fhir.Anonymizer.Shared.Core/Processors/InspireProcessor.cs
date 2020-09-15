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
    public class InspireProcessor : IAnonymizerProcessor
    {
        private StringBuilder _printInfo;

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

            var inspireSetting = InspireSetting.CreateFromRuleSettings(settings);
            var resourceNode = node;
            while (!resourceNode.IsFhirResource())
            {
                resourceNode = resourceNode.Parent;
            }

            var freeText = new StringBuilder(node.Value.ToString());
            foreach (var ruleExpression in inspireSetting.Expressions)
            {
                var matchNodes = resourceNode.Select(ruleExpression).Cast<ElementNode>();
                foreach (var matchNode in matchNodes)
                {
                    MatchAndReplaceRecursive(matchNode, freeText);
                }
            }
            _printInfo.AppendLine(node.Value.ToString());
            _printInfo.AppendLine(freeText.ToString());
            _printInfo.AppendLine(new string('-', 100));
            _printInfo.AppendLine();

            Console.WriteLine(_printInfo);
            node.Value = freeText.ToString();
            processResult.AddProcessRecord(AnonymizationOperations.Masked, node);
            return processResult;
        }

        private void MatchAndReplaceRecursive(ElementNode node, StringBuilder freeText)
        {
            var childs = node.Children().Cast<ElementNode>();
            // dfs
            if (childs.Any())
            {
                foreach (var child in childs)
                {
                    // Avoid the fhirResource Node
                    if (child.IsFhirResource())
                    {
                        return;
                    }
                    MatchAndReplaceRecursive(child, freeText);
                }
            }
            // Is a leaf
            else
            {
                if (node.InstanceType.Equals("string") || node.InstanceType.Equals("date") || node.InstanceType.Equals("dateTime"))
                {
                    var combineName = $"{node.Parent.Name}.{node.Name}";
                    // TODO: Here Converter to string just for print the replace information, will be removed after finishing testing
                    var freeTextString = freeText.ToString();
                    if (freeTextString.IndexOf(node.Value.ToString()) > 0)
                    {
                        _printInfo.AppendLine($"{$"[{combineName}]", -15} {node.InstanceType}: , {node.Value.ToString()}");
                    }
                    
                    freeText.Replace(node.Value.ToString(), $"[{combineName}]");
                }
            }
            return;
        }
    }
}
