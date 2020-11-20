using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility.Inspect
{
    // Used to handle the overlap entities
    // The task is similiar to an task  https://leetcode.com/problems/the-skyline-problem/
    // Solution refers to the discussion under this problem.
    class EntityProcessUtility
    {
        struct Event
        {
            public int index;
            public int position;
            public double score;

            public Event(int index, int position, double score)
            {
                this.index = index;
                this.position = position;
                this.score = score;
            }
        }

        public static string ProcessEntities(string originText, IEnumerable<Entity> entities)
        {
            if (string.IsNullOrWhiteSpace(originText))
            {
                return originText;
            }

            var result = new StringBuilder();
            // Use StringInfo to avoid offset issues https://docs.microsoft.com/en-us/azure/cognitive-services/text-analytics/concepts/text-offsets
            var text = new StringInfo(originText);
            var startIndex = 0;
            foreach (var entity in entities)
            {
                result.Append(text.SubstringByTextElements(startIndex, entity.Offset - startIndex));
                result.Append($"[{entity.Category.ToUpperInvariant()}]");
                startIndex = entity.Offset + entity.Length;
            }
            if (startIndex < text.LengthInTextElements)
            {
                result.Append(text.SubstringByTextElements(startIndex));
            }
            return result.ToString();
        }

        // Process overlap entities
        // E.g.
        // Raw entities: {("MS 100 University", "ORG", 0.2), ("100", "AGE", 0.5)}
        // Processed entities: {("MS ", "ORG", 0.2), ("100", "AGE", 0.5), (" University", "ORG", 0.2)}
        public static List<Entity> PreprocessEntities(List<Entity> entities)
        {
            // Get the entering and leaving events for entities
            var events = new List<Event>();
            int index = 0;
            foreach (var entity in entities)
            {
                events.Add(new Event(index, entity.Offset, entity.ConfidenceScore));
                events.Add(new Event(index, entity.Offset + entity.Length, -entity.ConfidenceScore));
                index++;
            }
            events.Sort((e1, e2) =>
            {
                if (e1.position == e2.position)
                {
                    return e2.score.CompareTo(e1.score);
                }
                else
                {
                    return e1.position.CompareTo(e2.position);
                }
            });

            // Find booundary records
            var boundaries = new List<Tuple<int, int>>();
            var heapInstance = new Dictionary<int, double>();
            foreach (var e in events)
            {
                bool entering = e.score > 0;
                double score = Math.Abs(e.score);
                if (entering)
                {
                    var maxInstance = MaxInstance(heapInstance);
                    if (score > maxInstance.Item2)
                    {
                        boundaries.Add(new Tuple<int, int>(e.position, e.index));
                    }
                    heapInstance.Add(e.index, e.score);
                }
                else
                {
                    heapInstance.Remove(e.index);
                    var maxInstance = MaxInstance(heapInstance);
                    if (score >= maxInstance.Item2)
                    {
                        boundaries.Add(new Tuple<int, int>(e.position, maxInstance.Item1));
                    }
                }
            }

            // Generate the processed entities
            var result = new List<Entity>();
            for (int i = 1; i < boundaries.Count; i++)
            {
                index = boundaries[i - 1].Item2;
                if (index == -1)
                {
                    continue;
                }
                var start = boundaries[i - 1].Item1;
                var end = boundaries[i].Item1;
                var originOffset = entities[index].Offset;
                var entity = new Entity()
                {
                    Category = entities[index].Category,
                    SubCategory = entities[index].SubCategory,
                    Text = entities[index].Text.Substring(start - originOffset, end - start),
                    Offset = start,
                    Length = end - start,
                    ConfidenceScore = entities[index].ConfidenceScore,
                    Recognizer = entities[index].Recognizer
                };

                // Ignore the entities don't contain information
                if (Regex.Matches(entity.Text, @"[a-zA-Z0-9]").Count > 0)
                {
                    result.Add(entity);
                }
            }
            return result;
        }

        public static List<Entity> PostprocessEntities(List<Entity> entities, stripInfo stripInfo)
        {
            var result = new List<Entity>();
            int shiftLength = 0;
            int i = 0;
            foreach (var entity in entities)
            {
                while (i < stripInfo.SkipPositions.Count)
                {
                    if (entity.Offset >= stripInfo.SkipPositions[i].Index)
                    {
                        shiftLength += stripInfo.SkipPositions[i].Length;
                        i++;
                    }
                    else if (entity.Offset + entity.Length <= stripInfo.SkipPositions[i].Index)
                    {
                        entity.Offset += shiftLength;
                        result.Add(entity);
                        break;
                    }
                    else
                    {
                        var text = entity.Text.Substring(0, stripInfo.SkipPositions[i].Index - entity.Offset);
                        if (text.Trim().Length == 0) break;
                        result.Add(new Entity() {
                            Category = entity.Category,
                            SubCategory = entity.SubCategory,
                            Text = text,
                            Offset = entity.Offset + shiftLength,
                            Length = stripInfo.SkipPositions[i].Index - entity.Offset,
                            ConfidenceScore = entity.ConfidenceScore,
                            Recognizer = entity.Recognizer
                        });
                        entity.Text = entity.Text.Substring(stripInfo.SkipPositions[i].Index - entity.Offset + 1);
                        entity.Offset = stripInfo.SkipPositions[i].Index + 1;
                        entity.Length = entity.Text.Length;
                        shiftLength += stripInfo.SkipPositions[i].Length;
                        i++;
                    }
                }
                if (i >= stripInfo.SkipPositions.Count)
                {
                    entity.Offset += shiftLength;
                }
            }
            return result;
        }

        private static Tuple<int, double> MaxInstance(Dictionary<int, double> heapInstance)
        {
            double maxScore = 0;
            int maxIndex = -1;
            foreach (var tem in heapInstance)
            {
                if (tem.Value > maxScore)
                {
                    maxScore = tem.Value;
                    maxIndex = tem.Key;
                }
            }
            return new Tuple<int, double>(maxIndex, maxScore);
        }
    }
}