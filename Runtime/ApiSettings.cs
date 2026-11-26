// ArchNet · API Service — settings
// The base URL of each environment (Development, Production…), which one the Editor and
// development builds use and which one release builds use, plus timeout and retries.
// Never put a secret here: it would ship in the game. Set ApiService.AuthToken at runtime.
// Docs: https://archnet-plateform.fr/#ServiceApi

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArchNet.Service
{
    [CreateAssetMenu(fileName = "ApiSettings", menuName = "ArchNet/API Settings")]
    public class ApiSettings : ScriptableObject
    {
        [Serializable]
        public class Environment
        {
            public string name;
            [Tooltip("e.g. https://api.mygame.com/v1")]
            public string baseUrl;
        }

        public List<Environment> environments = new List<Environment>
        {
            new Environment { name = "Development", baseUrl = "http://localhost:3000" },
            new Environment { name = "Production", baseUrl = "https://api.example.com" },
        };

        [Tooltip("Environment used in the Editor and in development builds (index in the list).")]
        [Min(0)] public int developmentEnvironment;
        [Tooltip("Environment used in release builds (index in the list).")]
        [Min(0)] public int releaseEnvironment = 1;
        [Min(1)] public int timeoutSeconds = 10;
        [Tooltip("New tries after a network error or a server error (5xx, 429).")]
        [Range(0, 5)] public int retries = 2;
        [Tooltip("Wait before the first new try; doubled each time.")]
        [Min(0f)] public float retryDelaySeconds = 0.5f;

        public Environment Active
        {
            get
            {
                if (environments == null || environments.Count == 0) throw new InvalidOperationException("ApiSettings has no environment: add one with its base URL.");
                int index = Application.isEditor || Debug.isDebugBuild ? developmentEnvironment : releaseEnvironment;
                return environments[Mathf.Clamp(index, 0, environments.Count - 1)];
            }
        }
    }
}
