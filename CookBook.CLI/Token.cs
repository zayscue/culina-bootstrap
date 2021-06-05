using System;
namespace Culina.Bootstrap.CookBook.CLI
{
    public class Token
    {
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Scope { get; set; }
        public string TokenType { get; set; }

        public bool IsValidAndNotExpiring
        {
            get
            {
                return !string.IsNullOrEmpty(this.AccessToken) && this.ExpiresAt > DateTime.UtcNow.AddSeconds(30);
            }
        }
    }
}
