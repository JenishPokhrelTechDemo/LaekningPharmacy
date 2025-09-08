using Microsoft.AspNetCore.Http;
using System.Text.Json;

public static class SessionExtensions
{
	//Save an object into session as a JSON string
    public static void SetObjectAsJson(this ISession session, string key, object value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

	// Retrieve an object from session by reading and deserializing JSON
    public static T GetObjectFromJson<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
		
		//If nothing is found, return default(T); otherwise convert JSON back to object
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }
}
