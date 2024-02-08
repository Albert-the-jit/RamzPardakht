// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using NBitcoin;
using RamzPardakht.ApplicationCore.Contracts;

namespace RamzPardakht.ApplicationCore.Services;

public class BitcoinWalletProvider : IBitcoinWalletProvider
{
    public (WalletVersion walletVersion, PubKey pubKey) GetNewWalletPublicKey(int uniqueId)
    {
        var mnemonic =
            new Mnemonic(
                "notable dice nurse december correct spy indicate chaos garment gate require there match transfer vast casino market degree brand puzzle news dragon summer weather");
        var masterKey = mnemonic.DeriveExtKey("my master password");

        var versionExtKey = masterKey.Derive((int)NewWalletsVersion(), hardened: true);

        var extKey = versionExtKey.Derive(uniqueId, hardened: true);

        return (NewWalletsVersion(), extKey.GetPublicKey());
    }

    public ExtPubKey GetMasterPublicKey()
    {
        var mnemonic =
            new Mnemonic(
                "notable dice nurse december correct spy indicate chaos garment gate require there match transfer vast casino market degree brand puzzle news dragon summer weather");
        var masterKey = mnemonic.DeriveExtKey("my master password");

        return masterKey.Neuter();
    }

    public Key GetPrivateKeyById(WalletVersion version,int uniqueId)
    {
        var mnemonic =
            new Mnemonic(
                "notable dice nurse december correct spy indicate chaos garment gate require there match transfer vast casino market degree brand puzzle news dragon summer weather");
        var masterKey = mnemonic.DeriveExtKey("my master password");

        var versionExtKey = masterKey.Derive((int)version, hardened: true);

        var extKey = versionExtKey.Derive(uniqueId, hardened: true);

        return extKey.PrivateKey;


    }

    public WalletVersion NewWalletsVersion() => WalletVersion.V1;
}
