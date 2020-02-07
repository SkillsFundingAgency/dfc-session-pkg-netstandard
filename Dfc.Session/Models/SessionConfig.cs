namespace Dfc.Session.Models
{
    public class SessionConfig
    {
        public string ApplicationName { get; set; }

        public string Salt { get; set; } = "ncs"
    }
}
