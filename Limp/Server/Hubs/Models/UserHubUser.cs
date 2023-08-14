namespace Limp.Server.Hubs.Models
{
    public record UserHubUser
    {
        public string Username { get; set; } = "Unknown user";
        public List<string> ConnectionIds { get; set; } = new();
    }
}
