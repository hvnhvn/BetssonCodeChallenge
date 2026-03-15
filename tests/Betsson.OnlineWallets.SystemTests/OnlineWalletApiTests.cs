using System.Net.Http.Json;
using Betsson.OnlineWallets.SystemTests.Models;
using Betsson.OnlineWallets.Web.Models;

namespace Betsson.OnlineWallets.SystemTests
{
    [TestFixture]
    [Timeout(3000)]
    [NonParallelizable]
    public class OnlineWalletApiTests
    {
        private HttpClient? _httpClient;

        private static readonly Uri BaseAddress =
            new("http://localhost:8080");

        private static readonly object[] Deposit_PositiveCases =
        {
            new object[] { 0m },
            new object[] { 1.5m },
            new object[] { decimal.MaxValue }
        };

        private static readonly object[] Deposit_NegativeCases_NegativeInput =
        {
            new object[] { -1m }
        };

        private static readonly object[] Deposit_NegativeCases_OverflowingBalance =
        {
            new object[] { decimal.MaxValue }
        };

        private static readonly object[] Deposit_NegativeCases_IncorrectType =
        {
            new object[] { "" },
            new object[] { "qweasd" },
            new object[] { "7922816251426433759354395033400000" }
        };

        private static readonly object[] Withdraw_PositiveCases =
        {
            new object[] { 0m },
            new object[] { 1m },
            new object[] { 2.5m },
            new object[] { decimal.MaxValue }
        };

        private static readonly object[] Withdraw_NegativeCases_NegativeInput =
        {
            new object[] { -1m }
        };

        private static readonly object[] Withdraw_NegativeCases_BalanceIsTooLow =
        {
            new object[] { 1m }
        };

        private static readonly object[] Withdraw_NegativeCases_IncorrectType =
        {
            new object[] { "" },
            new object[] { "qweasd" },
            new object[] { "7922816251426433759354395033400000" }
        };

        private static readonly object[] MixedScenario =
        {
            new object[] { 4m, 5m },
            new object[] { 1000m, 900m}
        };

