using Betsson.OnlineWallets.SystemTests.Models;
using Betsson.OnlineWallets.Web.Models;
using System.Net.Http.Json;

namespace Betsson.OnlineWallets.SystemTests
{
    [TestFixture]
    [Timeout(3000)]
    [NonParallelizable]
    public class OnlineWalletApiMultiRequestTests
    {
        private HttpClient? _httpClient;

        private static readonly Uri BaseAddress =
            new("http://localhost:8080");

        private static readonly object[] MutuallyExclusiveWithdrawals =
        {
            new object[] { 3m, 4m, 5m, 2m }
        };

        private static readonly object[] MultipleWithdrawals =
        {
            new object[] { 1m, 100 }
        };

        private static readonly object[] MultipleDeposits =
        {
            new object[] { 1m, 100 }
        };

        [SetUp]
        public async Task Setup()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = BaseAddress
            };

            var response = await _httpClient.GetAsync("onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.GreaterThanOrEqualTo(0m),
                "For system testing purposes we expect balance to have a non-negative value.");

            if (balanceResponse.Amount > 0m)
            {
                var request = new WithdrawalRequest { Amount = balanceResponse.Amount };
                response = await _httpClient.PostAsJsonAsync("onlinewallet/withdraw", request);
                response.EnsureSuccessStatusCode();
                balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();
                Assert.That(balanceResponse, Is.Not.Null);
                Assert.That(balanceResponse.Amount, Is.EqualTo(0m));
            }
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient?.Dispose();
        }

        // This test fails, as the server is unable to correctly handle multiple simultaneous requests
        // It is caused by an implementation of OnlineWalletContext
        [TestCaseSource(nameof(MultipleDeposits))]
        public async Task PostDeposit_MultipleRequests(decimal depositAmount, int iterations)
        {
            var expectedFinalBalance = depositAmount * iterations;
            var depositRequest = new DepositRequest { Amount = depositAmount };
            var taskList = new List<Task>();

            for (int i = 0; i < iterations; i++)
            {
                var task = _httpClient.PostAsJsonAsync("onlinewallet/deposit", depositRequest);
                taskList.Add(task);
            }
            await Task.WhenAll(taskList);

            var response = await _httpClient.GetAsync("onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(expectedFinalBalance),
                $"Expected balance after all deposits is {expectedFinalBalance}, but was {balanceResponse.Amount}.");
        }

        // This test fails, as the server is unable to correctly handle multiple simultaneous requests
        // It is caused by an implementation of OnlineWalletContext
        [TestCaseSource(nameof(MultipleWithdrawals))]
        public async Task PostWithdraw_MultipleRequests(decimal withdrawAmount, int iterations)
        {
            var depositRequest = new DepositRequest { Amount = iterations * withdrawAmount };
            var response = await _httpClient.PostAsJsonAsync("onlinewallet/deposit", depositRequest);
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(response, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(iterations * withdrawAmount));

            var withdrawalRequest = new WithdrawalRequest { Amount = withdrawAmount };
            var taskList = new List<Task>();

            for (int i = 0; i < iterations; i++)
            {
                var task = _httpClient.PostAsJsonAsync("onlinewallet/withdraw", withdrawalRequest);
                taskList.Add(task);
            }
            await Task.WhenAll(taskList);

            response = await _httpClient.GetAsync("onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(0m),
                $"Expected balance after all withdrawals is 0, but was {balanceResponse.Amount}.");
        }

        // This test may fail, as the server is unable to correctly handle multiple simultaneous requests
        [TestCaseSource(nameof(MutuallyExclusiveWithdrawals))]
        public async Task PostWithdraw_MutuallyExclusiveRequests(
            decimal firstWithdrawal, decimal secondWithdrawal, decimal initialBalance, decimal expectedNewBalance)
        {
            var depositRequest = new DepositRequest { Amount = initialBalance };
            var response = await _httpClient.PostAsJsonAsync("onlinewallet/deposit", depositRequest);
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(initialBalance));

            var request1 = new WithdrawalRequest { Amount = firstWithdrawal };
            var request2 = new WithdrawalRequest { Amount = secondWithdrawal };

            var task1 = _httpClient.PostAsJsonAsync("onlinewallet/withdraw", request1);
            var task2 = _httpClient.PostAsJsonAsync("onlinewallet/withdraw", request2);
            await Task.WhenAll(task1, task2);

            var response1 = task1.Result;
            var response2 = task2.Result;

            response1.EnsureSuccessStatusCode();
            Assert.That((int)response2.StatusCode, Is.EqualTo(400),
                "The seconds request should be responded with code 400: \"InsufficientBalanceException\" ");

            var balanceResponse1 = await response1.Content.ReadFromJsonAsync<BalanceResponse>();
            var badRequestResponse = await response2.Content.ReadFromJsonAsync<BadRequestResponse>();
            Assert.That(balanceResponse1, Is.Not.Null);
            Assert.That(balanceResponse1.Amount, Is.EqualTo(expectedNewBalance));
            Assert.That(badRequestResponse, Is.Not.Null);
            Assert.That(badRequestResponse.Type, Is.EqualTo("InsufficientBalanceException"));

            response = await _httpClient.GetAsync("onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(expectedNewBalance));
        }
    }
}