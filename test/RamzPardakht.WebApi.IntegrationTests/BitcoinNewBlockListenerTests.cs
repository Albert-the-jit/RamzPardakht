// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NBitcoin;
using NBXplorer;
using NBXplorer.Models;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.MessageModels;
using RamzPardakht.WebApi.BackgroundServices;
using RamzPardakht.WebApi.Consumers;

namespace RamzPardakht.WebApi.IntegrationTests;

public class BitcoinNewBlockListenerTests
{
    [Fact]
    public async Task Test_Will_New_Block_Event_Publish_When_New_Bitcoin_Block_Mined()
    {
        var cancellationTokenSource = new CancellationTokenSource();

        var serviceCollection = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<NewBitcoinBlockEventConsumer>();
            });
        var bitcoinWalletProvider = new Mock<IBitcoinWalletProvider>();
        bitcoinWalletProvider.Setup(walletProvider => walletProvider.GetMasterPublicKey())
            .Returns(new ExtKey().Neuter());

        serviceCollection.AddTransient<IBitcoinWalletProvider>(serviceProvider => bitcoinWalletProvider.Object);
        serviceCollection.AddHostedService<BitcoinNewBlockListener>();

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.Setup(environment => environment.EnvironmentName).Returns("Test");

        serviceCollection.AddTransient<IHostEnvironment>(serviceProvider => hostEnvironment.Object);

        var explorerClientAdapterMock = new Mock<IExplorerClientAdapter<Bitcoin>>();
        explorerClientAdapterMock.Setup(adapter => adapter.NbXplorerNetwork)
            .Returns(() => new NBXplorerNetworkProvider(ChainName.Testnet).GetBTC());

        explorerClientAdapterMock.Setup(adapter => adapter.ListenNewBlockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NewBlockEvent() { CryptoCode = "BTC" }, TimeSpan.FromSeconds(5));

        serviceCollection.AddTransient(_ => explorerClientAdapterMock);
        serviceCollection.AddTransient<IExplorerClientAdapter<Bitcoin>>(_ => explorerClientAdapterMock.Object);

        await using var provider = serviceCollection.BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();

        var hostedService = provider.GetService<IHostedService>();

        hostedService.StartAsync(cancellationTokenSource.Token);

        await Task.Delay(500);

        explorerClientAdapterMock.Verify();

        (await harness.Published.Any<NewBitcoinBlockEvent>()).Should().BeTrue();
        harness.Published.Select(context => context.MessageType == typeof(NewBitcoinBlockEvent)).Count().Should().Be(1);

        await Task.Delay(5000);

        cancellationTokenSource.Cancel();

        harness.Published.Select(context => context.MessageType == typeof(NewBitcoinBlockEvent)).Count().Should().Be(2);

    }
}
