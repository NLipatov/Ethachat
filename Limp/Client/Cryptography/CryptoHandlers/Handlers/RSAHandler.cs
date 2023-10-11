﻿using Limp.Client.Cryptography.KeyStorage;
using LimpShared.Models.Message;
using Microsoft.JSInterop;

namespace Limp.Client.Cryptography.CryptoHandlers.Handlers
{
    public class RSAHandler : ICryptoHandler
    {
        private readonly IJSRuntime _jSRuntime;

        public RSAHandler(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }
        public async Task<Cryptogramm> Encrypt(Cryptogramm cryptogramm, string? contact = null, string? PublicKeyToEncryptWith = null)
        {
            if (string.IsNullOrWhiteSpace(PublicKeyToEncryptWith))
                throw new ArgumentException("Please provide an RSA Key to Encrypt your text with.");

            string encryptedMessage = await _jSRuntime
                .InvokeAsync<string>("EncryptWithRSAPublicKey", cryptogramm.Cyphertext, PublicKeyToEncryptWith);

            if (string.IsNullOrWhiteSpace(encryptedMessage))
                throw new ApplicationException("Could not encrypt text.");

            return new Cryptogramm
            {
                Cyphertext = encryptedMessage,
                Iv = await _jSRuntime.InvokeAsync<string>("ExportIV"),
            };
        }

        public async Task<Cryptogramm> Decrypt(Cryptogramm cryptogramm, string? contact = null)
        {
            if (InMemoryKeyStorage.MyRSAPrivate?.Value == null)
                throw new ApplicationException("RSA Private key was null");

            string decryptedMessage = await _jSRuntime
                .InvokeAsync<string>("DecryptWithRSAPrivateKey", cryptogramm.Cyphertext, InMemoryKeyStorage.MyRSAPrivate.Value);

            return new Cryptogramm()
            {
                Cyphertext = decryptedMessage,
                Iv = await _jSRuntime.InvokeAsync<string>("ExportIV"),
            };
        }
    }
}
