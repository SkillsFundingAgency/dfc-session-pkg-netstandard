using Dfc.Session.Exceptions;
using Dfc.Session.Models;
using HashidsNet;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Dfc.Session.UnitTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Dfc.Session.Services
{
    internal class SessionIdGenerator : ISessionIdGenerator
    {
        private const string Alphabet = "acefghjkmnrstwxyz23456789";
        private static readonly object SyncLock = new object();
        private static int counter = 10;

        public SessionResult CreateSession(string salt, DateTime date)
        {
            var hashids = new Hashids(salt, 4, Alphabet);
            var currentCounter = Counter();
            var sessionId = GenerateSessionId(date, currentCounter);
            var encodedSessionId = hashids.EncodeLong(sessionId);

            var isValid = Validate(salt, encodedSessionId, sessionId.ToString());
            if (!isValid)
            {
                throw new InvalidSessionException("Invalid decode");
            }

            return new SessionResult
            {
                Counter = currentCounter,
                EncodedSessionId = encodedSessionId,
            };
        }

        public bool ValidateSessionId(DfcUserSession userSession)
        {
            var splitSalt = userSession.Salt.Split('|');
            var hashids = new Hashids(splitSalt[0], 4, Alphabet);
            if (!int.TryParse(splitSalt[1], out var currentCounter))
            {
                return false;
            }

            var sessionId = GenerateSessionId(userSession.CreatedDate, currentCounter);
            var encodedSessionId = hashids.EncodeLong(sessionId);

            return encodedSessionId == userSession.SessionId;
        }

        private static long GenerateSessionId(DateTime date, int currentCounter)
        {
            var yearFrom2018 = (date.Year - 2018).ToString();
            return Convert.ToInt64($"{yearFrom2018}{date:MMddHHmmssfff}{currentCounter}");
        }

        private static bool Validate(string salt, string encodedSessionId, string unencodedSessionId)
        {
            var decodedDigits = Decode(salt, encodedSessionId);
            return unencodedSessionId == decodedDigits;
        }

        private static string Decode(string salt, string code)
        {
            var hashids = new Hashids(salt, 4, Alphabet);
            var decode = hashids.DecodeLong(code);
            return decode.Length > 0 ? decode[0].ToString() : null;
        }

        private static int Counter()
        {
            lock (SyncLock)
            {
                if (counter >= 99)
                {
                    counter = 0;
                }

                return counter++;
            }
        }
    }
}