namespace Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect
{
    public class Entity
    {
        public string Category { get; set; }

        public string SubCategory { get; set; }

        public string Text { get; set; }

        public int Offset { get; set; }

        public int Length { get; set; }

        public double ConfidenceScore { get; set; }

        public string Recognizer { get; set; }
    }
}