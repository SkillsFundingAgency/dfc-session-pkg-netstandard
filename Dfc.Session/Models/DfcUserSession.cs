using System;

namespace Dfc.Session.Models
{
    public class DfcUserSession
    {
        public string PartitionKey { get; set; }

        public string SessionId { get; set; }

        public string Salt { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}