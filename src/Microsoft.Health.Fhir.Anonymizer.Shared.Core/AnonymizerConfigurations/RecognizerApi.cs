using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics
{
    [DataContract]
    public class RecognizerApi
    {
        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "key")]
        public string Key { get; set; }
    }
}
