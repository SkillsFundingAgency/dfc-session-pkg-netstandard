namespace Dfc.Session.Services
{
    public interface IPartitionKeyGenerator
    {
        string GeneratePartitionKey(string applicationName, string sessionId);
    }
}