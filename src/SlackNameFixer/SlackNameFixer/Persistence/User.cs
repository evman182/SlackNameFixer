using System.ComponentModel.DataAnnotations;

namespace SlackNameFixer.Persistence;

public class User
{
    public Guid Id { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string TeamId { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string UserId { get; set; }

    public string PreferredFullName { get; set; }
    
    [Required(AllowEmptyStrings = false)]
    public string AccessToken { get; set; }
}