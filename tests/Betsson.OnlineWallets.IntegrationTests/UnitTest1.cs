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




        [Test]
        public async Task Deposit()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            var payload = JsonContent.Create(new DepositRequest { Amount = 5m });

            var response = await _httpClient.PostAsync("/onlinewallet/deposit", payload);

            Assert.That((int)response.StatusCode, Is.EqualTo(200));

            var body = await response.Content.ReadAsStringAsync();
            var balance = JsonSerializer.Deserialize<BalanceResponse>(body, options);
            Assert.That(balance.Amount, Is.EqualTo(5m));
        }

        [Test]
        public async Task DepositAndCheck()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            var payload = JsonContent.Create(new DepositRequest { Amount = 5m });

            var response = await _httpClient.PostAsync("/onlinewallet/deposit", payload);
            Assert.That((int)response.StatusCode, Is.EqualTo(200));

            var body = await response.Content.ReadAsStringAsync();
            var balance = JsonSerializer.Deserialize<BalanceResponse>(body, options);
            Assert.That(balance.Amount, Is.EqualTo(5m));

            response = await _httpClient.GetAsync("/onlinewallet/balance");
            Assert.That((int)response.StatusCode, Is.EqualTo(200));

            body = await response.Content.ReadAsStringAsync();
            balance = JsonSerializer.Deserialize<BalanceResponse>(body, options);
            Assert.That(balance.Amount, Is.EqualTo(5m));



            response = await _httpClient.PostAsync("/onlinewallet/deposit", payload);
            Assert.That((int)response.StatusCode, Is.EqualTo(200));

            body = await response.Content.ReadAsStringAsync();
            balance = JsonSerializer.Deserialize<BalanceResponse>(body, options);
            Assert.That(balance.Amount, Is.EqualTo(10m));

            response = await _httpClient.GetAsync("/onlinewallet/balance");
            Assert.That((int)response.StatusCode, Is.EqualTo(200));

            body = await response.Content.ReadAsStringAsync();
            balance = JsonSerializer.Deserialize<BalanceResponse>(body, options);
            Assert.That(balance.Amount, Is.EqualTo(10m));
        }

        
    }
}