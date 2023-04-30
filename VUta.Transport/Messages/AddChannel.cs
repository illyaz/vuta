namespace VUta.Transport.Messages
{
    public record AddChannel(string Id);
    public record AddChannelResult(
        bool Exists, AddChannelResultInfo? Info = null, string? Error = null);
    public record AddChannelResultInfo(
        string Title,
        string Thumbnail);
}
