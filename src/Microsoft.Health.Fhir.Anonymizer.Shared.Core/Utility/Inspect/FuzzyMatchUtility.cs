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
        public static List<Entity> FuzzyMatch(string text, string pattern)
        {
            var entities = new List<Entity>();
            var patternTokens = Tokenizer.DefaultTokenizerImpl(pattern.ToUpper());
            var textTokens = Tokenizer.DefaultTokenizerImpl(text.ToUpper());
            if (textTokens.Count == 0 || patternTokens.Count == 0)
            {
                return entities;
            }
            // calculate distances
            var bestMatchEditDistances = new int[textTokens.Count];
            var bestMatchPatternIndexes = new int[textTokens.Count];
            CalculateEditDistances(textTokens, patternTokens, bestMatchEditDistances, bestMatchPatternIndexes);
            // map distances to scores
            var scores = MapEditDistancesToScores(textTokens, patternTokens, bestMatchEditDistances, bestMatchPatternIndexes);
            var scoreSums = CalculateScoreSums(textTokens.Count, scores);
            // add entities
            for (var i = 0; i < textTokens.Count; i++)
            {
                for (var j = i; j < textTokens.Count; j++)
                {
                    var scoreAvg = scoreSums[i, j] / (j - i + 1);
                    var startToken = textTokens[i];
                    var endToken = textTokens[j];
                    var entityLength = endToken.End - startToken.Start + 1;
                    var spanRatio = (double)Math.Abs(entityLength - pattern.Length) / (double)pattern.Length;
                    spanRatio = spanRatio > 1 ? 1 : spanRatio;
                    if (scoreAvg > 0.8 && spanRatio <= 0.4)
                    {
                        entities.Add(new Entity()
                        {
                            Category = "Category",
                            SubCategory = "Subcategory",
                            ConfidenceScore = scoreAvg * (1 - spanRatio),
                            Length = entityLength,
                            Offset = startToken.Start,
                            Text = text.Substring(startToken.Start, entityLength)
                        });
                    }
                }
            }
            return Validation(entities);
        }

        private static void CalculateEditDistances(List<Token> textTokens, List<Token> patternTokens, int[] bestMatchEditDistances, int[] bestMatchPatternIndexes)
        {
            for (var i = 0; i < textTokens.Count; i++)
            {
                bestMatchEditDistances[i] = int.MaxValue;
            }
            for (var i = 0; i < patternTokens.Count; i++)
            {
                for (var j = 0; j < textTokens.Count; j++)
                {
                    var distance = EditDistance(patternTokens[i].Text, textTokens[j].Text);
                    if (distance < bestMatchEditDistances[j])
                    {
                        bestMatchEditDistances[j] = distance;
                        bestMatchPatternIndexes[j] = i;
                    }
                }
            }
        }

        private static double[] MapEditDistancesToScores(List<Token> textTokens, List<Token> patternTokens, int[] bestMatchEditDistances, int[] bestMatchPatternIndexes)
        {
            var scores = new double[textTokens.Count];
            for (var i = 0; i < textTokens.Count; i++)
            {
                scores[i] = 1 - (double)bestMatchEditDistances[i] / (double)(Math.Max(textTokens[i].Text.Length, patternTokens[bestMatchPatternIndexes[i]].Text.Length));
            }
            return scores;
        }

        private static double[,] CalculateScoreSums(int textTokensCount, double[] scores)
        {
            var scoreSums = new double[textTokensCount, textTokensCount];
            for (var i = 0; i < textTokensCount; i++)
            {
                for (var j = i; j < textTokensCount; j++)
                {
                    if (j == 0)
                    {
                        scoreSums[i, j] = scores[j];
                    }
                    else
                    {
                        scoreSums[i, j] = scoreSums[i, j - 1] + scores[j];
                    }
                }
            }
            return scoreSums;
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