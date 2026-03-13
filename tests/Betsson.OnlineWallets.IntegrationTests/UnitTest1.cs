using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.IntegrationTests.Betsson.OnlineWallets.Web.ApiTests;
using Betsson.OnlineWallets.IntegrationTests.Mocks;
using Betsson.OnlineWallets.Web.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;

namespace Betsson.OnlineWallets.IntegrationTests
{
    public class Tests
    {
        private IOnlineWalletRepository? _repositoryMock;
        private OnlineWalletsAppFactory? _appFactory;
        private HttpClient? _httpClient;

        private static object[] Balance_PositiveCases =
        {
            new object[] { 10m },
            new object[] { decimal.MaxValue }
        };

        private static object[] Deposit_PositiveCase_NoEntries =
        {
            new object[] { 0m },
            new object[] { 1m },
            new object[] { decimal.MaxValue }
        };

        private static object[] Deposit_PositiveCases =
        {
            new object[] { 0m, -1m, -1m },
            new object[] { 0m, 0m, 0m },
            new object[] { 0m, 1m, 1m },
            new object[] { 0m, decimal.MaxValue, decimal.MaxValue },
            new object[] { 1m, -1m, 0m },
            new object[] { 1m, 0m, 1m },
            new object[] { 1m, 1m, 2m },
            new object[] { 1m, decimal.MaxValue - 1m, decimal.MaxValue },
            new object[] { decimal.MaxValue - 1m, -1m, decimal.MaxValue - 2m },
            new object[] { decimal.MaxValue - 1m, 0m, decimal.MaxValue - 1m },
            new object[] { decimal.MaxValue - 1m, 1m, decimal.MaxValue },
            new object[] { decimal.MaxValue, 0m, decimal.MaxValue }
        };

        private static readonly object[] Deposit_NegativeCases =
        {
            // overflow
            // incorrect input
        };

        [SetUp]
        public void Setup()
        {
            _appFactory = new OnlineWalletsAppFactory(ConfigureServices);
            _httpClient = _appFactory.CreateClient();
        }
        [TearDown]
        public void TearDown()
        {
            _appFactory?.Dispose();
            _httpClient?.Dispose();
        }

        void ConfigureServices(IServiceCollection collection)
        {
            _repositoryMock = new OnlineWalletRepositoryMock();
            collection.AddScoped<IOnlineWalletRepository>(_ => _repositoryMock);
        }

        [Test]
        public async Task GetBalance_InitialBalanceShouldBeZero()
        {
            var response = await _httpClient.GetAsync("/onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.Zero);
        }

        [TestCaseSource(nameof(Balance_PositiveCases))]
        public async Task GetBalance_ShouldReturnCorrectBalance(decimal balance)
        {
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = 0,
                BalanceBefore = balance,
            };
            await _repositoryMock.InsertOnlineWalletEntryAsync(onlineWalletEntry);

            var response = await _httpClient.GetAsync("/onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(balance));

        }




        [TestCaseSource(nameof(Deposit_PositiveCase_NoEntries))]
        public async Task PostDeposit_ShouldReturnCorrectBalance_WhenThereWereNoEntries(decimal depositAmount)
        {
            var request = new DepositRequest { Amount = depositAmount };

            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/deposit", request);
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(depositAmount));
        }

        [TestCaseSource(nameof(Deposit_PositiveCases))]
        public async Task PostDeposit_ShouldReturnCorrectBalance(decimal depositAmount, decimal balance, decimal expectedNewBalance)
        {
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = 0,
                BalanceBefore = balance
            };
            await _repositoryMock.InsertOnlineWalletEntryAsync(onlineWalletEntry);
            var request = new DepositRequest { Amount = depositAmount };

            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/deposit", request);
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(expectedNewBalance));
        }



        
    }
}