using System.ComponentModel.DataAnnotations;

namespace VUta.Worker;

public class WorkerOptions
{
    public static string Section = "Worker";

    [Required] public string Host { get; set; } = "localhost";

    [Required] [Range(1, 65535)] public int Port { get; set; } = 5672;

    [Required] public string VirtualHost { get; set; } = "/";

    public string? Username { get; set; }
    public string? Password { get; set; }

    [Required] [Range(1, 65535)] public int Prefetch { get; set; } = 128;

    [Required] public ProducerEntry Producer { get; set; } = new();

    [Required] public bool ElasticsearchSync { get; set; } = true;

    public class ProducerEntry
    {
        [Required] public bool VideoNextUpdate { get; set; } = true;

        [Required] public bool ChannelNextUpdate { get; set; } = true;

        [Required] public bool VideoNextUpdateStuck { get; set; } = true;

        [Required] public bool ChannelNextUpdateStuck { get; set; } = true;
    }
}