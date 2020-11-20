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
        public static List<Entity> FuzzyMatch1(string text, string pattern, int span = 2, double threshold = 0.6)
        {
            var tokensPattern = Tokenizer.DefaultTokenizerImpl(pattern.ToUpper());
            var tokensText = Tokenizer.DefaultTokenizerImpl(text.ToUpper());
            var entities = new List<Entity>();
            var tokenStringPattern = ConcateTokensOrdered(tokensPattern, 0, tokensPattern.Count);
            string tokenStringText;

            // Do not do FuzzyMatch if the length of pattern less than 4
            if (tokenStringPattern.Length < 4)
            {
                return entities;
            }

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

        public static List<Entity> FuzzyMatch2(string text, string pattern, int span = 2, double threshold = 0.6)
        {
            var entities = new List<Entity>();
            var tokensPattern = Tokenizer.DefaultTokenizerImpl(pattern.ToUpper());
            var tokensText = Tokenizer.DefaultTokenizerImpl(text.ToUpper());
            if (tokensText.Count == 0 || tokensPattern.Count == 0)
            {
                return entities;
            }
            var bestMatchEditDistances = new int[tokensText.Count];
            var bestMatchIndexes = new int[tokensText.Count];
            for (var i = 0; i < tokensText.Count; i++)
            {
                bestMatchEditDistances[i] = int.MaxValue;
            }
            for (var i = 0; i < tokensPattern.Count; i++)
            {
                for (var j = 0; j < tokensText.Count; j++)
                {
                    var distance = EditDistance(tokensPattern[i].Text, tokensText[j].Text);
                    if (distance < bestMatchEditDistances[j])
                    {
                        bestMatchEditDistances[j] = distance;
                        bestMatchIndexes[j] = i;
                    }
                }
            }
            var scores = new double[tokensText.Count];
            for (var i = 0; i < tokensText.Count; i++)
            {
                // threshold
                scores[i] = 0.2 - (double)bestMatchEditDistances[i] / (double)(Math.Max(tokensText[i].Text.Length, tokensPattern[bestMatchIndexes[i]].Text.Length));
            }
            var scoreSumsAndStartIndexes = new ScoreSumAndStartIndex[tokensText.Count];
            for (var i = 0; i < tokensText.Count; i++)
            {
                scoreSumsAndStartIndexes[i] = new ScoreSumAndStartIndex();
            }
            scoreSumsAndStartIndexes[0].ScoreSum = scores[0];
            scoreSumsAndStartIndexes[0].StartIndex = 0;
            for (var i = 1; i < tokensText.Count; i++)
            {
                if (scoreSumsAndStartIndexes[i-1].ScoreSum > 0)
                {
                    // sum
                    scoreSumsAndStartIndexes[i].ScoreSum = scores[i] + scoreSumsAndStartIndexes[i-1].ScoreSum;
                    scoreSumsAndStartIndexes[i].StartIndex = scoreSumsAndStartIndexes[i-1].StartIndex;
                }
                else
                {
                    scoreSumsAndStartIndexes[i].ScoreSum = scores[i];
                    scoreSumsAndStartIndexes[i].StartIndex = i;
                }
            }
            var k = tokensText.Count - 1;
            while(k >= 0)
            {
                // 这种计算方式会导致尽可能长的匹配
                var scoreSum = scoreSumsAndStartIndexes[k].ScoreSum;
                var startIndex = scoreSumsAndStartIndexes[k].StartIndex;
                var tokenCount = k - startIndex + 1;
                var entityLength = tokensText[k].End - tokensText[startIndex].Start + 1;
                if (scoreSum > 0)
                {
                    var patternHit = new int[tokensPattern.Count];
                    for (var i = startIndex; i <= k; i++)
                    {
                        patternHit[bestMatchIndexes[i]] = 1;
                    }
                    var hitCount = patternHit.Sum();
                    // span for length is more reasonable
                    var spanRatio = (double)Math.Abs(entityLength - pattern.Length)/(double)pattern.Length;
                    spanRatio = spanRatio > 1 ? 1 : spanRatio;
                    // dynamic span limit 
                    if (spanRatio <= 0.4)
                    {
                        entities.Add(new Entity()
                        {
                            Category = "Category",
                            SubCategory = "Subcategory",
                            // (-0.8 ~ 0.2) + 0.8
                            ConfidenceScore = (scoreSum / tokenCount + 0.8) * (1 - spanRatio),
                            Length = tokensText[k].End - tokensText[startIndex].Start + 1,
                            Offset = tokensText[startIndex].Start,
                            Text = text.Substring(tokensText[startIndex].Start, entityLength)
                        });
                    }
                    // k = startIndex - 1;
                    k--;
                }
                else
                {
                    k--;
                }
            }
            return Validation(entities);
        }

        public static List<Entity> FuzzyMatch3(string text, string pattern, int span = 2, double threshold = 0.6)
        {
            var entities = new List<Entity>();
            var tokensPattern = Tokenizer.DefaultTokenizerImpl(pattern.ToUpper());
            var tokensText = Tokenizer.DefaultTokenizerImpl(text.ToUpper());
            if (tokensText.Count == 0 || tokensPattern.Count == 0)
            {
                return entities;
            }
            var bestMatchEditDistances = new int[tokensText.Count];
            var bestMatchIndexes = new int[tokensText.Count];
            for (var i = 0; i < tokensText.Count; i++)
            {
                bestMatchEditDistances[i] = int.MaxValue;
            }
            for (var i = 0; i < tokensPattern.Count; i++)
            {
                for (var j = 0; j < tokensText.Count; j++)
                {
                    var distance = EditDistance(tokensPattern[i].Text, tokensText[j].Text);
                    if (distance < bestMatchEditDistances[j])
                    {
                        bestMatchEditDistances[j] = distance;
                        bestMatchIndexes[j] = i;
                    }
                }
            }
            var scores = new double[tokensText.Count];
            for (var i = 0; i < tokensText.Count; i++)
            {
                scores[i] = 0.5 - (double)bestMatchEditDistances[i] / (double)(Math.Max(tokensText[i].Text.Length, tokensPattern[bestMatchIndexes[i]].Text.Length));
            }
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
                double scoreSum = 0;
                var patternHit = new int[tokensPattern.Count];
                for (int i = 0; i < count; i++)
                {
                    scoreSum += scores[i];
                    if (scores[i] > 0)
                    {
                        patternHit[bestMatchIndexes[i]]++;
                    }
                }
                var score = scoreSum / count;
                if (score > 0)
                {
                    entities.Add(new Entity()
                    {
                        Category = "Category",
                        SubCategory = "Subcategory",
                        ConfidenceScore = score,
                        Length = tokensText[count - 1].End - tokensText[0].Start + 1,
                        Offset = tokensText[0].Start,
                        Text = text.Substring(tokensText[0].Start, tokensText[count - 1].End - tokensText[0].Start + 1)
                    });
                }
                for (int i = count; i < tokensText.Count; i++)
                {
                    // threshold
                    scoreSum = scoreSum - scores[i - count] + scores[i];
                    if (scores[i - count] > 0)
                    {
                        patternHit[bestMatchIndexes[i - count]]--;
                    }
                    if (scores[i] > 0)
                    {
                        patternHit[bestMatchIndexes[i]]++;
                    }
                    score = scoreSum / count; 
                    // span = 2 3-2=1
                    // A B C    A A A
                    // 这种score算法会导致1个token完全匹配时得分最高，需要引入token数的差别到score中
                    if (score > 0)
                    {
                        entities.Add(new Entity()
                        {
                            Category = "Category",
                            SubCategory = "Subcategory",
                            ConfidenceScore = score,
                            Length = tokensText[i].End - tokensText[i - count + 1].Start + 1,
                            Offset = tokensText[i - count + 1].Start,
                            Text = text.Substring(tokensText[i - count + 1].Start, tokensText[i].End - tokensText[i - count + 1].Start + 1)
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

    public class ScoreSumAndStartIndex
    {
        public double ScoreSum { get; set; }

        public int StartIndex { get; set; }
    }
}