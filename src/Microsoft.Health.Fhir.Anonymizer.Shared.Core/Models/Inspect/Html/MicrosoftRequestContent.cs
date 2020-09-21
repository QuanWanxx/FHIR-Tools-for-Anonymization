using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect.Html
{
    [DataContract]
    public class MicrosoftRequestContent
    {
        [DataMember(Name = "documents")]
        public List<MicrosoftRequestDocument> Documents { get; set; }
    }
}
