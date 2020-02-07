using Dfc.Session.Models;
using Dfc.Session.Services;
using System;
using Xunit;

namespace Dfc.Session.UnitTests.Services
{
    public class SessionIdGeneratorTests
    {
        private const string DummySalt = "someSalt";
        private readonly ISessionIdGenerator sessionGenerator;
        private readonly DateTime currentDate = DateTime.UtcNow;

        public SessionIdGeneratorTests()
        {
            this.sessionGenerator = new SessionIdGenerator();
        }

        [Fact]
        public void CreateSessionReturnsASessionResult()
        {
            // Act
            var creationResult = sessionGenerator.CreateSession(DummySalt, currentDate);

            //Assert
            Assert.True(!string.IsNullOrWhiteSpace(creationResult.EncodedSessionId) && creationResult.Counter > 0);
        }

        [Fact]
        public void ValidateSessionReturnsTrueWhenValidSession()
        {
            // Arrange
            var creationResult = sessionGenerator.CreateSession(DummySalt, currentDate);
            var session = new DfcUserSession
            {
                Salt = $"{DummySalt}|{creationResult.Counter}",
                CreatedDate = currentDate,
                SessionId = creationResult.EncodedSessionId,
            };

            // Act
            var validateResult = sessionGenerator.ValidateSessionId(session);

            // Assert
            Assert.True(validateResult);
        }

        [Fact]
        public void ValidateSessionReturnsFalseWhenInvalidSession()
        {
            // Arrange
            var creationResult = sessionGenerator.CreateSession(DummySalt, currentDate);
            var session = new DfcUserSession
            {
                Salt = $"{DummySalt}|{creationResult.Counter}",
                CreatedDate = currentDate.AddDays(1),
                SessionId = "SomeInvalidSessionId",
            };

            // Act
            var validateResult = sessionGenerator.ValidateSessionId(session);

            // Assert
            Assert.False(validateResult);
        }
    }
}