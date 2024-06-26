﻿using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;

namespace Client.Infrastructure.Cryptography
{
    public class CryptographyService : ICryptographyService
    {
        private readonly IRuntimeCryptographyExecutor _cryptographyExecutor;
        private readonly IKeyStorage _keyStorage;

        public CryptographyService(IPlatformRuntime platformRuntime, IKeyStorage keyStorage)
        {
            _cryptographyExecutor = new RuntimeCryptographyExecutor(platformRuntime);
            _keyStorage = keyStorage;
            _ = GenerateRsaKeyPairAsync();
        }

        private async Task GenerateRsaKeyPairAsync()
        {
            var keyPair = await _cryptographyExecutor.InvokeAsync<string[]>("GenerateRSAOAEPKeyPairAsync", []);
            var publicRsa = new Key
            {
                Value = keyPair[0],
                Format = KeyFormat.PemSpki,
                Type = KeyType.RsaPublic,
                Contact = string.Empty
            };
            var privateRsa = new Key
            {
                Value = keyPair[1],
                Format = KeyFormat.PemSpki,
                Type = KeyType.RsaPrivate,
                Contact = string.Empty
            };
            await _keyStorage.StoreAsync(privateRsa);
            await _keyStorage.StoreAsync(publicRsa);
        }

        public async Task<Key> GenerateAesKeyAsync(string contact)
        {
            var key = await _cryptographyExecutor.InvokeAsync<string>("GenerateAESKeyAsync", []);
            return new Key
            {
                Value = key,
                Format = KeyFormat.Raw,
                Type = KeyType.Aes,
                Contact = contact,
                CreationDate = DateTime.UtcNow,
                IsAccepted = false
            };
        }

        public async Task<Cryptogram> DecryptAsync<T>(Cryptogram cryptogram, Key key)
            where T : ICryptoHandler
        {
            ICryptoHandler? cryptoHandler = (T?)Activator.CreateInstance(typeof(T), _cryptographyExecutor);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(T)} instance.");

            return await cryptoHandler.Decrypt(cryptogram, key);
        }

        public async Task<Cryptogram> EncryptAsync<T>(Cryptogram cryptogram, Key key) where T : ICryptoHandler
        {
            ICryptoHandler? cryptoHandler = (T?)Activator.CreateInstance(typeof(T), _cryptographyExecutor);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(T)} instance.");

            return await cryptoHandler.Encrypt(cryptogram, key);
        }

        public async Task<BinaryCryptogram> DecryptAsync<T>(BinaryCryptogram cryptogram, Key key)
            where T : ICryptoHandler
        {
            ICryptoHandler? cryptoHandler = (T?)Activator.CreateInstance(typeof(T), _cryptographyExecutor);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(T)} instance.");

            return await cryptoHandler.Decrypt(cryptogram, key);
        }

        public async Task<BinaryCryptogram> EncryptAsync<TCryptoHandler, TData>(TData data, Key key) where TCryptoHandler : ICryptoHandler
        {
            ICryptoHandler? cryptoHandler = (TCryptoHandler?)Activator.CreateInstance(typeof(TCryptoHandler), _cryptographyExecutor);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(TCryptoHandler)} instance.");

            return await cryptoHandler.Encrypt(data, key);
        }
    }
}