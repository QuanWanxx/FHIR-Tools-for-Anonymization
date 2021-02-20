﻿using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility.NerTAUtility
{
    public interface INamedEntityRecognizer
    {
        public List<Entity> RecognizeText(string text);
    }
}
