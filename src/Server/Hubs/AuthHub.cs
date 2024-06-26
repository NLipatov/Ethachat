﻿using Ethachat.Server.Utilities.HttpMessaging;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Models.UserAuthentication;
using Microsoft.AspNetCore.SignalR;

namespace Ethachat.Server.Hubs
{
    public class AuthHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        public AuthHub(IServerHttpClient serverHttpClient)
        {
            _serverHttpClient = serverHttpClient;
        }

        public async Task Register(UserAuthentication userDto)
        {
            AuthResult result = await _serverHttpClient.Register(userDto);

            await Clients.Caller.SendAsync("OnRegister", result);
        }
        public async Task LogIn(UserAuthentication userDto)
        {
            var result = await _serverHttpClient.GetJWTPairAsync(userDto);

            await Clients.Caller.SendAsync("OnLoggingIn", result);
        }

        public async Task GetTokenRefreshHistory(string accessToken)
        {
            var history = await _serverHttpClient.GetTokenRefreshHistory(accessToken);

            await Clients.Caller.SendAsync("OnRefreshTokenHistoryResponse", history);
        }

        public async Task ValidateCredentials(CredentialsDTO dto)
        {
            AuthResult result = await _serverHttpClient.ValidateCredentials(dto);

            await Clients.Caller.SendAsync("OnValidateCredentials", result);
        }

        public async Task RefreshCredentials(CredentialsDTO dto)
        {
            AuthResult result = await _serverHttpClient.RefreshCredentials(dto);
            
            await Clients.Caller.SendAsync("OnRefreshCredentials", result);
        }
    }
}
