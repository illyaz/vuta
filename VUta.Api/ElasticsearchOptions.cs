﻿using System.ComponentModel.DataAnnotations;

namespace VUta.Api;

public class ElasticsearchOptions
{
    public static string Section = "Elasticsearch";

    [Required] public Uri Host { get; set; } = new("http://localhost:19200");

    public string? ApiKey { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}