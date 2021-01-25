using System;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility.Inspect;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Utility.Inspect
{
    public class HtmlTextUtilityTests
    {
        public static IEnumerable<object[]> GetHtmlForStripTagsTest()
        {
            yield return new object[] 
            { 
                string.Empty, 
                new StripInfo() { SkipPositions = new List<SkipPosition>(), StrippedText = string.Empty } 
            };
            yield return new object[]
            {
                "<div></div>",
                new StripInfo() 
                { 
                    SkipPositions = new List<SkipPosition>()
                    {
                        new SkipPosition() { Index = 0, Length = 10 }
                    },
                    StrippedText = " " 
                }
            };
            yield return new object[]
            {
                "<div>A<b>B</b><span></span>C</div>",
                new StripInfo()
                {
                    SkipPositions = new List<SkipPosition>()
                    {
                        new SkipPosition() { Index = 0, Length = 4 },
                        new SkipPosition() { Index = 2, Length = 2 },
                        new SkipPosition() { Index = 4, Length = 16 },
                        new SkipPosition() { Index = 6, Length = 5 }
                    },
                    StrippedText = " A B C "
                }
            };
            yield return new object[]
            {
                "<a href=\"https://microsoft.com\">Microsoft</a>",
                new StripInfo()
                {
                    SkipPositions = new List<SkipPosition>()
                    {
                        new SkipPosition() { Index = 0, Length = 31 },
                        new SkipPosition() { Index = 10, Length = 3}
                    },
                    StrippedText = " Microsoft "
                }
            };
        }

        [Theory]
        [MemberData(nameof(GetHtmlForStripTagsTest))]
        public void GivenHtml_WhenStripTags_ThenTagsShouldBeStripped(string html, StripInfo expectedStripInfo)
        {
            var processResult = HtmlTextUtility.StripTags(html);
            var expectedStr = JsonConvert.SerializeObject(expectedStripInfo);
            var actualStr = JsonConvert.SerializeObject(processResult);
            Assert.Equal(expectedStr, actualStr);
        }
    }
}
