using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    [DataContract]
    public class RuleBasedRecognizerParameters
    {
        [DataMember(Name = "enableRuleBasedRecognizer")]
        public bool EnableRuleBasedRecognizer { get; set; }

        [DataMember(Name = "timeout")]
        public int Timeout { get; set; }
    }
}