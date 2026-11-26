using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace ArchNet.Service.Tests
{
    [Serializable]
    public class Status
    {
        public string version;
        public bool online;
    }

    // Answers from a list instead of the network; remembers what was asked.
    public class FakeApi : ApiService
    {
        public readonly Queue<ApiResponse> Answers = new Queue<ApiResponse>();
        public readonly List<string> Calls = new List<string>();

        protected internal override Task<ApiResponse> Transport(string method, string url, string json)
        {
            Calls.Add($"{method} {url} {json}");
            return Task.FromResult(Answers.Dequeue());
        }
    }

    public class ApiServiceTests
    {
        GameObject _object;
        FakeApi _api;
        ApiSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<ApiSettings>();
            _settings.environments[0].baseUrl = "https://dev.example.com/v1/";
            _settings.developmentEnvironment = 0;
            _settings.retries = 2;
            _settings.retryDelaySeconds = 0f; // no waiting in tests
            _object = new GameObject("Api");
            _api = _object.AddComponent<FakeApi>();
            _api.Configure(_settings);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_object);
            UnityEngine.Object.DestroyImmediate(_settings);
        }

        [Test]
        public void BuildsUrlsFromTheActiveEnvironment()
        {
            Assert.AreEqual("https://dev.example.com/v1/status", _api.UrlFor("/status"));
            Assert.AreEqual("https://other.example.com/x", _api.UrlFor("https://other.example.com/x"));
        }

        [Test]
        public void ReadsTheJsonAnswer()
        {
            _api.Answers.Enqueue(new ApiResponse(200, "{\"version\":\"1.2\",\"online\":true}", null, false));

            Status status = _api.Get<Status>("status").Result;

            Assert.AreEqual("1.2", status.version);
            Assert.IsTrue(status.online);
            Assert.AreEqual("GET https://dev.example.com/v1/status ", _api.Calls[0]);
        }

        [Test]
        public void TriesAgainAfterAServerError()
        {
            _api.Answers.Enqueue(new ApiResponse(0, null, "Cannot connect", true));
            _api.Answers.Enqueue(new ApiResponse(503, "busy", null, false));
            _api.Answers.Enqueue(new ApiResponse(200, "{\"online\":true}", null, false));

            Assert.IsTrue(_api.Get<Status>("status").Result.online);
            Assert.AreEqual(3, _api.Calls.Count);
        }

        [Test]
        public void AClientErrorThrowsAtOnceWithItsBody()
        {
            _api.Answers.Enqueue(new ApiResponse(404, "{\"error\":\"not found\"}", null, false));

            AggregateException failure = Assert.Throws<AggregateException>(() => _api.Get<Status>("missing").Wait());
            var error = (ApiException)failure.InnerException;
            Assert.AreEqual(404, error.StatusCode);
            StringAssert.Contains("not found", error.Body);
            Assert.AreEqual(1, _api.Calls.Count, "4xx errors are not tried again");
        }

        [Test]
        public void SendsTheBodyAsJson()
        {
            _api.Answers.Enqueue(new ApiResponse(201, "", null, false));
            _api.Post<Status>("scores", new Status { version = "x", online = false }).Wait();
            StringAssert.Contains("{\"version\":\"x\",\"online\":false}", _api.Calls[0]);
        }
    }
}
