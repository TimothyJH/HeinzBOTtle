namespace HeinzBOTtle.Hypixel;

/// <summary>
/// Represents a cached Hypixel API request response, which may be overwritten 10 minutes after being initially retrieved.
/// </summary>
public class CachedInfo {

    /// <summary>The timestamp of the most recent request for this data.</summary>
    public long Timestamp { get; set; }
    /// <summary>The cached JSON response.</summary>
    public Json JsonResponse { get; set; }

    public CachedInfo(long timestamp, Json jsonResponse) {
        Timestamp = timestamp;
        JsonResponse = jsonResponse;
    }

}
