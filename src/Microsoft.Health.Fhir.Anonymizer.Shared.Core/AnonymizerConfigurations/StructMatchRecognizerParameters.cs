using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    [DataContract]
    public class StructMatchRecognizerParameters
    {
        [DataMember(Name = "enableStructMatchRecognizer")]
        public bool EnableStructMatchRecognizer { get; set; }

        [DataMember(Name = "enableFuzzyMatch")]
        public bool EnableFuzzyMatch { get; set; }
    }
}