namespace VUta.Api
{
    using System.ComponentModel.DataAnnotations;

    public class ApiOptions
    {
        public static string Section = "Api";

        [Required]
        public string Secret { get; set; } = null!;
    }
}
