namespace VUta.Transport.Messages;

public record UpdateVideo(
    string Id,
    bool ScanComment = false);