using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpireChess.Utils
{
    public static class CanonicalJson
    {
        public static string Normalize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON document is required.", nameof(json));
            }

            return Normalize(JToken.Parse(json));
        }

        public static string Normalize(JToken token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return Canonicalize(token).ToString(Formatting.None);
        }

        public static string ComputeSha256(params string[] jsonDocuments)
        {
            if (jsonDocuments == null || jsonDocuments.Length == 0)
            {
                throw new ArgumentException(
                    "At least one JSON document is required.",
                    nameof(jsonDocuments));
            }

            var canonical = string.Join("\n", jsonDocuments.Select(Normalize));
            return ComputeTextSha256(canonical);
        }

        public static string ComputeTokenSha256(JToken token)
        {
            return ComputeTextSha256(Normalize(token));
        }

        public static string ComputeTextSha256(string text)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(
                        sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty)))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private static JToken Canonicalize(JToken token)
        {
            var obj = token as JObject;
            if (obj != null)
            {
                var sorted = new JObject();
                foreach (var property in obj.Properties()
                             .OrderBy(value => value.Name, StringComparer.Ordinal))
                {
                    sorted.Add(property.Name, Canonicalize(property.Value));
                }

                return sorted;
            }

            var array = token as JArray;
            if (array != null)
            {
                return new JArray(array.Select(Canonicalize));
            }

            return token.DeepClone();
        }
    }
}
