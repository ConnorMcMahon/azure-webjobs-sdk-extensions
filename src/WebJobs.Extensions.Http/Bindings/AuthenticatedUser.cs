using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs
{
    public class AuthenticatedUser
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; }

        [JsonProperty("expires_on")]
        public DateTime ExpiresOn { get; }

        [JsonProperty("id_token")]
        public string IdToken { get; }

        [JsonProperty("provider_name")]
        public string ProviderName { get; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; }

        [JsonProperty("user_claims")]
        public Claim[] UserClaims { get;  } 

        internal static AuthenticatedUser DeserializeJson(string json)
        {
            return JsonConvert.DeserializeObject<AuthenticatedUser>(json, new JsonConverter[] { new ClaimConverter() });
        }

        private class ClaimConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Claim);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JObject jObject = JObject.Load(reader);
                return new Claim(jObject["typ"].Value<string>(), jObject["val"].Value<string>());
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
