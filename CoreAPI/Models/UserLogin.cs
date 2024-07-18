namespace Core.Models;

public partial class UserLogin
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime? AccessTokenExp { get; set; }
    public DateTime? RefreshTokenExp { get; set; }
    public bool Active { get; set; }
    public string InsertedBy { get; set; }
    public DateTime? InsertedDate { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string CreatedBy { get; set; }
    public string IpAddress { get; set; }
}
