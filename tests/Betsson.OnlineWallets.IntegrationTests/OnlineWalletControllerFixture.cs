using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.IntegrationTests.Betsson.OnlineWallets.Web.ApiTests;
using Betsson.OnlineWallets.IntegrationTests.Mocks;
using Betsson.OnlineWallets.IntegrationTests.Models;
using Betsson.OnlineWallets.Web.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace Betsson.OnlineWallets.IntegrationTests
{
    public class OnlineWalletControllerFixture
    {
        private IOnlineWalletRepository? _repositoryMock;
        private OnlineWalletsAppFactory? _appFactory;
        private HttpClient? _httpClient;

        private static readonly object[] Balance_PositiveCases =
        {
            new object[] { decimal.MinValue },
            new object[] { 10m },
            new object[] { decimal.MaxValue }
        };

        private static readonly object[] Deposit_PositiveCase_NoEntries =
        {
            new object[] { 0m },
            new object[] { 1m },
            new object[] { decimal.MaxValue }
        };

        private static readonly object[] Deposit_PositiveCases =
        {
            new object[] { 0m, decimal.MinValue, decimal.MinValue },
            new object[] { 0m, -1m, -1m },
            new object[] { 0m, 0m, 0m },
            new object[] { 0m, 1m, 1m },
            new object[] { 0m, decimal.MaxValue, decimal.MaxValue },

            new object[] { 1m, decimal.MinValue, decimal.MinValue + 1m },
            new object[] { 1m, -1m, 0m },
            new object[] { 1m, 0m, 1m },
            new object[] { 1m, 1m, 2m },
            new object[] { 1m, decimal.MaxValue - 1m, decimal.MaxValue },

            new object[] { decimal.MaxValue - 1m, decimal.MinValue, -1m },
            new object[] { decimal.MaxValue - 1m, -1m, decimal.MaxValue - 2m },
            new object[] { decimal.MaxValue - 1m, 0m, decimal.MaxValue - 1m },
            new object[] { decimal.MaxValue - 1m, 1m, decimal.MaxValue },
            new object[] { decimal.MaxValue, 0m, decimal.MaxValue }
        };

        private static readonly object[] Deposit_NegativeCases_NegativeInput =
        {
            new object[] { -1m, 0m },
            new object[] { -1m, 1m }
        };

        private static readonly object[] Deposit_NegativeCases_OverflowingBalance =
        {
            new object[] { 1m, decimal.MaxValue },
            new object[] { decimal.MaxValue, 1m }
        };

        private static readonly object[] Deposit_NegativeCases_IncorrectType =
        {
            new object[] { "", 0m },
            new object[] { "qweasd", 0m },
            new object[] { "7922816251426433759354395033400000", 0m }
        };

        private static readonly object[] Withdraw_PositiveCases =
        {
            new object[] { 0m, 0m, 0m },
            new object[] { 0m, 1m, 1m },
            new object[] { 0m, decimal.MaxValue, decimal.MaxValue },

            new object[] { 1m, 1m, 0m },
            new object[] { 1m, 2m, 1m },
            new object[] { 1m, decimal.MaxValue, decimal.MaxValue - 1m },

            new object[] { decimal.MaxValue - 1m, decimal.MaxValue, 1m },
            new object[] { decimal.MaxValue, decimal.MaxValue, 0m }
        };

        private static readonly object[] Withdraw_NegativeCases_NegativeInput =
        {
            new object[] { -1m, -1m },
            new object[] { -1m, 0m }
        };

        private static readonly object[] Withdraw_NegativeCases_BalanceIsTooLow =
        {
            new object[] { 0m, decimal.MinValue },
            new object[] { 0m, -1m },
            new object[] { 1m, 0m },
            new object[] { decimal.MaxValue, decimal.MaxValue - 1m }
        };

        private static readonly object[] Withdraw_NegativeCases_IncorrectType =
        {
            new object[] { "", decimal.MaxValue },
            new object[] { "qweasd", decimal.MaxValue },
            new object[] { "7922816251426433759354395033400000", decimal.MaxValue }
        };

        private static readonly object[] MixedScenario =
        {
            new object[] { 3m, 4m, 5m, 2m }
        };

        [SetUp]
        public void Setup()
        {
            _appFactory = new OnlineWalletsAppFactory(ConfigureServices);
            _httpClient = _appFactory.CreateClient();

            void ConfigureServices(IServiceCollection collection)
            {
                _repositoryMock = new OnlineWalletRepositoryMock();
                collection.AddScoped<IOnlineWalletRepository>(_ => _repositoryMock);
            }
        }

        [TearDown]
        public void TearDown()
        {
            _appFactory?.Dispose();
            _httpClient?.Dispose();
        }
        
        [Test]
        public async Task GetBalance_InitialBalanceShouldBeZero()
        {
            var response = await _httpClient.GetAsync("/onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.Zero);

            response = await _httpClient.GetAsync("/onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

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

            response = await _httpClient.GetAsync("/onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

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

        [TestCaseSource(nameof(Deposit_NegativeCases_NegativeInput))]
        public async Task PostDeposit_ShouldNotProcessNegativeInput(decimal depositAmount, decimal balance)
        {
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = 0,
                BalanceBefore = balance
            };
            await _repositoryMock.InsertOnlineWalletEntryAsync(onlineWalletEntry);
            var request = new DepositRequest { Amount = depositAmount };

            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/deposit", request);
            Assert.That((int)response.StatusCode, Is.EqualTo(400));

            response = await _httpClient.GetAsync("/onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(balance));
        }

        // This test fails, as after provoking OverflowException by making balance too high, the application doesn't recover
        // All the following requests (even valid ones) get response 500
        // I'd consider this an issue
        [TestCaseSource(nameof(Deposit_NegativeCases_OverflowingBalance))]
        public async Task PostDeposit_ShouldNotDepositFunds_WhenBalanceGetsOverflown(decimal depositAmount, decimal balance)
        {
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = 0,
                BalanceBefore = balance
            };
            await _repositoryMock.InsertOnlineWalletEntryAsync(onlineWalletEntry);
            var request = new DepositRequest { Amount = depositAmount };

            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/deposit", request);
            Assert.That((int)response.StatusCode, Is.EqualTo(500));

            response = await _httpClient.GetAsync("/onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(balance));
        }

        [TestCaseSource(nameof(Deposit_NegativeCases_IncorrectType))]
        public async Task PostDeposit_ShouldNotProcess_WhenInputHasIncorrectType(string input, decimal balance)
        {
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = 0,
                BalanceBefore = balance
            };
            await _repositoryMock.InsertOnlineWalletEntryAsync(onlineWalletEntry);
            var request = new IncorrectDepositRequest { Amount = input };

            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/deposit", request);
            Assert.That((int)response.StatusCode, Is.EqualTo(400));

            response = await _httpClient.GetAsync("/onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(balance));
        }

        [Test]
        public async Task PostWithdraw_ShouldBeAbleToWithdrawZeroFunds_WhenThereWereNoEntries()
        {
            var request = new WithdrawalRequest { Amount = 0m };

            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/withdraw", request);
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(0m));
        }

        [TestCaseSource(nameof(Withdraw_PositiveCases))]
        public async Task PostWithdraw_ShouldReturnCorrectBalance(decimal withdrawAmount, decimal balance, decimal expectedNewBalance)
        {
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = 0,
                BalanceBefore = balance
            };
            await _repositoryMock.InsertOnlineWalletEntryAsync(onlineWalletEntry);
            var request = new WithdrawalRequest { Amount = withdrawAmount };

            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/withdraw", request);
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(expectedNewBalance));
        }

        [TestCaseSource(nameof(Withdraw_NegativeCases_NegativeInput))]
        public async Task PostWithdraw_ShouldNotProcessNegativeInput(decimal withdrawAmount, decimal balance)
        {
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = 0,
                BalanceBefore = balance
            };
            await _repositoryMock.InsertOnlineWalletEntryAsync(onlineWalletEntry);
            var request = new DepositRequest { Amount = withdrawAmount };

            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/withdraw", request);
            Assert.That((int)response.StatusCode, Is.EqualTo(400));

            response = await _httpClient.GetAsync("/onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(balance));
        }

        [TestCaseSource(nameof(Withdraw_NegativeCases_BalanceIsTooLow))]
        public async Task PostWithdraw_ShouldNotWithdrawFunds_WhenBalanceIsTooLow(decimal withdrawAmount, decimal balance)
        {
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = 0,
                BalanceBefore = balance
            };
            await _repositoryMock.InsertOnlineWalletEntryAsync(onlineWalletEntry);
            var request = new WithdrawalRequest { Amount = withdrawAmount };

            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/withdraw", request);
            Assert.That((int)response.StatusCode, Is.EqualTo(400));

            response = await _httpClient.GetAsync("/onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(balance));
        }

        [TestCaseSource(nameof(Withdraw_NegativeCases_IncorrectType))]
        public async Task PostWithdraw_ShouldNotProcess_WhenInputHasIncorrectType(string input, decimal balance)
        {
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = 0,
                BalanceBefore = balance
            };
            await _repositoryMock.InsertOnlineWalletEntryAsync(onlineWalletEntry);
            var request = new IncorrectWithdrawalRequest { Amount = input };

            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/withdraw", request);
            Assert.That((int)response.StatusCode, Is.EqualTo(400));

            response = await _httpClient.GetAsync("/onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(balance));
        }

        [TestCaseSource(nameof(MixedScenario))]
        public async Task MixedScenario_ShouldReturnCorrectBalance_AfterAChainOfRequests(
            decimal balance, decimal depositAmount, decimal withdrawAmount, decimal expectedNewBalance)
        {
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = 0,
                BalanceBefore = balance
            };
            await _repositoryMock.InsertOnlineWalletEntryAsync(onlineWalletEntry);
            var depositRequest = new DepositRequest { Amount = depositAmount };
            var withdrawalRequest = new WithdrawalRequest { Amount = withdrawAmount };

            var response = await _httpClient.PostAsJsonAsync("/onlinewallet/deposit", depositRequest);
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(balance + depositAmount));

            response = await _httpClient.PostAsJsonAsync("/onlinewallet/withdraw", withdrawalRequest);
            response.EnsureSuccessStatusCode();
            balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(expectedNewBalance));

            response = await _httpClient.GetAsync("/onlinewallet/balance");
            response.EnsureSuccessStatusCode();
            balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            Assert.That(balanceResponse, Is.Not.Null);
            Assert.That(balanceResponse.Amount, Is.EqualTo(expectedNewBalance));
        }
    }
}