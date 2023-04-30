namespace VUta.ESIndexer
{
    using System.ComponentModel.DataAnnotations;

    public class ESIndexerOptions
    {
        public static string Section = "ESIndexer";

        [Required]
        public string ConnectionString { get; set; } = null!;

        [Required]
        public string Replication { get; set; } = null!;

        [Required]
        public string Publication { get; set; } = null!;

        [Required]
        public string Elasticsearch { get; set; } = "http://localhost:9200";

        public string? ElasticsearchAuthorization { get; set; }
    }
}
