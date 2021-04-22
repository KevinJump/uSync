
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
    }
}
