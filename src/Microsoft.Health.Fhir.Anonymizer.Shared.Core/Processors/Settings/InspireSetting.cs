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
        public List<string> Expressions { get; set; }

        public static InspireSetting CreateFromRuleSettings(Dictionary<string, object> ruleSettings)
        {
            EnsureArg.IsNotNull(ruleSettings);

            var expressionsString = ruleSettings.GetValueOrDefault("expressions")?.ToString();
            var expressions = JsonConvert.DeserializeObject<List<string>>(expressionsString);
            return new InspireSetting
            {
                Expressions = expressions
            };
        }

        
    }
}
