using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    public class InspectSetting
    {
        public List<string> Expressions { get; set; }
        public List<string> MathTypes { get; set; }

        public static InspectSetting CreateFromRuleSettings(Dictionary<string, object> ruleSettings)
        {
            EnsureArg.IsNotNull(ruleSettings);

            var expressionsString = ruleSettings.GetValueOrDefault("expressions")?.ToString();
            var matchTypesString = ruleSettings.GetValueOrDefault("matchTypes")?.ToString();
            return new InspectSetting
            {
                Expressions = JsonConvert.DeserializeObject<List<string>>(expressionsString),
                MathTypes = JsonConvert.DeserializeObject<List<string>>(matchTypesString),
            };
        }
    }
}
