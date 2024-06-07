using System.Text.Json.Serialization;

namespace FluentValidation.Auto;

public sealed record ErrorResult([property: JsonPropertyName("traceId")]
                                 string TraceId,
                                 [property: JsonPropertyName("title")]  string                        Title,
                                 [property: JsonPropertyName("status")] int                           Status = 400,
                                 [property: JsonPropertyName("type")]   string                        Type   = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                                 [property: JsonPropertyName("errors")] Dictionary<string, string[]>? Errors = null);