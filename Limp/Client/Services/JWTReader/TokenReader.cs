﻿using System.IdentityModel.Tokens.Jwt;

namespace Ethachat.Client.Services.JWTReader
{
    public static class TokenReader
    {
        public static bool IsTokenReadable(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ReadJwtToken(accessToken);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
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
    }
}
