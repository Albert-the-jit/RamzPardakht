// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NBitcoin;

namespace RamzPardakht.ApplicationCore.Contracts;

public interface IBitcoinWalletProvider
{
    (WalletVersion walletVersion, PubKey pubKey) GetNewWalletPublicKey(int uniqueId);
    Key GetPrivateKeyById(WalletVersion version, int uniqueId);
    WalletVersion NewWalletsVersion();
    ExtPubKey GetMasterPublicKey();
}

public enum WalletVersion
{
    V1 = 1,
}
