namespace Laekning.Infrastructure {

    // Extension methods for HttpRequest
    public static class UrlExtensions {

        // Returns the full path and query string of the current request
        public static string PathAndQuery(this HttpRequest request) =>
            // If the request has a query string, append it to the path
            request.QueryString.HasValue
                ? $"{request.Path}{request.QueryString}"
                // Otherwise, return just the path
                : request.Path.ToString();
    }
}
