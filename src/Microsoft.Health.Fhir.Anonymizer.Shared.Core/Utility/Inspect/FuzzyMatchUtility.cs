using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility.Inspect
{
    public class FuzzyMatchUtility
    {
        public static List<Entity> FuzzyMatch(string text, string pattern, int span = 2, double threshold = 0.6)
        {
            var tokensPattern = Tokenizer.DefaultTokenizerImpl(pattern.ToUpper());
            var tokensText = Tokenizer.DefaultTokenizerImpl(text.ToUpper());
            var entities = new List<Entity>();
            var tokenStringPattern = ConcateTokensOrdered(tokensPattern, 0, tokensPattern.Count);
            string tokenStringText;
            for (int d = -span; d <= span; d++)
            {
                var count = tokensPattern.Count + d;
                if (count <= 0)
                {
                    continue;
                }
                if (count > tokensText.Count)
                {
                    break;
                }
                for (int i = 0; i + count <= tokensText.Count; i++)
                {
                    tokenStringText = ConcateTokensOrdered(tokensText, i, count);
                    var distance = EditDistance(tokenStringPattern, tokenStringText);
                    var score = distance >= tokenStringPattern.Length ?
                                   0 :
                                   1 - ((double)distance / (double)tokenStringPattern.Length);
                    if (score >= threshold)
                    {
                        entities.Add(new Entity()
                        {
                            Category = "Category",
                            SubCategory = "Subcategory",
                            ConfidenceScore = score,
                            Length = tokensText[i + count - 1].End - tokensText[i].Start + 1,
                            Offset = tokensText[i].Start,
                            Text = text.Substring(tokensText[i].Start, tokensText[i + count - 1].End - tokensText[i].Start + 1)
                        });
                    }
                }
            }
            return Validation(entities);
        }

        private static List<Entity> Validation(List<Entity> entities)
        {
            var sortedList = new SortedList<int, Entity>();
            entities.Sort((e1, e2) =>
            {
                return e2.ConfidenceScore.CompareTo(e1.ConfidenceScore);
            });
            foreach (var entity in entities)
            {
                var firstLarger = sortedList.FirstOrDefault(x => x.Value.Offset + x.Value.Length - 1 >= entity.Offset);
                if (firstLarger.Value != null &&
                    firstLarger.Value.Offset + firstLarger.Value.Length - 1 == entity.Offset)
                {
                    continue;
                }
                if (firstLarger.Value == null ||
                    firstLarger.Value.Offset > entity.Offset + entity.Length - 1)
                {
                    sortedList.Add(entity.Offset, entity);
                }
            }
            return sortedList.Values.ToList();
        }

        private static string ConcateTokensOrdered(List<Token> tokens, int start, int end)
        {
            var tokensSelect = tokens.GetRange(start, end);
            tokensSelect.Sort((t1, t2) =>
            {
                return t1.Text.CompareTo(t2.Text);
            });

            var pattern = new StringBuilder();
            foreach (var token in tokensSelect)
            {
                pattern.Append(token.Text);
            }
            return pattern.ToString();
        }
        private static int EditDistance(string str1, string str2)
        {
            if (str1 == str2)
            {
                return 0;
            }
            else if (string.IsNullOrEmpty(str1))
            {
                return str2.Length;
            }
            else if (string.IsNullOrEmpty(str2))
            {
                return str1.Length;
            }

            var dp = new int[str1.Length + 1][];
            for (int i = 0; i < dp.Length; i++)
            {
                dp[i] = new int[str2.Length + 1];
                dp[i][0] = i;
            }

            for (int j = 0; j < dp[0].Length; j++)
            {
                dp[0][j] = j;
            }

            for (int i = 1; i < dp.Length; i++)
            {
                for (int j = 1; j < dp[0].Length; j++)
                {
                    if (str1[i - 1] == str2[j - 1])
                    {
                        dp[i][j] = dp[i - 1][j - 1];
                    }
                    else
                    {
                        dp[i][j] = Math.Min(Math.Min(dp[i - 1][j], dp[i][j - 1]), dp[i - 1][j - 1]) + 1;
                    }
                }
            }
            return dp[str1.Length][str2.Length];
        }
    }
}
