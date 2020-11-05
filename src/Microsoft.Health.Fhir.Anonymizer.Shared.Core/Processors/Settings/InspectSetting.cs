using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    public class InspectSetting
    {
        public bool EnableStructMatchRecognizer { get; set; }
        public bool EnableRuleBasedRecognizer { get; set; }
        public bool EnableTextAnalyticRecognizer { get; set; }

        public List<string> MatchExpressions { get; set; }
        public List<string> MatchTypes { get; set; }
        public List<string> IgnoreExpressions { get; set; }

        public static InspectSetting CreateFromRuleSettings(Dictionary<string, object> ruleSettings)
        {
            EnsureArg.IsNotNull(ruleSettings);

            var enableStructMatchRecognizer = Convert.ToBoolean(ruleSettings.GetValueOrDefault("enableStructMatchRecognizer")?.ToString());
            var enableRuleBasedRecognizer = Convert.ToBoolean(ruleSettings.GetValueOrDefault("enableRuleBasedRecognizer")?.ToString());
            var enableTextAnalyticRecognizer = Convert.ToBoolean(ruleSettings.GetValueOrDefault("enableTextAnalyticRecognizer")?.ToString());
            var matchExpressionsString = ruleSettings.GetValueOrDefault("matchExpressions")?.ToString();
            var matchTypesString = ruleSettings.GetValueOrDefault("matchTypes")?.ToString();
            var ignoreExpressionsString = ruleSettings.GetValueOrDefault("ignoreExpressions")?.ToString();
            return new InspectSetting
            {
                EnableStructMatchRecognizer = enableStructMatchRecognizer,
                EnableRuleBasedRecognizer = enableRuleBasedRecognizer,
                EnableTextAnalyticRecognizer = enableTextAnalyticRecognizer,
                MatchExpressions = JsonConvert.DeserializeObject<List<string>>(matchExpressionsString),
                MatchTypes = JsonConvert.DeserializeObject<List<string>>(matchTypesString),
                IgnoreExpressions = JsonConvert.DeserializeObject<List<string>>(ignoreExpressionsString),
            };
        }
    }
}
