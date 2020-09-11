using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics.Html;
using Newtonsoft.Json;
using Polly;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility.NerTAUtility
{
    public class TextAnalyticRecognizer : INamedEntityRecognizer
    {
        // Class members for HTTP requests
        private readonly string _version = "v31preview1";
        private readonly int _maxLength = 5000; // byte
        private readonly HttpClient _client = new HttpClient();
        private static readonly int _maxNumberOfRetries = 6;
        protected static readonly HttpStatusCode[] _httpStatusCodesForRetrying = {
            HttpStatusCode.RequestTimeout, // 408
            HttpStatusCode.TooManyRequests, // 429
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway, // 502
            HttpStatusCode.ServiceUnavailable, // 503
            HttpStatusCode.GatewayTimeout // 504
        };

        public TextAnalyticRecognizer(RecognizerApi recognizerApi)
        {
            // Configure client
            _client.BaseAddress = new Uri(recognizerApi.Url);
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", recognizerApi.Key);
        }

        public List<Entity> RecognizeText(string text)
        {
            var segments = SegmentUtility.SegmentText(text, _maxLength);
            var segmentRecognitionResults = new List<List<Entity>>();
            foreach (var segment in segments)
            {
                segmentRecognitionResults.Add(RecognizeSegment(segment));
                // Console.WriteLine("Finished: {0} {1}", segment.DocumentId, segment.Offset);
            }
            // Merge results
            var recognitionResults = SegmentUtility.MergeSegmentRecognitionResults(segments, segmentRecognitionResults);

            return recognitionResults;
        }

        public List<Entity> RecognizeSegment(Segment segment)
        {
            string responseString = GetResponse(segment.Text).Result;
            var responseContent = JsonConvert.DeserializeObject<MicrosoftResponseContent>(responseString);
            var recognitionResult = ResponseContentToEntities(responseContent);

            //TODO : Check the inmatched issue and remove this empty return
            foreach (var entity in recognitionResult)
            {
                if (!entity.Text.Equals(segment.Text.Substring(entity.Offset, entity.Length)))
                {
                    //Console.WriteLine("{0} | {1}", segment.Text.Substring(entity.Offset, entity.Length), entity.Text);
                    return new List<Entity>();
                }
            }
            return recognitionResult;
        }

        private HttpRequestMessage CreateRequestMessage(string requestText)
        {
            var microsoftRequestDocument = new MicrosoftRequestDocument()
            {
                DocumentId = "Microsoft.Ner",
                Language = "en",
                Text = requestText
            };
            var microsoftRequestContent = new MicrosoftRequestContent()
            {
                Documents = new List<MicrosoftRequestDocument>() { microsoftRequestDocument }
            };
            var content = new StringContent(JsonConvert.SerializeObject(microsoftRequestContent), Encoding.UTF8, "application/json");
            HttpRequestMessage requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = content
            };
            return requestMessage;
        }

        private async Task<string> GetResponse(string requestText)
        {
            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => _httpStatusCodesForRetrying.Contains(r.StatusCode))
                .WaitAndRetryAsync(
                    _maxNumberOfRetries,
                    retryAttempt =>
                    {
                        Console.WriteLine("Processor: Retry");
                        return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    });

            var response = await retryPolicy.ExecuteAsync(
                    async ct => await _client.SendAsync(CreateRequestMessage(requestText), ct), CancellationToken.None);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            return responseString;
        }

        private List<Entity> ResponseContentToEntities(MicrosoftResponseContent responseContent)
        {
            var recognitionResult = new List<Entity>();
            if (responseContent.Documents.Count == 1)
            {
                var responseEntities = responseContent.Documents[0].Entities;
                foreach (var responseEntity in responseEntities)
                {
                    var entity = new Entity()
                    {
                        Category = responseEntity.Category,
                        SubCategory = responseEntity.SubCategory,
                        Text = responseEntity.Text,
                        Offset = responseEntity.Offset,
                        Length = responseEntity.Length,
                        ConfidenceScore = responseEntity.ConfidenceScore
                    };
                    if (entity.Category != string.Empty)
                    {
                        recognitionResult.Add(entity);
                    }
                }
            }
            return recognitionResult;
        }
    }
}
