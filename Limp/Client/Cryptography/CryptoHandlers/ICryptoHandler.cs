﻿using LimpShared.Models.Message;

namespace Limp.Client.Cryptography.CryptoHandlers;

public interface ICryptoHandler
{
    public Task<Cryptogramm> Encrypt(Cryptogramm cryptogramm, string? contact = null, string? PublicKeyToEncryptWith = null);
    public Task<string> Decrypt(Cryptogramm cryptogramm, string? contact = null);
}
