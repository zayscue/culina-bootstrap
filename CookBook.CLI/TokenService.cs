using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Culina.Bootstrap.CookBook.CLI
{
    public class TokenService
    {
        private Token token = new Token();
        private readonly Credentials credentials;

        public TokenService(Credentials credentials)
        {
            this.credentials = credentials;
        }

        public async Task<(string tokenType, string accessToken)> GetToken()
        {
            if (!this.token.IsValidAndNotExpiring)
            {
                this.token = await this.GetNewAccessToken();
            }
            return (tokenType: this.token.TokenType, accessToken: this.token.AccessToken);
        }

        private async Task<Token> GetNewAccessToken()
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var json = JsonSerializer.Serialize(new
            {
                client_id = credentials.ClientId,
                client_secret = credentials.ClientSecret,
                audience = credentials.Audience,
                grant_type = "client_credentials"
            });
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(credentials.STS, requestContent);
            if (!response.IsSuccessStatusCode) throw new Exception("Unable to retrieve access token from Auth0");
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
            var tokenType = tokenResponse["token_type"].ToString();
            var accessToken = tokenResponse["access_token"].ToString();
            var expiresIn = int.Parse(tokenResponse["expires_in"].ToString());
            return new Token
            {
                AccessToken = accessToken,
                TokenType = tokenType,
                ExpiresIn = expiresIn,
                ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn)
            };
        }
    }
}
