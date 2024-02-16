//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;

//using Umbraco.Extensions;

//namespace uSync.Core;

//public static class JsonExtensions
//{
//    /// <summary>
//    ///  get a JToken value of a string.
//    /// </summary>
//    /// <remarks>
//    ///  if the string is valid JSON then you will get a parsed
//    ///  version of the json. 
//    ///  
//    ///  if it isn't then you just get the string value (which will cast 
//    ///  automatically to JToken when you need it).
//    /// </remarks>
//    public static JToken GetJsonTokenValue(this string value)
//    {
//        if (!value.DetectIsJson()) return value;
//        try
//        {
//            return JToken.Parse(value);
//        }
//        catch
//        {
//            // error parsing, so it's not actually json
//            // it just might look like it a bit.
//            return value;
//        }
//    }

//    public static JToken ExpandAllJsonInToken(this JToken token)
//        => ExpandAllJsonInToken(token, false);

//    /// <summary>
//    ///  Will recurse through a JToken value, and expand and child values that may contain escaped 
//    ///  JSON (in the GRID all the json is true).
//    /// </summary>
//    public static JToken ExpandAllJsonInToken(this JToken token, bool fullStringExpansion)
//    {
//        if (token == null) return null;

//        switch (token)
//        {
//            case JArray jArray:
//                for (int i = 0; i < jArray.Count; i++)
//                {
//                    jArray[i] = jArray[i].ExpandAllJsonInToken(fullStringExpansion);
//                }
//                break;
//            case JObject jObject:
//                foreach (var property in jObject.Properties())
//                {
//                    jObject[property.Name] = jObject[property.Name].ExpandAllJsonInToken(fullStringExpansion);
//                }
//                break;
//            case JValue jValue:
//                if (jValue.Value is null) return null;
//                return GetJTokenValue(jValue, fullStringExpansion);
//            // return ExpandStringValue(token.ToString());
//            default:
//                return ExpandStringValue(token.ToString());
//        }

//        return token.ToString().GetJsonTokenValue();
//    }

//    private static JToken GetJTokenValue(JValue token, bool fullStringExpansion)
//    {
//        switch (token.Type)
//        {
//            case JTokenType.Boolean:
//            case JTokenType.Integer:
//            case JTokenType.Date:
//            case JTokenType.Bytes:
//            case JTokenType.Float:
//            case JTokenType.Guid:
//            case JTokenType.Null:
//                return token;
//            case JTokenType.String:
//                if (!fullStringExpansion)
//                {
//                    return token;
//                }
//                return ExpandStringValue(token.ToString());
//            default:
//                return ExpandStringValue(token.ToString());
//        }
//    }

//    private static JToken ExpandStringValue(string stringValue)
//    {
//        if (stringValue.DetectIsJson() && !stringValue.IsAngularExpression())
//        {
//            try
//            {
//                return JToken.Parse(stringValue).ExpandAllJsonInToken();
//            }
//            catch
//            {
//                return stringValue;
//            }
//        }

//        return stringValue.GetJsonTokenValue();
//    }

//    public static bool IsValidJsonString(this string value)
//    {
//        if (string.IsNullOrWhiteSpace(value) || !value.DetectIsJson())
//            return false;

//        // umbraco thinks it's json, but is it ? 

//        try
//        {
//            JToken.Parse(value);
//            return true;
//        }
//        catch
//        {
//            return false;
//        }
//    }

//    public static bool TryParseValidJsonString(this string value, out JToken token)
//        => TryParseValidJsonString<JToken>(value, out token);

//    /// <summary>
//    ///  parse a value and return the JSON - only if it's valid JSON 
//    /// </summary>
//    /// <remarks>
//    ///  this value will return false for strings that don't look like json strings (e.g "hello" is false) 
//    /// </remarks>
//    public static bool TryParseValidJsonString<TResult>(this string value, out TResult result)
//        where TResult : JToken
//    {
//        result = default;
//        if (string.IsNullOrWhiteSpace(value) || !value.DetectIsJson())
//            return false;

//        try
//        {
//            result = JsonConvert.DeserializeObject<TResult>(value);
//            return true;
//        }
//        catch
//        {
//            return false;
//        }
//    }



//    public static JToken GetJTokenFromObject(this object value)
//    {
//        var stringValue = value.GetValueAs<string>();
//        if (string.IsNullOrWhiteSpace(stringValue) || !stringValue.DetectIsJson())
//            return stringValue;

//        try
//        {
//            var jsonToken = JsonConvert.DeserializeObject<JToken>(stringValue);
//            return jsonToken;
//        }
//        catch
//        {
//            return default;
//        }
//    }

//    public static TObject GetValueAs<TObject>(this object value)
//    {
//        if (value == null) return default;
//        var attempt = value.TryConvertTo<TObject>();
//        if (!attempt) return default;
//        return attempt.Result;
//    }

//    public static bool IsAngularExpression(this string value)
//        => value.StartsWith("{{") && value.EndsWith("}}");
//}