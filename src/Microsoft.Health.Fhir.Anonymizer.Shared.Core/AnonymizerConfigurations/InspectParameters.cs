using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    [DataContract]
    public class InspectParameters
    {
        [DataMember(Name = "structMatchRecognizerParameters")]
        public StructMatchRecognizerParameters StructMatchRecognizerParameters { get; set; }

        [DataMember(Name = "textAnalyticRecognizerParameters")]
        public TextAnalyticRecognizerParameters TextAnalyticRecognizerParameters { get; set; }

        [DataMember(Name = "ruleBasedRecognizerParameters")]
        public RuleBasedRecognizerParameters RuleBasedRecognizerParameters { get; set; }
    }
}
