using System;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility.Inspect;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Utility.Inspect
{
    public class EntityProcessUtilityTests
    {
        public static IEnumerable<object[]> GetEntitiesForResolveEntitiesTest()
        {
            yield return new object[] { new List<Entity>(), new List<Entity>() };
            yield return new object[] 
            { 
                new List<Entity>()
                {
                    new Entity() { Category = "ORG", Text = "University", Offset = 4, Length = 10, ConfidenceScore = 0.2 },
                    new Entity() { Category = "AGE", Text = "100", Offset = 0, Length = 3, ConfidenceScore = 0.5 }
                }, 
                new List<Entity>()
                {
                    new Entity() { Category = "AGE", Text = "100", Offset = 0, Length = 3, ConfidenceScore = 0.5 },
                    new Entity() { Category = "ORG", Text = "University", Offset = 4, Length = 10, ConfidenceScore = 0.2 }
                } 
            };
            yield return new object[] 
            { 
                new List<Entity>()
                {
                    new Entity() { Category = "ORG", Text = "MS 100 University", Offset = 0, Length = 17, ConfidenceScore = 0.2 },
                    new Entity() { Category = "AGE", Text = "100", Offset = 3, Length = 3, ConfidenceScore = 0.5 }
                }, 
                new List<Entity>()
                {
                    new Entity() { Category = "ORG", Text = "MS ", Offset = 0, Length = 3, ConfidenceScore = 0.2 },
                    new Entity() { Category = "AGE", Text = "100", Offset = 3, Length = 3, ConfidenceScore = 0.5 },
                    new Entity() { Category = "ORG", Text = " University", Offset = 6, Length = 11, ConfidenceScore = 0.2}
                }
            };
        }

        public static IEnumerable<object[]> GetEntitiesAndStripInfoForShiftEntitiesTest()
        {
            yield return new object[] 
            {
                new List<Entity>(),
                new StripInfo() { SkipPositions = new List<SkipPosition>(), StrippedText = string.Empty },
                new List<Entity>(),
            };
            // "<div>Cathy <span> </span><b>Jones</b></div>"
            // " Cathy     Jones  "
            yield return new object[]
            {
                new List<Entity>()
                {
                    new Entity() { Category = "NAME", Text = "Cathy     Jones", Offset = 1, Length = 15, ConfidenceScore = 0.9 }
                },
                new StripInfo()
                {
                    SkipPositions = new List<SkipPosition>()
                    { 
                        new SkipPosition() { Index = 0, Length = 4 },
                        new SkipPosition() { Index = 7, Length = 5 },
                        new SkipPosition() { Index = 9, Length = 6 },
                        new SkipPosition() { Index = 10, Length = 2 },
                        new SkipPosition() { Index = 16, Length = 3 },
                        new SkipPosition() { Index = 17, Length = 5 }
                    },
                    StrippedText = " Cathy     Jones  "
                },
                new List<Entity>()
                {
                    new Entity() { Category = "NAME", Text = "Cathy ", Offset = 5, Length = 6, ConfidenceScore = 0.9 },
                    new Entity() { Category = "NAME", Text = "Jones", Offset = 28, Length = 5, ConfidenceScore = 0.9 }
                },
            };
        }

        public static IEnumerable<object[]> GetTextAndEntitiesForReplaceEntitiesTest()
        {
            yield return new object[] { string.Empty, new List<Entity>(), string.Empty };
            yield return new object[] { "100 University", new List<Entity>(), "100 University" };
            yield return new object[] 
            {
                "100 University",
                new List<Entity>()
                {
                    new Entity() { Category = "AGE", Text = "100", Offset = 0, Length = 3, ConfidenceScore = 0.5 },
                    new Entity() { Category = "ORG", Text = "University", Offset = 4, Length = 10, ConfidenceScore = 0.2 }
                },
                "[AGE] [ORG]"
            };
        }

        [Theory]
        [MemberData(nameof(GetEntitiesForResolveEntitiesTest))]
        public void GivenEntities_WhenResolveEntities_ThenEntitiesShouldBeResolved(List<Entity> entities, List<Entity> expectedEntities)
        {
            var processResult = EntityProcessUtility.ResolveEntities(entities);
            Assert.Equal(expectedEntities, processResult);
        }

        [Theory]
        [MemberData(nameof(GetEntitiesAndStripInfoForShiftEntitiesTest))]
        public void GivenEntitiesAndStripInfo_WhenShiftEntities_ThenEntitiesShouldBeShifted(List<Entity> entities, StripInfo stripInfo, List<Entity> expectedEntities)
        {
            var processResult = EntityProcessUtility.ShiftEntities(entities, stripInfo);
            Assert.Equal(expectedEntities, processResult);
        }

        [Theory]
        [MemberData(nameof(GetTextAndEntitiesForReplaceEntitiesTest))]
        public void GivenTextAndEntities_WhenReplaceEntities_ThenEntitiesInTextShouldBeReplaced(string text, List<Entity> entities, string expectedText)
        {
            var processResult = EntityProcessUtility.ReplaceEntities(text, entities);
            Assert.Equal(expectedText, processResult);
        }
    }
}
