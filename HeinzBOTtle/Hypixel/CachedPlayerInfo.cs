namespace HeinzBOTtle.Hypixel;

/// <summary>
/// Represents a cached Hypixel API player request response, which may be overwritten 10 minutes after being initially retrieved.
/// </summary>
public class CachedPlayerInfo {

    /// <summary>The timestamp of the most recent instance of the command's execution.</summary>
    public long Timestamp { get; set; }
    /// <summary>The cached JSON response.</summary>
    public Json JsonResponse { get; set; }

    public CachedPlayerInfo(long timestamp, Json jsonResponse) {
        Timestamp = timestamp;
        JsonResponse = jsonResponse;
    }

}
