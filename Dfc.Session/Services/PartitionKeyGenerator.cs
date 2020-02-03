using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

[assembly: InternalsVisibleTo("Dfc.Session.UnitTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Dfc.Session.Services
{
    internal class PartitionKeyGenerator : IPartitionKeyGenerator
    {
        private const int NumberOfPartitions = 20;
        private readonly MD5 md5 = MD5.Create();

        public string GeneratePartitionKey(string applicationName, string sessionId)
        {
            var suffix = GenerateSuffix(sessionId);
            return $"{applicationName}{suffix}";
        }

        private int GenerateSuffix(string sessionId)
        {
            var hashedBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(sessionId));
            var hashedInt = BitConverter.ToInt32(hashedBytes, 0);
            hashedInt = hashedInt == int.MinValue ? hashedInt + 1 : hashedInt;
            return Math.Abs(hashedInt) % NumberOfPartitions;
        }
    }
}