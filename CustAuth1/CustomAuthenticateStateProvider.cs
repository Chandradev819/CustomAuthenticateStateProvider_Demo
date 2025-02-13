﻿using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;

namespace CustAuth1
{
    public class CustomAuthenticateStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticateStateProvider(ProtectedSessionStorage sessionStorage, IHttpContextAccessor httpContextAccessor)
        {
            _sessionStorage = sessionStorage;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_currentUser.Identity?.IsAuthenticated == true)
            {
                return new AuthenticationState(_currentUser);
            }

            var isPrerendering = _httpContextAccessor.HttpContext?.Response.HasStarted == false;

            if (!isPrerendering)
            {
                await LoadUserFromSessionAsync();
            }

            return new AuthenticationState(_currentUser);
        }

        public async Task<bool> LoginAsync(string userId, string password)
        {
            if (userId == "admin" && password == "password")
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, userId),
                    new Claim(ClaimTypes.Role, "Admin")
                };
                _currentUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "BasicAuth"));
                await _sessionStorage.SetAsync("UserRole", "Admin");
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
                return true;
            }
            return false;
        }

        public async Task LogoutAsync()
        {
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            await _sessionStorage.DeleteAsync("UserRole");
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        private async Task LoadUserFromSessionAsync()
        {
            try
            {
                var sessionResult = await _sessionStorage.GetAsync<string>("UserRole");
                if (sessionResult.Success && sessionResult.Value == "Admin")
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, "admin"),
                        new Claim(ClaimTypes.Role, "Admin")
                    };
                    _currentUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "BasicAuth"));
                }
            }
            catch
            {
                // Ignore exceptions during prerendering
            }
        }
    }
}