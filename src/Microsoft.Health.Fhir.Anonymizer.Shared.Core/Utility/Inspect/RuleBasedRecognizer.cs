using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.NumberWithUnit;
using Microsoft.Recognizers.Text.Sequence;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility.Inspect
{
    public class RuleBasedRecognizer : INamedEntityRecognizer
    {
        // MRN regex
        private static readonly string _mrnFormat = @"[0-9a-zA-Z]{5,}";
        private static readonly Regex _mrnRegex = new Regex($@"(?<=(MRN|mrn|Mrn):?\s*){_mrnFormat}\b");
        // DateTime regex (https://www.hl7.org/fhir/datatypes.html#dateTime)
        private static readonly string _dateTimeFormat = @"([0-9]([0-9]([0-9][1-9]|[1-9]0)|[1-9]00)|[1-9]000)(-(0[1-9]|1[0-2])(-(0[1-9]|[1-2][0-9]|3[0-1])(T([01][0-9]|2[0-3]):[0-5][0-9]:([0-5][0-9]|60)(\.[0-9]+)?(Z|(\+|-)((0[0-9]|1[0-3]):[0-5][0-9]|14:00)))?)?)";
        private static readonly Regex _dateTimeRegex = new Regex($@"\b{_dateTimeFormat}\b");
        // SSN regex (http://rion.io/2013/09/10/validating-social-security-numbers-through-regular-expressions-2)
        private static readonly string _ssnWithDashesFormat = @"(?!219-09-9999|078-05-1120)(?!666|000|9\d{2})\d{3}-(?!00)\d{2}-(?!0{4})\d{4}";
        private static readonly string _ssnWithoutDashesFormat = @"(?!219099999|078051120)(?!666|000|9\d{2})\d{3}(?!00)\d{2}(?!0{4})\d{4}";
        private static readonly Regex _ssnRegex = new Regex($@"(?<=(SSN|ssn|Ssn):?\s*)({_ssnWithDashesFormat}|{_ssnWithoutDashesFormat})\b");
        // Oid regex (https://www.hl7.org/fhir/datatypes.html#oid)
        private static readonly string _oidFormat = @"urn:oid:[0-2](\.(0|[1-9][0-9]*))+";
        private static readonly Regex _oidRegex = new Regex($@"\b{_oidFormat}\b");
        // Phone number validation
        private static readonly Func<string, string> _phoneNumberValidationRegex1 = (phoneNumber) => $@"(SNOMED CT code|LOINC code|RxNorm code)\s+'?{Regex.Escape(phoneNumber)}'?";
        private static readonly Func<string, string> _phoneNumberValidationRegex2 = (phoneNumber) => $@"code.{{,10}}'?{Regex.Escape(phoneNumber)}'?";
        private static readonly Func<string, string> _phoneNumberValidationRegex3 = (phoneNumber) => $@"id.{{,10}}'?{Regex.Escape(phoneNumber)}'?";

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

            var customResults = new List<ModelResult>();
            customResults.AddRange(RecognizeMRN(text));
            customResults.AddRange(RecognizeDateTime(text));
            customResults.AddRange(RecognizeSSN(text));
            customResults.AddRange(RecognizeOid(text));

            foreach (var modelResult in modelResults)
            {
                recognitionResults.Add(new Entity() 
                {
                    Category = modelResult.TypeName,
                    Text = modelResult.Text,
                    Offset = modelResult.Start,
                    Length = modelResult.End - modelResult.Start + 1,
                    ConfidenceScore = 0.5
                });
            }

            foreach (var customResult in customResults)
            {
                recognitionResults.Add(new Entity() 
                {
                    Category = customResult.TypeName,
                    Text = customResult.Text,
                    Offset = customResult.Start,
                    Length = customResult.End - customResult.Start + 1,
                    ConfidenceScore = 1
                });
            }

            recognitionResults = Postprocess(text, recognitionResults);

            recognitionResults = EntityProcessUtility.PreprocessEntities(recognitionResults);

            return recognitionResults;
        }

        public List<Entity> Postprocess(string text, List<Entity> entities)
        {
            foreach (var entity in entities)
            {   
                if (entity.Category == "phonenumber")
                {
                    if (Regex.IsMatch(text, _phoneNumberValidationRegex1(entity.Text), RegexOptions.IgnoreCase))
                    {
                        entity.Category = string.Empty;
                    }
                    else if (Regex.IsMatch(text, _phoneNumberValidationRegex2(entity.Text), RegexOptions.IgnoreCase))
                    {
                        entity.Category = "code";
                        entity.ConfidenceScore = 1;
                    }
                    else if (Regex.IsMatch(text, _phoneNumberValidationRegex3(entity.Text), RegexOptions.IgnoreCase))
                    {
                        entity.Category = "id";
                        entity.ConfidenceScore = 1;
                    }
                }
            }
            entities.RemoveAll(entity => string.IsNullOrEmpty(entity.Category));
            return entities;
        }

        public List<ModelResult> RecognizeMRN(string query)
        {
            return Extract(query, _mrnRegex, "mrn");
        }

        public List<ModelResult> RecognizeDateTime(string query)
        {
            return Extract(query, _dateTimeRegex, "datetime");
        }

        public List<ModelResult> RecognizeSSN(string query)
        {
            return Extract(query, _ssnRegex, "ssn");
        }

        public List<ModelResult> RecognizeOid(string query)
        {
            return Extract(query, _oidRegex, "oid");
        }

        public List<ModelResult> Extract(string query, Regex regex, string type)
        {
            var modelResults = new List<ModelResult>();
            var matches = regex.Matches(query);
            foreach (Match match in matches)
            {
                modelResults.Add(new ModelResult() {
                    Text = match.Value,
                    Start = match.Index,
                    End = match.Index + match.Length - 1,
                    TypeName = type,
                });
            }
            return modelResults;
        }
    }
} 