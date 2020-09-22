using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.NumberWithUnit;
using Microsoft.Recognizers.Text.Sequence;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility
{
    public class TextRecognizer : INamedEntityRecognizer
    {
        public string[] IdRegex = { @"MRN", @"code", @"id" };
        public int WindowBefore = 10;
        public int WindowAfter = 0;

        public List<Entity> RecognizeText(string text)
        {
            var culture = Culture.English;

            var modelResults = new List<ModelResult>();
            modelResults.AddRange(NumberWithUnitRecognizer.RecognizeAge(text, culture));
            modelResults.AddRange(DateTimeRecognizer.RecognizeDateTime(text, culture));
            modelResults.AddRange(SequenceRecognizer.RecognizePhoneNumber(text, culture));
            modelResults.AddRange(SequenceRecognizer.RecognizeIpAddress(text, culture));
            modelResults.AddRange(SequenceRecognizer.RecognizeEmail(text, culture));
            modelResults.AddRange(SequenceRecognizer.RecognizeGUID(text, culture));
            modelResults.AddRange(SequenceRecognizer.RecognizeURL(text, culture));

            var recognitionResults = new List<Entity>();
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

            recognitionResults = Postprocess(text, recognitionResults);

            recognitionResults = EntityProcessUtility.ProcessEntities(recognitionResults);
            
            return recognitionResults;
        }

        public List<Entity> Postprocess(string text, List<Entity> entities)
        {
            foreach (var entity in entities)
            {   
                if (entity.Category == "phonenumber")
                {
                    var startIndex = Math.Max(0, entity.Offset - WindowBefore);
                    var endIndex = Math.Min(text.Length, entity.Offset + entity.Length + WindowAfter);
                    if (IdRegex.Any(regex => Regex.IsMatch(text.Substring(startIndex, endIndex - startIndex), regex, RegexOptions.IgnoreCase)))
                    {
                        entity.Category = "id";
                    }
                }
            }
            return entities;
        }
    }
}