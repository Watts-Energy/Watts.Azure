namespace Watts.Azure.Common.General.Serialization
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Utility for serializing and deserializing Json.
    /// </summary>
    public class Json
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public static string ToJson(object obj)
        {
            string retVal = JsonConvert.SerializeObject(obj, Formatting.Indented, Settings);

            return retVal;
        }

        public static T FromJson<T>(string serializedObject)
        {
            T retVal = JsonConvert.DeserializeObject<T>(serializedObject, Settings);

            return retVal;
        }

        public static JObject FromJson(string serializedObject)
        {
            return (JObject)JsonConvert.DeserializeObject(serializedObject, Settings);
        }

        public static object FromJson(string serializedObject, Type objectType)
        {
            object retVal = JsonConvert.DeserializeObject(serializedObject, objectType, Settings);

            return retVal;
        }
    }
}