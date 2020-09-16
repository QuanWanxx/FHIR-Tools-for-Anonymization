using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility
{
    public interface INamedEntityRecognizer
    {
        public List<Entity> RecognizeText(string text);
    }
}