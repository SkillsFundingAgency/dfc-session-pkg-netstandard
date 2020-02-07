using Dfc.Session.Models;
using System;

namespace Dfc.Session.Services
{
    public interface ISessionIdGenerator
    {
        SessionResult CreateSession(string salt, DateTime date);

        bool ValidateSessionId(DfcUserSession userSession);
    }
}