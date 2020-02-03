using Dfc.Session.Services;
using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Dfc.Session.UnitTests.Services
{
    public class PartitionKeyGeneratorTests
    {
        [Fact]
        public void GeneratePartitionKeyReturnsApplicationNameAndSuffix()
        {
            // Arrange
            const string sessionId = "dummySessionId";
            const string applicationName = "applicationName";

            var md5 = MD5.Create();
            var hashedBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(sessionId));
            var hashedInt = BitConverter.ToInt32(hashedBytes, 0);
            var expectedResult = $"{applicationName}{Math.Abs(hashedInt) % 20}";

            var generator = new PartitionKeyGenerator();

            // Act
            var result = generator.GeneratePartitionKey(applicationName, sessionId);

            // Assert
            Assert.Equal(expectedResult, result);
            md5.Dispose();
        }
    }
}