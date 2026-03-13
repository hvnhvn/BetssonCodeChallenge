using Betsson.OnlineWallets.IntegrationTests.Betsson.OnlineWallets.Web.ApiTests;
using Betsson.OnlineWallets.Web.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace Betsson.OnlineWallets.IntegrationTests
{
    public class Tests
    {
        private HttpClient _httpClient;

        [TearDown]
        public async Task TearDown()
        {
            _httpClient.Dispose();
        }

        [Test]
        public async Task Balance()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            var app = new OnlineWalletsAppFactory();
            _httpClient = app.CreateClient();

            var response = await _httpClient.GetAsync("/onlinewallet/balance");

            Assert.That((int)response.StatusCode, Is.EqualTo(200));

            var body = await response.Content.ReadAsStringAsync();
            var balance = JsonSerializer.Deserialize<BalanceResponse>(body, options);
            Assert.That(balance.Amount, Is.EqualTo(10m));
        }

        [Test]
        public async Task Deposit()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            var app = new OnlineWalletsAppFactory();
            _httpClient = app.CreateClient();


            var payload = JsonContent.Create(new DepositRequest { Amount = 5m });

            var response = await _httpClient.PostAsync("/onlinewallet/deposit", payload);

            Assert.That((int)response.StatusCode, Is.EqualTo(200));

            var body = await response.Content.ReadAsStringAsync();
            var balance = JsonSerializer.Deserialize<BalanceResponse>(body, options);
            Assert.That(balance.Amount, Is.EqualTo(15m));
        }
    }
}