using System;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility.Inspect;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Utility.Inspect
{
    public class SegmentUtilityTests
    {
        public static IEnumerable<object[]> GetTextAndMaxSegmentLengthForSegmentTextTest()
        {
            yield return new object[]
            {
                string.Empty, 10,
                new List<Segment>()
                {
                    new Segment() { Offset = 0, Text = string.Empty }
                }
            };
            yield return new object[]
            {
                "I love Microsoft.", 10,
                new List<Segment>()
                {
                    new Segment() { Offset = 0, Text = "I love " },
                    new Segment() { Offset = 7, Text = "Microsoft." }
                }
            };
            yield return new object[]
            {
                "I love Microsoft.", 17,
                new List<Segment>()
                {
                    new Segment() { Offset = 0, Text = "I love Microsoft." }
                }
            };
            yield return new object[]
            {
                "I love Microsoft. ", 17,
                new List<Segment>()
                {
                    new Segment() { Offset = 0, Text = "I love " },
                    new Segment() { Offset = 7, Text = "Microsoft. " }
                }
            };
            yield return new object[]
            {
                "I love Microsoft. ", 18,
                new List<Segment>()
                {
                    new Segment() { Offset = 0, Text = "I love Microsoft. " }
                }
            };
            yield return new object[]
            {
                "I love Microsoft.", 20,
                new List<Segment>()
                {
                    new Segment() { Offset = 0, Text = "I love Microsoft." }
                }
            };
            yield return new object[]
            {
                "I hate Goooooooooogle.", 10,
                new List<Segment>()
                {
                    new Segment() { Offset = 0, Text = "I hate " },
                    new Segment() { Offset = 7, Text = "Gooooooooo" },
                    new Segment() { Offset = 17, Text = "ogle." }
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidTextForSegmentTextTest()
        {
            yield return new object[] { null, 10 };
        }

        public static IEnumerable<object[]> GetInvalidMaxSegmentLengthForSegmentTextTest()
        {
            yield return new object[] { "I love Microsoft.", -1 };
            yield return new object[] { "I love Microsoft.", 0 };
        }

        public static IEnumerable<object[]> GetTextForEndOfLastSentenceOrParagraphTest()
        {
            yield return new object[] { string.Empty, 0 };
            yield return new object[] { "I love Microsoft.", 0 };
            yield return new object[] { "I love Microsoft! ", 17 };
            yield return new object[] { "I love Microsoft? ", 17 };
            yield return new object[] { "I love Microsoft\n", 17 };
            yield return new object[] { "I love Microsoft. 2020.12.25", 17 };
            yield return new object[] { "I love Microsoft", 0 };
        }

        public static IEnumerable<object[]> GetInvalidTextForEndOfLastSentenceOrParagraphTest()
        {
            yield return new object[] { null };
        }

        public static IEnumerable<object[]> GetTextForEndOfLastSpaceTest()
        {
            yield return new object[] { string.Empty, 0 };
            yield return new object[] { "I love Microsoft.", 7 };
            yield return new object[] { "I_love_Microsoft", 0 };
        }

        public static IEnumerable<object[]> GetInvalidTextForEndOfLastSpaceTest()
        {
            yield return new object[] { null };
        }

        public static IEnumerable<object[]> GetSegmentsAndRecognitionResultsForMergeSegmentRecognitionResultsTest()
        {
            yield return new object[] 
            {
                new List<Segment>()
                {
                    new Segment() { Offset = 0, Text = "I love " },
                    new Segment() { Offset = 7, Text = "Microsoft." }
                },
                new List<List<Entity>>()
                {
                    new List<Entity>(),
                    new List<Entity>()
                    {
                        new Entity() { Category = "ORG", Text = "Microsoft", Offset = 0, Length = 9, ConfidenceScore = 1.0 }
                    }
                },
                new List<Entity>()
                {
                    new Entity() { Category = "ORG", Text = "Microsoft", Offset = 7, Length = 9, ConfidenceScore = 1.0 }
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidSegmentsAndRecognitionResultsForMergeSegmentRecognitionResultsTest()
        {
            yield return new object[] { null, null };
            yield return new object[] { null, new List<List<Entity>>() };
            yield return new object[] { new List<Segment>(), null };
        }

        public static IEnumerable<object[]> GetUnmatchedSegmentsAndRecognitionResultsForMergeSegmentRecognitionResultsTest()
        {
            yield return new object[]
            {
                new List<Segment>() { new Segment(), new Segment() },
                new List<List<Entity>>() { new List<Entity>() }
            };
        }

        [Theory]
        [MemberData(nameof(GetTextAndMaxSegmentLengthForSegmentTextTest))]
        public void GivenTextAndMaxSegmentLength_WhenSegmentText_ThenTextShouldBeSegmented(string text, int maxSegmentLength, List<Segment> expectedSegments)
        {
            var processResult = SegmentUtility.SegmentText(text, maxSegmentLength);
            var expectedStr = JsonConvert.SerializeObject(expectedSegments);
            var actualStr = JsonConvert.SerializeObject(processResult);
            Assert.Equal(expectedStr, actualStr);
        }

        [Theory]
        [MemberData(nameof(GetInvalidTextForSegmentTextTest))]
        public void GivenInvalidText_WhenSegmentText_ThenArgumentNullExceptionShouldBeThrown(string text, int maxSegmentLength)
        {
            Assert.Throws<ArgumentNullException>(() => SegmentUtility.SegmentText(text, maxSegmentLength));
        }

        [Theory]
        [MemberData(nameof(GetInvalidMaxSegmentLengthForSegmentTextTest))]
        public void GivenInvalidMaxSegmentLength_WhenSegmentText_ThenArgumentOutOfRangeExceptionShouldBeThrown(string text, int maxSegmentLength)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SegmentUtility.SegmentText(text, maxSegmentLength));
        }

        [Theory]
        [MemberData(nameof(GetTextForEndOfLastSentenceOrParagraphTest))]
        public void GivenText_WhenEndOfLastSentenceOrParagraph_ThenTheEndOfLastSentenceOrParagraphShouldBeReturned(string text, int expectedEndOfLastSentenceOrParagraph)
        {
            var processResult = SegmentUtility.EndOfLastSentenceOrParagraph(text);
            Assert.Equal(expectedEndOfLastSentenceOrParagraph, processResult);
        }

        [Theory]
        [MemberData(nameof(GetInvalidTextForEndOfLastSentenceOrParagraphTest))]
        public void GivenInvalidText_WhenEndOfLastSentenceOrParagraph_ThenArgumentNullExceptionShouldBeThrown(string text)
        {
            Assert.Throws<ArgumentNullException>(() => SegmentUtility.EndOfLastSentenceOrParagraph(text));
        }

        [Theory]
        [MemberData(nameof(GetTextForEndOfLastSpaceTest))]
        public void GivenText_WhenEndOfLastSpace_ThenTheEndOfLastSpaceShouldBeReturned(string text, int expectedEndOfLastSpace)
        {
            var processResult = SegmentUtility.EndOfLastSpace(text);
            Assert.Equal(expectedEndOfLastSpace, processResult);
        }

        [Theory]
        [MemberData(nameof(GetInvalidTextForEndOfLastSpaceTest))]
        public void GivenInvalidText_WhenEndOfLastSpace_ThenArgumentNullExceptionShouldBeThrown(string text)
        {
            Assert.Throws<ArgumentNullException>(() => SegmentUtility.EndOfLastSpace(text));
        }

        [Theory]
        [MemberData(nameof(GetSegmentsAndRecognitionResultsForMergeSegmentRecognitionResultsTest))]
        public void GivenSegmentsAndRecognitionResults_WhenMergeSegmentRecognitionResults_ThenSegmentRecognitionResultsShouldBeMerged(
            List<Segment> segments, 
            List<List<Entity>> segmentRecognitionResults,
            List<Entity> expectedResults)
        {
            var processResult = SegmentUtility.MergeSegmentRecognitionResults(segments, segmentRecognitionResults);
            var expectedStr = JsonConvert.SerializeObject(expectedResults);
            var actualStr = JsonConvert.SerializeObject(processResult);
            Assert.Equal(expectedStr, actualStr);
        }

        [Theory]
        [MemberData(nameof(GetInvalidSegmentsAndRecognitionResultsForMergeSegmentRecognitionResultsTest))]
        public void GivenInvalidSegmentsAndRecognitionResults_WhenMergeSegmentRecognitionResults_ThenArgumentNullExceptionShouldBeThrown(
            List<Segment> segments,
            List<List<Entity>> segmentRecognitionResults)
        {
            Assert.Throws<ArgumentNullException>(() => SegmentUtility.MergeSegmentRecognitionResults(segments, segmentRecognitionResults));
        }

        [Theory]
        [MemberData(nameof(GetUnmatchedSegmentsAndRecognitionResultsForMergeSegmentRecognitionResultsTest))]
        public void GivenUnmatchedSegmentsAndRecognitionResults_WhenMergeSegmentRecognitionResults_ThenArgumentExceptionShouldBeThrown(
            List<Segment> segments,
            List<List<Entity>> segmentRecognitionResults)
        {
            Assert.Throws<ArgumentException>(() => SegmentUtility.MergeSegmentRecognitionResults(segments, segmentRecognitionResults));
        }
    }
}