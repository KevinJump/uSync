
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Extensions;

namespace uSync.Core
{
    public static class JsonExtensions
    {
        /// <summary>
        ///  get a JToken value of a string.
        /// </summary>
        /// <remarks>
        ///  if the string is valid JSON then you will get a parsed
        ///  version of the json. 
        ///  
        ///  if it isn't then you just get the string value (which will cast 
        ///  automatically to JToken when you need it).
        /// </remarks>
        public static JToken GetJsonTokenValue(this string value)
        {
            if (value.DetectIsJson())
            {
                try
                {
                    return JToken.Parse(value);
                }
                catch
                {
                    // error parsing, so it's not actually json
                    // it just might look like it a bit.
                    return value;
                }
            }

            return value;
        }

        /// <summary>
        ///  Will recurse through a JToken value, and expand and child values that may contain escaped 
        ///  JSON (in the GRID all the json is true).
        /// </summary>
        public static JToken ExpandAllJsonInToken(this JToken token)
        {
            if (token == null) return null;

            switch (token)
            {
                case JArray jArray:
                    for(int i = 0; i < jArray.Count; i++)
                    {
                        jArray[i] = jArray[i].ExpandAllJsonInToken();
                    }
                    break;
                case JObject jObject:
                    foreach(var property in jObject.Properties())
                    {
                        jObject[property.Name] = jObject[property.Name].ExpandAllJsonInToken();
                    }
                    break;
                case JValue jValue:
                    if (jValue.Value is null) return null;
                    return ExpandStringValue(token.ToString());
                default:
                    return ExpandStringValue(token.ToString());
            }

            return token.ToString().GetJsonTokenValue();
        }

        private static JToken ExpandStringValue(string stringValue)
        {
            if (stringValue.DetectIsJson())
            {
                try
                {
                    return JToken.Parse(stringValue).ExpandAllJsonInToken();
                }
                catch
                {
                    return stringValue;
                }
            }

            return stringValue.GetJsonTokenValue();
        }


        public static JToken GetJTokenFromObject(this object value)
        {
            var stringValue = value.GetValueAs<string>();
            if (string.IsNullOrWhiteSpace(stringValue) || !stringValue.DetectIsJson()) return null;

            try
            {
                var jsonToken = JsonConvert.DeserializeObject<JToken>(stringValue);
                return jsonToken;
            }
            catch
            {
                return default;
            }
        }

        private static TObject GetValueAs<TObject>(this object value)
        {
            if (value == null) return default;
            var attempt = value.TryConvertTo<TObject>();
            if (!attempt) return default;
            return attempt.Result;
        }
    }
}
