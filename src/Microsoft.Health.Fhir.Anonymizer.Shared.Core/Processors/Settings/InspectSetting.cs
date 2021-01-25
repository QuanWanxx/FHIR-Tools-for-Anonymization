using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    public class InspectSetting
    {
        public List<string> ExactMatchExpressions { get; set; }
        public List<string> FuzzyMatchExpressions { get; set; }
        public List<string> MatchTypes { get; set; }
        public List<string> IgnoreExpressions { get; set; }

        public static InspectSetting CreateFromRuleSettings(Dictionary<string, object> ruleSettings)
        {
            EnsureArg.IsNotNull(ruleSettings);

            var exactMatchExpressionsString = ruleSettings.GetValueOrDefault("exactMatchExpressions")?.ToString();
            var fuzzyMatchExpressionsString = ruleSettings.GetValueOrDefault("fuzzyMatchExpressions")?.ToString();
            var matchTypesString = ruleSettings.GetValueOrDefault("matchTypes")?.ToString();
            var ignoreExpressionsString = ruleSettings.GetValueOrDefault("ignoreExpressions")?.ToString();
            return new InspectSetting
            {
                ExactMatchExpressions = JsonConvert.DeserializeObject<List<string>>(exactMatchExpressionsString),
                FuzzyMatchExpressions = JsonConvert.DeserializeObject<List<string>>(fuzzyMatchExpressionsString),
                MatchTypes = JsonConvert.DeserializeObject<List<string>>(matchTypesString),
                IgnoreExpressions = JsonConvert.DeserializeObject<List<string>>(ignoreExpressionsString),
            };
        }
    }
}
