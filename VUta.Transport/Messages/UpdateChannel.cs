namespace VUta.Transport.Messages;

public record UpdateChannel(
    string Id,
    bool ScanVideo = false);