        [SetUp]
        public void Setup()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = BaseAddress
            };
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient?.Dispose();
        }

        [Test]
        public async Task GetBalance_ShouldReturnBalance()
        {
            var response1 = await _httpClient.GetAsync("/onlinewallet/balance");
            response1.EnsureSuccessStatusCode();
            var balanceResponse1 = await response1.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse1, Is.Not.Null);
            var balance1 = balanceResponse1.Amount;

            var response2 = await _httpClient.GetAsync("/onlinewallet/balance");
            response2.EnsureSuccessStatusCode();
            var balanceResponse2 = await response2.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse2, Is.Not.Null);
            var balance2 = balanceResponse2.Amount;

            Assert.That(balance2, Is.EqualTo(balance1));
        }

        [TestCaseSource(nameof(Deposit_PositiveCases))]
        public async Task PostDeposit_ShouldCorrectlyIncreaseBalance(decimal depositAmount)
        {
            var balanceCheckResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceCheckResponse.EnsureSuccessStatusCode();
            var balanceCheckModel = await balanceCheckResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceCheckModel, Is.Not.Null);
            var currentBalance = balanceCheckModel.Amount;

            if (decimal.MaxValue - currentBalance <= depositAmount)
            {
                var topUpResponse = await _httpClient.PostAsJsonAsync(
                    "/onlinewallet/withdraw", new WithdrawalRequest { Amount = currentBalance });
                topUpResponse.EnsureSuccessStatusCode();
            }

            var balanceBeforeResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceBeforeResponse.EnsureSuccessStatusCode();
            var balanceBeforeModel = await balanceBeforeResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceBeforeModel, Is.Not.Null);
            var balanceBefore = balanceBeforeModel.Amount;

            var request = new DepositRequest { Amount = depositAmount };
            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/deposit", request);
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(balanceBefore + depositAmount));

            var balanceAfterResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceAfterResponse.EnsureSuccessStatusCode();
            var balanceAfterModel = await balanceAfterResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceAfterModel, Is.Not.Null);
            var balanceAfter = balanceAfterModel.Amount;

            Assert.That(balanceAfter, Is.EqualTo(balanceBefore + depositAmount));
        }

        [TestCaseSource(nameof(Deposit_NegativeCases_NegativeInput))]
        public async Task PostDeposit_ShouldRejectNegativeInput(decimal depositAmount)
        {
            var balanceBeforeResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceBeforeResponse.EnsureSuccessStatusCode();
            var balanceBeforeModel = await balanceBeforeResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceBeforeModel, Is.Not.Null);
            var balanceBefore = balanceBeforeModel.Amount;

            var request = new DepositRequest { Amount = depositAmount };
            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/deposit", request);
            Assert.That((int)response.StatusCode, Is.EqualTo(400));

            var balanceAfterResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceAfterResponse.EnsureSuccessStatusCode();
            var balanceAfterModel = await balanceAfterResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceAfterModel, Is.Not.Null);
            var balanceAfter = balanceAfterModel.Amount;

            Assert.That(balanceAfter, Is.EqualTo(balanceBefore));
        }

        [TestCaseSource(nameof(Deposit_NegativeCases_OverflowingBalance))]
        public async Task PostDeposit_ShouldReject_WhenBalanceGetsOverflown(decimal depositAmount)
        {
            // I intentionally skip this case here, as being performed it would corrupt the state of server,
            // and make the rest of tests fail.
            // The runnable version of this test may be found in Betsson.OnlineWallets.IntegrationTests
        }

        [TestCaseSource(nameof(Deposit_NegativeCases_IncorrectType))]
        public async Task PostDeposit_ShouldRejectIncorrectType(string input)
        {
            var balanceBeforeResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceBeforeResponse.EnsureSuccessStatusCode();
            var balanceBeforeModel = await balanceBeforeResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceBeforeModel, Is.Not.Null);
            var balanceBefore = balanceBeforeModel.Amount;

            var request = new IncorrectDepositRequest { Amount = input };
            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/deposit", request);
            Assert.That((int)response.StatusCode, Is.EqualTo(400));

            var balanceAfterResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceAfterResponse.EnsureSuccessStatusCode();
            var balanceAfterModel = await balanceAfterResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceAfterModel, Is.Not.Null);
            var balanceAfter = balanceAfterModel.Amount;

            Assert.That(balanceAfter, Is.EqualTo(balanceBefore));
        }

        [TestCaseSource(nameof(Withdraw_PositiveCases))]
        public async Task PostWithdraw_ShouldCorrectlyDecreaseBalance_WhenEnoughFunds(decimal withdrawAmount)
        {
            var balanceCheckResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceCheckResponse.EnsureSuccessStatusCode();
            var balanceCheckModel = await balanceCheckResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceCheckModel, Is.Not.Null);
            var currentBalance = balanceCheckModel.Amount;

            if (currentBalance < withdrawAmount)
            {
                var topUpAmount = withdrawAmount - currentBalance;
                var topUpResponse = await _httpClient.PostAsJsonAsync(
                    "/onlinewallet/deposit", new DepositRequest { Amount = topUpAmount });
                topUpResponse.EnsureSuccessStatusCode();
            }

            var balanceBeforeResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceBeforeResponse.EnsureSuccessStatusCode();
            var balanceBeforeModel = await balanceBeforeResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceBeforeModel, Is.Not.Null);
            var balanceBefore = balanceBeforeModel.Amount;

            var request = new WithdrawalRequest { Amount = withdrawAmount };
            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/withdraw", request);
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(balanceBefore - withdrawAmount));

            var balanceAfterResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceAfterResponse.EnsureSuccessStatusCode();
            var balanceAfterModel = await balanceAfterResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceAfterModel, Is.Not.Null);
            var balanceAfter = balanceAfterModel.Amount;

            Assert.That(balanceAfter, Is.EqualTo(balanceBefore - withdrawAmount));
        }

        [TestCaseSource(nameof(Withdraw_NegativeCases_NegativeInput))]
        public async Task PostWithdraw_ShouldRejectNegativeInput(decimal withdrawAmount)
        {
            var balanceBeforeResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceBeforeResponse.EnsureSuccessStatusCode();
            var balanceBeforeModel = await balanceBeforeResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceBeforeModel, Is.Not.Null);
            var balanceBefore = balanceBeforeModel.Amount;

            var request = new WithdrawalRequest { Amount = withdrawAmount };
            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/withdraw", request);
            Assert.That((int)response.StatusCode, Is.EqualTo(400));

            var balanceAfterResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceAfterResponse.EnsureSuccessStatusCode();
            var balanceAfterModel = await balanceAfterResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceAfterModel, Is.Not.Null);
            var balanceAfter = balanceAfterModel.Amount;

            Assert.That(balanceAfter, Is.EqualTo(balanceBefore));
        }

        [TestCaseSource(nameof(Withdraw_NegativeCases_BalanceIsTooLow))]
        public async Task PostWithdraw_ShouldReject_WhenBalanceIsTooLow(decimal difference)
        {
            var balanceBeforeResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceBeforeResponse.EnsureSuccessStatusCode();
            var balanceBeforeModel = await balanceBeforeResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceBeforeModel, Is.Not.Null);
            var balanceBefore = balanceBeforeModel.Amount;

            var withdrawAmount = balanceBefore + difference;
            var request = new WithdrawalRequest { Amount = withdrawAmount };
            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/withdraw", request);
            Assert.That((int)response.StatusCode, Is.EqualTo(400));
            var badRequestResponse = await response.Content.ReadFromJsonAsync<BadRequestResponse>();
            Assert.That(badRequestResponse, Is.Not.Null);
            Assert.That(badRequestResponse.Type, Is.EqualTo("InsufficientBalanceException"));

            var balanceAfterResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceAfterResponse.EnsureSuccessStatusCode();
            var balanceAfterModel = await balanceAfterResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceAfterModel, Is.Not.Null);
            var balanceAfter = balanceAfterModel.Amount;

            Assert.That(balanceAfter, Is.EqualTo(balanceBefore));
        }

        [TestCaseSource(nameof(Withdraw_NegativeCases_IncorrectType))]
        public async Task PostWithdraw_ShouldRejectIncorrectType(string input)
        {
            var balanceBeforeResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceBeforeResponse.EnsureSuccessStatusCode();
            var balanceBeforeModel = await balanceBeforeResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceBeforeModel, Is.Not.Null);
            var balanceBefore = balanceBeforeModel.Amount;

            var request = new IncorrectWithdrawalRequest { Amount = input };
            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/withdraw", request);
            Assert.That((int)response.StatusCode, Is.EqualTo(400));

            var balanceAfterResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceAfterResponse.EnsureSuccessStatusCode();
            var balanceAfterModel = await balanceAfterResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceAfterModel, Is.Not.Null);
            var balanceAfter = balanceAfterModel.Amount;

            Assert.That(balanceAfter, Is.EqualTo(balanceBefore));
        }

        [TestCaseSource(nameof(MixedScenario))]
        public async Task MixedScenario_ShouldReturnCorrectBalance_AfterAChainOfRequests(decimal depositAmount, decimal withdrawAmount)
        {
            var balanceCheckResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceCheckResponse.EnsureSuccessStatusCode();
            var balanceCheckModel = await balanceCheckResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceCheckModel, Is.Not.Null);
            var currentBalance = balanceCheckModel.Amount;

            if (decimal.MaxValue - currentBalance <= depositAmount)
            {
                var topUpResponse = await _httpClient.PostAsJsonAsync(
                    "/onlinewallet/withdraw", new DepositRequest { Amount = currentBalance });
                topUpResponse.EnsureSuccessStatusCode();
            }

            balanceCheckResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceCheckResponse.EnsureSuccessStatusCode();
            balanceCheckModel = await balanceCheckResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceCheckModel, Is.Not.Null);
            currentBalance = balanceCheckModel.Amount;

            if (currentBalance + depositAmount < withdrawAmount)
            {
                var topUpAmount = withdrawAmount - currentBalance - depositAmount;
                var topUpResponse = await _httpClient.PostAsJsonAsync(
                    "/onlinewallet/deposit", new DepositRequest { Amount = topUpAmount });
                topUpResponse.EnsureSuccessStatusCode();
            }

            var balanceBeforeResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            balanceBeforeResponse.EnsureSuccessStatusCode();
            var balanceBeforeModel = await balanceBeforeResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(balanceBeforeModel, Is.Not.Null);
            var balanceBefore = balanceBeforeModel.Amount;

            var depositResponse = await _httpClient.PostAsJsonAsync(
                "/onlinewallet/deposit", new DepositRequest { Amount = depositAmount });
            depositResponse.EnsureSuccessStatusCode();
            var afterDeposit = await depositResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(afterDeposit, Is.Not.Null);
            Assert.That(afterDeposit.Amount, Is.EqualTo(balanceBefore + depositAmount));

            var withdrawResponse = await _httpClient.PostAsJsonAsync(
                "/onlinewallet/withdraw", new WithdrawalRequest { Amount = withdrawAmount });
            withdrawResponse.EnsureSuccessStatusCode();
            var afterWithdraw = await withdrawResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(afterWithdraw, Is.Not.Null);
            Assert.That(afterWithdraw.Amount, Is.EqualTo(balanceBefore + depositAmount - withdrawAmount));

            var finalBalanceResponse = await _httpClient.GetAsync("/onlinewallet/balance");
            finalBalanceResponse.EnsureSuccessStatusCode();
            var finalBalanceModel = await finalBalanceResponse.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.That(finalBalanceModel, Is.Not.Null);
            var finalBalance = finalBalanceModel.Amount;

            Assert.That(finalBalance, Is.EqualTo(balanceBefore + depositAmount - withdrawAmount));
        }
    }
}