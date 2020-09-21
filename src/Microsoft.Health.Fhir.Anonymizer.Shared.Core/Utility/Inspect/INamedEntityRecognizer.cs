using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility.Inspect
{
    public interface INamedEntityRecognizer
    {
        public List<Entity> RecognizeText(string text);
    }
}
