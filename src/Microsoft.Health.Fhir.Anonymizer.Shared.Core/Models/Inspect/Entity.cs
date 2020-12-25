using System;
using System.Security.Cryptography;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect
{
    public class Entity : IEquatable<Entity>
    {
        public string Category { get; set; }

        public string SubCategory { get; set; }

        public string Text { get; set; }

        public int Offset { get; set; }

        public int Length { get; set; }

        public double ConfidenceScore { get; set; }

        public string Recognizer { get; set; }

        public bool Equals(Entity entity)
        {
            return Category == entity.Category && SubCategory == entity.SubCategory && Text == entity.Text
                && Offset == entity.Offset && Length == entity.Length && ConfidenceScore == entity.ConfidenceScore
                && Recognizer == entity.Recognizer;
        }
    }
}