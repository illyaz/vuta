using System.ComponentModel.DataAnnotations;

namespace VUta.Api;

public class ApiOptions
{
    public static string Section = "Api";

    [Required] public string Secret { get; set; } = null!;
}