namespace VUta.Transport.Messages;

public record ScanChannelVideo(
    string Id,
    bool FullScan = false,
    string? Continuation = null);