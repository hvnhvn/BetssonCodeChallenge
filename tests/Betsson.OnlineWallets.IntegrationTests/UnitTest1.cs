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
        private OnlineWalletsAppFactory? _appFactory;
        private HttpClient? _httpClient;


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
            var _repositoryMock = new OnlineWalletRepositoryMock();
            collection.AddScoped<IOnlineWalletRepository>(_ => _repositoryMock);
        }
        [Test]
        public async Task Balance()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            
            var response = await _httpClient.GetAsync("/onlinewallet/balance");

            Assert.That((int)response.StatusCode, Is.EqualTo(200));

            var body = await response.Content.ReadAsStringAsync();
            var balance = JsonSerializer.Deserialize<BalanceResponse>(body, options);
            Assert.That(balance.Amount, Is.EqualTo(0m));
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