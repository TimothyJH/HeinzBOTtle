namespace HeinzBOTtle.Hypixel;

public class CachedPlayerInfo {

    public long Timestamp { get; set; }
    public Json JsonResponse { get; set; }

    public CachedPlayerInfo(long timestamp, Json jsonResponse) {
        Timestamp = timestamp;
        JsonResponse = jsonResponse;
    }

}
