using Dfc.Session.Models;
using System.Threading.Tasks;

namespace Dfc.Session
{
    public interface ISessionClient
    {
        DfcUserSession NewSession();

        void CreateCookie(DfcUserSession userSession);

        Task<string> TryFindSessionCode();
    }
}