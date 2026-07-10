namespace LichessNET.Internal;

internal sealed class LichessRequest
{
    public LichessRequest(string uri)
    {
        Uri = uri;
    }

    public string Uri { get; }
    public string Body { get; set; }
    public Dictionary<string, string> Query { get; } = new();
    public Dictionary<string, string> Headers { get; } = new();

    public void AddFormData(Dictionary<string, string> values)
    {
        if (values == null)
            return;

        foreach (var pair in values)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null)
                Query[pair.Key] = pair.Value;
        }
    }

    public string BuildUri()
    {
        if (Query.Count == 0)
            return Uri;

        var separator = Uri.Contains('?') ? "&" : "?";
        var query = string.Join("&", Query.Select(x => $"{System.Uri.EscapeDataString(x.Key)}={System.Uri.EscapeDataString(x.Value)}"));
        return Uri + separator + query;
    }
}

internal sealed class LichessResponse
{
    public LichessResponse(string content)
    {
        Content = new LichessContent(content ?? string.Empty);
    }

    public LichessContent Content { get; }
}

internal sealed class LichessContent
{
    private readonly string _content;

    public LichessContent(string content)
    {
        _content = content ?? string.Empty;
    }

    public Task<string> ReadAsStringAsync()
    {
        return Task.FromResult(_content);
    }

    public Task<T> ReadFromJsonAsync<T>(JsonSerializerOptions options = null)
    {
        return Task.FromResult(JsonSerializer.Deserialize<T>(_content, options ?? LichessJson.Options));
    }
}

