using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    public class InspectSetting
    {
        public List<string> MatchExpressions { get; set; }
        public List<string> MathTypes { get; set; }
        public List<string> IgnoreExpressions { get; set; }

        public static InspectSetting CreateFromRuleSettings(Dictionary<string, object> ruleSettings)
        {
            EnsureArg.IsNotNull(ruleSettings);

            var matchExpressionsString = ruleSettings.GetValueOrDefault("matchExpressions")?.ToString();
            var matchTypesString = ruleSettings.GetValueOrDefault("matchTypes")?.ToString();
            var ignoreExpressionsString = ruleSettings.GetValueOrDefault("ignoreExpressions")?.ToString();
            return new InspectSetting
            {
                MatchExpressions = JsonConvert.DeserializeObject<List<string>>(matchExpressionsString),
                MathTypes = JsonConvert.DeserializeObject<List<string>>(matchTypesString),
                IgnoreExpressions = JsonConvert.DeserializeObject<List<string>>(ignoreExpressionsString),
            };
        }
    }
}
