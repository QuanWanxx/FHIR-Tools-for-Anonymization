using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EnsureThat;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    public class InspireSetting
    {
        public List<string> Match { get; set; }

        public static InspireSetting CreateFromRuleSettings(Dictionary<string, object> ruleSettings)
        {
            EnsureArg.IsNotNull(ruleSettings);

            var matchString = ruleSettings.GetValueOrDefault("match")?.ToString();
            var match = JsonConvert.DeserializeObject<List<string>>(matchString);
            return new InspireSetting
            {
                Match = match
            };
        }

        
    }
}
