namespace Airport.Services.Abstractions
{
    public interface IAuthService
    {
        string? Login(string username, string password);
    }
}
