using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using MathNet.Numerics.Distributions;
using Microsoft.Health.Fhir.Anonymizer.Core;
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


            var inspireSetting = InspireSetting.CreateFromRuleSettings(settings);

            return new ProcessResult();
        }

        /*
       public override void MatchStructText(ElementNode node)
       {

           if (node.IsFhirResource())
           {
               // Collect sensitive struct data
               var structData = new List<Tuple<string, string, string>>();
               string typeString = node.InstanceType;
               IEnumerable<AnonymizationFhirPathRule> resourceSpecificAndGeneralRules = GetRulesByType(typeString);

               foreach (var rule in resourceSpecificAndGeneralRules)
               {
                   string method = rule.Method.ToUpperInvariant();
                   // Only match the entity with "redact" method in structed data 
                   if (method.Equals("KEEP")) // KEEP
                   {
                       continue;
                   }

                   var matchNodes = node.Select(rule.Expression).Cast<ElementNode>();
                   foreach (var matchNode in matchNodes)
                   {
                       CollectStructDataRecursive(matchNode, structData);
                   }
               }

               // Match Text
               var experssion = "nodesByType('Narrative').div";
               var freeTextNodes = node.Select(experssion).Cast<ElementNode>();
               foreach (var freeTextNode in freeTextNodes)
               {
                   MatchAndReplace(freeTextNode, structData);
               }
           }
       }

       private void CollectStructDataRecursive(ElementNode node, List<Tuple<string, string, string>> structData)
       {
           var childs = node.Children().Cast<ElementNode>();
           // dfs
           if (childs.Any())
           {
               foreach (var child in childs)
               {
                   if (child.IsFhirResource())
                   {
                       return;
                   }
                   CollectStructDataRecursive(child, structData);
               }
           }
           // Is a leaf
           else
           {
               structData.Add(Tuple.Create(node.Name, node.InstanceType, node.Value.ToString()));
           }
           return;
       }

       private void MatchAndReplace(ElementNode node, List<Tuple<string, string, string>> structData)
       {
           var freeText = node.Value.ToString();
           foreach (var content in structData)
           {
               if (freeText.IndexOf(content.Item3) >= 0)
               {
                   freeText = freeText.Replace(content.Item3, $"[{content.Item1}]");
                   Console.WriteLine($"[{content.Item3}] replaced with [{content.Item1}]");
               }
           }
           node.Value = freeText;
       }
   */
    }
}
