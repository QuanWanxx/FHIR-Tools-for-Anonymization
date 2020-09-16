using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.Number;
using Microsoft.Recognizers.Text.NumberWithUnit;
using Microsoft.Recognizers.Text.Sequence;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility
{
    public class TextRecognizer : INamedEntityRecognizer
    {
        public List<Entity> RecognizeText(string text)
        {
            var culture = Culture.English;
            var recognitionResults = new List<Entity>();

            var modelResults = new List<ModelResult>();
            modelResults.AddRange(NumberWithUnitRecognizer.RecognizeAge(text, culture));
            modelResults.AddRange(DateTimeRecognizer.RecognizeDateTime(text, culture));
            modelResults.AddRange(SequenceRecognizer.RecognizePhoneNumber(text, culture));
            modelResults.AddRange(SequenceRecognizer.RecognizeIpAddress(text, culture));
            modelResults.AddRange(SequenceRecognizer.RecognizeEmail(text, culture));
            modelResults.AddRange(SequenceRecognizer.RecognizeGUID(text, culture));
            modelResults.AddRange(SequenceRecognizer.RecognizeURL(text, culture));

            foreach (var modelResult in modelResults)
            {
                recognitionResults.Add(new Entity() 
                {
                    Category = modelResult.TypeName,
                    Text = modelResult.Text,
                    Offset = modelResult.Start,
                    Length = modelResult.End - modelResult.Start + 1,
                    ConfidenceScore = 1
                });
            }

            recognitionResults = EntityProcessUtility.ProcessEntities(recognitionResults);

            // foreach (var entity in recognitionResults)
            // {
            //     Console.WriteLine($"{entity.Text} {entity.Category} {entity.Offset} {entity.Length}");
            // }
            
            return recognitionResults;
        }
    }
}