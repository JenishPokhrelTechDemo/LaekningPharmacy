using System.Text.Json;

namespace Laekning.Infrastructure {

    // Extension methods for storing and retrieving objects in session using JSON
    public static class SessionExtensions {

        // Save an object in session by serializing it to a JSON string
        public static void SetJson(this ISession session, 
                string key, object value) {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        // Retrieve an object from session by deserializing it from JSON
        public static T? GetJson<T>(this ISession session, string key) {
            // Get the JSON string from session
            var sessionData = session.GetString(key);

            // If session data is null, return default(T); otherwise, deserialize
            return sessionData == null
                ? default(T) : JsonSerializer.Deserialize<T>(sessionData);
        }
    }
}
