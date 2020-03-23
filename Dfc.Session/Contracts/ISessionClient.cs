using Dfc.Session.Models;
using System.Threading.Tasks;

namespace Dfc.Session
{
    public interface ISessionClient
    {
        DfcUserSession NewSession();

        void CreateCookie(DfcUserSession userSession, bool validateSessionId);

        Task<string> TryFindSessionCode();

        string GeneratePartitionKey(string sessionId);

        bool ValidateUserSession(DfcUserSession dfcUserSession);
    }
}