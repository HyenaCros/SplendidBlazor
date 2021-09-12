using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace SplendidBlazor
{
    public class SplendidAuthStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;

        public SplendidAuthStateProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        private AuthenticationState _authState = new AuthenticationState(new ClaimsPrincipal());
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return _authState;
        }

        public async Task Authorize()
        {
            var response = await _httpClient.PostAsync("Authorization", default);
            var token = await response.Content.ReadAsStringAsync();
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var claims = new List<Claim>();
            var payload = token.Split('.')[1];
            
            var jsonBytes = ParseBase64WithoutPadding(payload);
            
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
            claims.AddRange(keyValuePairs.Select(p => new Claim(p.Key, p.Value.ToString())));

            _authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims)));
            
            NotifyAuthenticationStateChanged(Task.FromResult(_authState));
        }
        
        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}