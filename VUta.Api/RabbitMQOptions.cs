using System.ComponentModel.DataAnnotations;

namespace VUta.Api;

public class RabbitMQOptions
{
    public static string Section = "RabbitMQ";

    [Required] public string Host { get; set; } = "localhost";

    [Required] [Range(1, 65535)] public int Port { get; set; } = 5672;

    [Required] public string VirtualHost { get; set; } = "/";

    public string? Username { get; set; }
    public string? Password { get; set; }
}