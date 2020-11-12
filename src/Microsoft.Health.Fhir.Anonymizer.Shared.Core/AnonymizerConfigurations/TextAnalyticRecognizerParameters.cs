using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    [DataContract]
    public class TextAnalyticRecognizerParameters
    {
        [DataMember(Name = "recognizerApi")]
        public RecognizerApi RecognizerApi { get; set; }

        [DataMember(Name = "enableTextAnalyticRecognizer")]
        public bool EnableTextAnalyticRecognizer { get; set; }

        [DataMember(Name = "timeoutPerTask")]
        public int TimeoutPerTask { get; set; }

        [DataMember(Name = "timeoutPerRequest")]
        public int TimeoutPerRequest { get; set; }
    }
}