using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility.Inspect
{
    public class SegmentUtility
    {
        public static List<Segment> SegmentText(string text, int maxSegmentLength)
        {
            var segments = new List<Segment>();
            int offset = 0;
            while (offset < text.Length)
            {
                string segmentText;
                if (text.Length - offset <= maxSegmentLength)
                {
                    segmentText = text.Substring(offset);
                }
                else
                {
                    segmentText = text.Substring(offset, maxSegmentLength);
                    var segmentLength = EndOfLastSentenceOrParagraph(segmentText);
                    if (segmentLength != 0)
                    {
                        segmentText = text.Substring(offset, segmentLength);
                    }
                    else
                    {
                        segmentLength = EndOfLastSpace(segmentText);
                        if (segmentLength != 0)
                        {
                            segmentText = text.Substring(offset, segmentLength);
                        }
                    }
                }
                segments.Add(new Segment()
                {
                    Text = segmentText,
                    Offset = offset
                });
                offset += segmentText.Length;
            }
            return segments;
        }

        public static int EndOfLastSentenceOrParagraph(string text)
        {
            return new int[] {
                text.LastIndexOf(". "), // end of last declarative sentence
                text.LastIndexOf("! "), // end of last exclamatory sentence
                text.LastIndexOf("? "), // end of last interrogative sentence
                text.LastIndexOf("\n")  // end of last paragraph
            }.Max() + 1;
        }

        public static int EndOfLastSpace(string text)
        {
            return text.LastIndexOf(" ") + 1;
        }

        public static List<Entity> MergeSegmentRecognitionResults(List<Segment> segments, List<List<Entity>> segmentRecognitionResults)
        {
            var recognitionResults = new List<Entity>();
            for (int i = 0; i < segments.Count; i++)
            {
                foreach (var entity in segmentRecognitionResults[i])
                {
                    entity.Offset += segments[i].Offset;
                    recognitionResults.Add(entity);
                }
            }
            return recognitionResults;
        }
    }
}
