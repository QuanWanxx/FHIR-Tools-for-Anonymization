using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect;
using Microsoft.Health.Fhir.Anonymizer.Core.Models.Inspect.Html;
using Newtonsoft.Json;
using Polly;
using Polly.Timeout;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility.Inspect
{
    // API version: v3.1-preview.1
    public class TextAnalyticRecognizer : INamedEntityRecognizer
    {
        // Class members for HTTP requests
        private readonly int _maxLength = 5000; // byte
        private readonly int _taskTimeout = 20000; // millisecond
        private int _requestTimeout = 5000; // millisecond
        private int _maxNumberOfRequestTimeoutRetries = 2;
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
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<TextAnalyticRecognizer>();

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

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(_taskTimeout));
            foreach (var segment in segments)
            {
                try
                {
                    segmentRecognitionResults.Add(RecognizeSegment(segment));
                }
                catch (AggregateException ae)
                {
                    ae.Handle(e =>
                    {
                        if (e is TimeoutException)
                        {
                            throw e;
                        }
                        return false;
                    });
                }
                if (cts.IsCancellationRequested)
                {
                    _logger.LogDebug($"TextAnalyticRecognizer: Total time for mutiple requests exceeded the time limit {_taskTimeout}.");
                    throw new TimeoutException();
                }
            }

            // Merge results
            var recognitionResults = SegmentUtility.MergeSegmentRecognitionResults(segments, segmentRecognitionResults);
            recognitionResults = EntityProcessUtility.PreprocessEntities(recognitionResults);
            return recognitionResults;
        }

        public List<Entity> RecognizeSegment(Segment segment)
        {
            string responseString = GetResponse(segment.Text).Result;
            var responseContent = JsonConvert.DeserializeObject<MicrosoftResponseContent>(responseString);
            var recognitionResult = ResponseContentToEntities(responseContent);

            //Check the in-matched issue
            foreach (var entity in recognitionResult)
            {
                if (!entity.Text.Equals(segment.Text.Substring(entity.Offset, entity.Length)))
                {
                    Console.WriteLine("Warning! in-matched entity: {0} | {1}", segment.Text.Substring(entity.Offset, entity.Length), entity.Text);
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
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount) =>
                    {
                        _logger.LogDebug($"TextAnalyticRecognizer: No.{retryCount} retry for unsuccessful status code.");
                    });

            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(_requestTimeout));

            var timeoutRetryPolicy = Policy<HttpResponseMessage>
               .Handle<TimeoutRejectedException>()
               .RetryAsync(
                   _maxNumberOfRequestTimeoutRetries,
                   (exception, retryCount) =>
                   {
                       _logger.LogDebug($"TextAnalyticRecognizer: No.{retryCount} retry for timeout.");
                   }
               );
            
            var policyWrap = Policy.WrapAsync(retryPolicy, timeoutRetryPolicy, timeoutPolicy);

            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = await policyWrap.ExecuteAsync(
                    async ct => await _client.SendAsync(CreateRequestMessage(requestText), ct), CancellationToken.None);
            }
            catch (TimeoutRejectedException)
            {
                _logger.LogDebug($"TextAnalyticRecognizer: {_maxNumberOfRequestTimeoutRetries} timeout retries of single request all failed.");
                throw new TimeoutException();
            }

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            return responseString;
        }

        private List<Entity> ResponseContentToEntities(MicrosoftResponseContent responseContent)
        {
            var recognitionResult = new List<Entity>();
            if (responseContent?.Documents?.Count == 1)
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
                        ConfidenceScore = responseEntity.ConfidenceScore,
                        Recognizer = "TextAnalyticRecognizer"
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
