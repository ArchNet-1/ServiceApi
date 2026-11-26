// ArchNet · API Service
// Calls a web API with JSON in and out. The Hub creates GameApi (your class) where each
// endpoint is one line:
//
//   public Task<Profile> Profile() => Get<Profile>("profile");
//   Profile me = await Services.Get<GameApi>().Profile();
//
// Paths are relative to the active environment's base URL (ApiSettings). Network errors and
// server errors (5xx, 429) are tried again a few times; any other failure throws an
// ApiException with the HTTP status and the answer's body. Results are read with JsonUtility:
// [Serializable] classes, no top-level arrays (wrap them: { "items": [ … ] }).
// Docs: https://archnet-plateform.fr/#ServiceApi

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ArchNet.Core;
using UnityEngine;
using UnityEngine.Networking;

[assembly: InternalsVisibleTo("ArchNet.Service.Api.Tests")]

namespace ArchNet.Service
{
    public sealed class ApiException : Exception
    {
        /// <summary>HTTP status, or 0 when the server could not be reached.</summary>
        public long StatusCode { get; }
        public string Body { get; }

        public ApiException(long statusCode, string message, string body) : base(message)
        {
            StatusCode = statusCode;
            Body = body;
        }
    }

    public readonly struct ApiResponse
    {
        public readonly long StatusCode;
        public readonly string Body;
        public readonly string Error;
        public readonly bool NetworkError;

        public ApiResponse(long statusCode, string body, string error, bool networkError)
        {
            StatusCode = statusCode;
            Body = body;
            Error = error;
            NetworkError = networkError;
        }

        public bool IsSuccess => !NetworkError && StatusCode >= 200 && StatusCode < 300;
    }

    public class ApiService : GameService
    {
        [SerializeField] ApiSettings _settings;

        readonly Dictionary<string, string> _headers = new Dictionary<string, string>();

        /// <summary>Sent as "Authorization: Bearer …" when set. Kept in memory only, never saved.</summary>
        public string AuthToken { get; set; }

        /// <summary>Extra headers sent with every request.</summary>
        public IDictionary<string, string> Headers => _headers;

        public ApiSettings Settings => _settings;

        public string BaseUrl => _settings != null
            ? _settings.Active.baseUrl
            : throw new InvalidOperationException("The API service has no ApiSettings assigned (ArchNet > Hub > Services > API > Open tool).");

        internal void Configure(ApiSettings settings) => _settings = settings;

        public Task<T> Get<T>(string path) => Send<T>("GET", path, null);
        public Task<T> Post<T>(string path, object body) => Send<T>("POST", path, body);
        public Task<T> Put<T>(string path, object body) => Send<T>("PUT", path, body);
        public Task Delete(string path) => Send<string>("DELETE", path, null);

        /// <summary>The full URL of a path: absolute URLs are kept, others go after the base URL.</summary>
        public string UrlFor(string path)
        {
            if (path != null && (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                return path;
            return BaseUrl.TrimEnd('/') + "/" + (path ?? "").TrimStart('/');
        }

        async Task<T> Send<T>(string method, string path, object body)
        {
            string url = UrlFor(path);
            string json = body == null ? null : body as string ?? JsonUtility.ToJson(body);
            int retries = _settings != null ? _settings.retries : 0;
            for (int attempt = 0; ; attempt++)
            {
                ApiResponse response = await Transport(method, url, json);
                if (response.IsSuccess) return Parse<T>(response.Body, url);
                if (attempt >= retries || !ShouldRetry(response))
                {
                    string reason = response.NetworkError ? response.Error : $"HTTP {response.StatusCode}";
                    throw new ApiException(response.StatusCode, $"{method} {url} failed: {reason}", response.Body);
                }
                int delay = RetryDelayMs(attempt);
                if (delay > 0) await Task.Delay(delay);
            }
        }

        internal static bool ShouldRetry(ApiResponse response) =>
            response.NetworkError || response.StatusCode >= 500 || response.StatusCode == 429;

        int RetryDelayMs(int attempt) => _settings == null ? 0 : (int)(_settings.retryDelaySeconds * 1000f) << attempt;

        static T Parse<T>(string body, string url)
        {
            if (typeof(T) == typeof(string)) return (T)(object)body;
            if (string.IsNullOrWhiteSpace(body)) return default;
            try
            {
                return JsonUtility.FromJson<T>(body);
            }
            catch (ArgumentException exception)
            {
                throw new ApiException(200, $"{url} did not answer JSON for {typeof(T).Name}: {exception.Message}", body);
            }
        }

        /// <summary>Sends one request. Tests and custom transports (signed requests…) override it.</summary>
        protected internal virtual Task<ApiResponse> Transport(string method, string url, string json)
        {
            var request = new UnityWebRequest(url, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = _settings != null ? _settings.timeoutSeconds : 10,
            };
            if (json != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            request.SetRequestHeader("Accept", "application/json");
            if (!string.IsNullOrEmpty(AuthToken)) request.SetRequestHeader("Authorization", "Bearer " + AuthToken);
            foreach (KeyValuePair<string, string> header in _headers) request.SetRequestHeader(header.Key, header.Value);

            var done = new TaskCompletionSource<ApiResponse>();
            request.SendWebRequest().completed += _ =>
            {
                bool network = request.result == UnityWebRequest.Result.ConnectionError;
                done.SetResult(new ApiResponse(request.responseCode, request.downloadHandler?.text, request.error, network));
                request.Dispose();
            };
            return done.Task;
        }
    }
}
