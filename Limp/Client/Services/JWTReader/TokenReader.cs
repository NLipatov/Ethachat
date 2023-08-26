﻿using LimpShared.Encryption;
using System.IdentityModel.Tokens.Jwt;

namespace Limp.Client.Services.JWTReader
{
    public static class TokenReader
    {
        public static bool IsTokenReadable(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.CanReadToken(accessToken);
        }
        public static bool HasAccessTokenExpired(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var securityToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;

            if (securityToken?.ValidTo == null)
                throw new ArgumentException("Access token is not valid");

            var now = DateTime.UtcNow;

            return securityToken.ValidTo <= now;
        }

        public static string GetUsernameFromAccessToken(string? accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ApplicationException($"Passed in parameter {nameof(accessToken)} was empty string or null.");

            string? usernameFromAccessToken = ReadUsernameFromAccessToken(accessToken);
            if (string.IsNullOrWhiteSpace(usernameFromAccessToken))
                throw new ApplicationException("Could read username from access-token");

            return usernameFromAccessToken;
        }

        private static string? ReadUsernameFromAccessToken(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;

            return securityToken?.Claims.FirstOrDefault(claim => claim.Type == "unique_name")?.Value;
        }

        public static Key GetPublicKey(string accessToken, string contactName)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;

            return new Key
            {
                Format = KeyFormat.PemSpki,
                Type = KeyType.RsaPublic,
                Value = securityToken?.Claims?.FirstOrDefault(claim => claim.Type == "RSA Public Key")?.Value,
                Contact = contactName,
            };
        }
    }
}
