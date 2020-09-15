using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using EnsureThat;
using Hl7.FhirPath;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using MathNet.Numerics.Distributions;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class InspireProcessor : IAnonymizerProcessor
    {
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

            Console.WriteLine(node.Value.ToString());
            Console.WriteLine(freeText);
            Console.WriteLine(new string('-', 100));
            Console.WriteLine();

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
                        Console.WriteLine("{0, -15} {1}: , {2}", $"[{combineName}]", node.InstanceType, node.Value.ToString());
                    }

                    
                    freeText.Replace(node.Value.ToString(), $"[{combineName}]");
                }
            }
            return;
        }
    }
}
