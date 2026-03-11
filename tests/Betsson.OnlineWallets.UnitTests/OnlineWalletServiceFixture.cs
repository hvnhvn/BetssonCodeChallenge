using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Exceptions;
using Betsson.OnlineWallets.Services;
using NSubstitute;

namespace Betsson.OnlineWallets.UnitTests
{
    public class Tests
    {
        static object[] GetBalance_PositiveCases =
        {

            new object[] { -1m, -1m, -2m },
            new object[] { -1m, 0m, -1m },
            new object[] { -2m, 1m, -1m },
            new object[] { -1m, 1m, 0m },
            new object[] { -1m, 2m, 1m },

            new object[] { 0m, -1m, -1m },
            new object[] { 0m, 0m, 0m },
            new object[] { 0m, 1m, 1m },

            new object[] { 1m, -2m, -1m },
            new object[] { 1m, -1m, 0m },
            new object[] { 2m, -1m, 1m },
            new object[] { 1m, 0m, 1m },
            new object[] { 1m, 1m, 2m },

            new object[] { decimal.MinValue + 1m, -1m, decimal.MinValue },
            new object[] { -1m, decimal.MinValue + 1m, decimal.MinValue },
            new object[] { decimal.MaxValue - 1m, 1m, decimal.MaxValue },
            new object[] { 1m, decimal.MaxValue - 1m, decimal.MaxValue },
        };

        static object[] GetBalance_NegativeCases =
        {
            new object[] { decimal.MaxValue, 1m },
            new object[] { 1m, decimal.MaxValue },
            new object[] { decimal.MinValue, -1m },
            new object[] { -1m, decimal.MinValue },
        };

        static object[] DepositFunds_PositiveCases =
        {
            new object[] { 0m, -1m, -1m },
            new object[] { 0m, 0m, 0m },
            new object[] { 0m, 1m, 1m },
            new object[] { 0m, decimal.MaxValue, decimal.MaxValue },

            new object[] { 1m, -1m, 0m },
            new object[] { 1m, 0m, 1m },
            new object[] { 1m, 1m, 2m },
            new object[] { 1m, decimal.MaxValue - 1m, decimal.MaxValue },

            new object[] { decimal.MaxValue, -1m, decimal.MaxValue - 1m },
            new object[] { decimal.MaxValue, 0m, decimal.MaxValue },
            new object[] { decimal.MaxValue - 1m, 1m, decimal.MaxValue },
        };

        static object[] DepositFunds_NegativeCases_BalanceExceedsMaxValue =
        {
            new object[] { decimal.MaxValue, 1m },
            new object[] { 1m, decimal.MaxValue }
        };

        static object[] DepositFunds_NegativeCases_NegativeInput =
        {
            new object[] { -1m },
            new object[] { decimal.MinValue }
        };

        static object[] WithdrawFunds_PositiveCases =
        {
            new object[] { 0m, 0m, 0m },
            new object[] { 0m, 1m, 1m },
            new object[] { 0m, decimal.MaxValue, decimal.MaxValue },

            new object[] { 1m, 1m, 0m },
            new object[] { 1m, decimal.MaxValue, decimal.MaxValue - 1m },

            new object[] { decimal.MaxValue - 1m, decimal.MaxValue, 1m },
            new object[] { decimal.MaxValue, decimal.MaxValue, 0m },
        };

        static object[] WithdrawFunds_NegativeCases_BalanceTooLow =
        {
            new object[] { 0m, -1m },
            new object[] { 1m, -1m },
            new object[] { 1m, 0m },
            new object[] { decimal.MaxValue, 1m },
            new object[] { decimal.MaxValue, decimal.MaxValue - 1m },
        };

        static object[] WithdrawFunds_NegativeCases_NegativeInput =
        {
            new object[] { -1m },
            new object[] { decimal.MinValue }
        };


        [Test]
        public async Task GetBalanceAsync_ShouldReturnZeroBalance_WhenLastOnlineWalletEntryIsDefault()
        {
            var onlineWalletRepository = Substitute.For<IOnlineWalletRepository>();
            var sut = new OnlineWalletService(onlineWalletRepository);

            var balance = await sut.GetBalanceAsync();

            Assert.That(balance.Amount, Is.Zero);
        }

        [TestCaseSource(nameof(GetBalance_PositiveCases))]
        public async Task GetBalanceAsync_ShouldReturnCorrectBalance(
            decimal amount, decimal balanceBefore, decimal expectedBalance)
        {
            var onlineWalletRepository = Substitute.For<IOnlineWalletRepository>();
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = amount,
                BalanceBefore = balanceBefore
            };
            onlineWalletRepository.GetLastOnlineWalletEntryAsync().Returns(Task.FromResult(onlineWalletEntry));

            var sut = new OnlineWalletService(onlineWalletRepository);

            var balance = await sut.GetBalanceAsync();

            Assert.That(balance.Amount, Is.EqualTo(expectedBalance));
        }

        [TestCaseSource(nameof(GetBalance_NegativeCases))]
        public async Task GetBalanceAsync_ShouldThrowOverflowExceptionOnIncorrectData(
            decimal amount, decimal balanceBefore)
        {
            var onlineWalletRepository = Substitute.For<IOnlineWalletRepository>();
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = amount,
                BalanceBefore = balanceBefore
            };
            onlineWalletRepository.GetLastOnlineWalletEntryAsync().Returns(Task.FromResult(onlineWalletEntry));

            var sut = new OnlineWalletService(onlineWalletRepository);

            Assert.ThrowsAsync<OverflowException>(async () => await sut.GetBalanceAsync(),
                "GetBalanceAsync() should've thrown OverflowException on incorrect input data!");
        }



        [TestCaseSource(nameof(DepositFunds_PositiveCases))]
        public async Task DepositFundsAsync_ShouldInsertCorrectOnlineWalletEntry_AndReturnCorrectBalance(
            decimal deposit, decimal currentBalanceValue, decimal expectedNewBalance)
        {
            var onlineWalletRepository = Substitute.For<IOnlineWalletRepository>();
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = 0m,
                BalanceBefore = currentBalanceValue
            };
            onlineWalletRepository.GetLastOnlineWalletEntryAsync().Returns(Task.FromResult(onlineWalletEntry));

            var sut = new OnlineWalletService(onlineWalletRepository);
            var currentBalance = await sut.GetBalanceAsync();

            var newBalance = await sut.DepositFundsAsync(new Models.Deposit { Amount = deposit });

            Assert.That(newBalance.Amount, Is.EqualTo(expectedNewBalance));
            await onlineWalletRepository.Received(1).InsertOnlineWalletEntryAsync(Arg.Is<OnlineWalletEntry>(o =>
                o.Amount == deposit &&
                o.BalanceBefore == currentBalance.Amount &&
                o.EventTime < DateTimeOffset.UtcNow));
        }

        // This test fails, as the SUT throws an OverflowException, yet inserts incorrect data into a DB
        // I'd consider this an issue
        [TestCaseSource(nameof(DepositFunds_NegativeCases_BalanceExceedsMaxValue))]
        public async Task DepositFundsAsync_ShouldThrowOverflowException_WhenNewBalanceExceedsMaxValue(
            decimal deposit, decimal currentBalanceValue)
        {
            var onlineWalletRepository = Substitute.For<IOnlineWalletRepository>();
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = 0m,
                BalanceBefore = currentBalanceValue
            };
            onlineWalletRepository.GetLastOnlineWalletEntryAsync().Returns(Task.FromResult(onlineWalletEntry));

            var sut = new OnlineWalletService(onlineWalletRepository);
            var currentBalance = await sut.GetBalanceAsync();


            Assert.ThrowsAsync<OverflowException>(async () =>
                await sut.DepositFundsAsync(new Models.Deposit { Amount = deposit }),
                "DepositFundsAsync() should've thrown OverflowException on incorrect data!");

            // Additionally checks that the method didn't produce a new OnlineWalletEntry
            await onlineWalletRepository.DidNotReceiveWithAnyArgs().InsertOnlineWalletEntryAsync(default);
        }

        // IMO, this should fail, as Deposit shouldn't accept negative input params
        [TestCaseSource(nameof(DepositFunds_NegativeCases_NegativeInput))]
        public async Task DepositFundsAsync_ShouldDeclineNegativeDepositAmount(decimal deposit)
        {
            var onlineWalletRepository = Substitute.For<IOnlineWalletRepository>();

            var sut = new OnlineWalletService(onlineWalletRepository);

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                await sut.DepositFundsAsync(new Models.Deposit { Amount = deposit }),
                "DepositFundsAsync() should've thrown ArgumentOutOfRangeException on negative input data!");

            // Additionally checks that the method didn't produce a new OnlineWalletEntry
            await onlineWalletRepository.DidNotReceiveWithAnyArgs().InsertOnlineWalletEntryAsync(default);
        }



        [TestCaseSource(nameof(WithdrawFunds_PositiveCases))]
        public async Task WithdrawFundsAsync_ShouldInsertCorrectOnlineWalletEntry_AndReturnCorrectBalance(
            decimal withdrawal, decimal currentBalanceValue, decimal expectedNewBalance)
        {
            var onlineWalletRepository = Substitute.For<IOnlineWalletRepository>();
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = 0m,
                BalanceBefore = currentBalanceValue
            };
            onlineWalletRepository.GetLastOnlineWalletEntryAsync().Returns(Task.FromResult(onlineWalletEntry));

            var sut = new OnlineWalletService(onlineWalletRepository);
            var currentBalance = await sut.GetBalanceAsync();

            var newBalance = await sut.WithdrawFundsAsync(new Models.Withdrawal { Amount = withdrawal });

            Assert.That(newBalance.Amount, Is.EqualTo(expectedNewBalance));

            await onlineWalletRepository.Received(1).InsertOnlineWalletEntryAsync(Arg.Is<OnlineWalletEntry>(o =>
                o.Amount == -withdrawal &&
                o.BalanceBefore == currentBalance.Amount &&
                o.EventTime < DateTimeOffset.UtcNow));
        }

        [TestCaseSource(nameof(WithdrawFunds_NegativeCases_BalanceTooLow))]
        public async Task WithdrawFundsAsync_ShouldThrowInsufficientBalanceException_WhenBalanceIsTooLow(
            decimal withdrawal, decimal currentBalanceValue)
        {
            var onlineWalletRepository = Substitute.For<IOnlineWalletRepository>();
            var onlineWalletEntry = new OnlineWalletEntry
            {
                Amount = 0m,
                BalanceBefore = currentBalanceValue
            };
            onlineWalletRepository.GetLastOnlineWalletEntryAsync().Returns(Task.FromResult(onlineWalletEntry));

            var sut = new OnlineWalletService(onlineWalletRepository);
            var currentBalance = await sut.GetBalanceAsync();

            Assert.ThrowsAsync<InsufficientBalanceException>(async () =>
                await sut.WithdrawFundsAsync(new Models.Withdrawal { Amount = withdrawal }),
                "WithdrawFundsAsync() should've thrown InsufficientBalanceException when balance was too low!");

            // Additionally checks that the method didn't produce a new OnlineWalletEntry
            await onlineWalletRepository.DidNotReceiveWithAnyArgs().InsertOnlineWalletEntryAsync(default);
        }

        // IMO, this should fail, as Withdrawal shouldn't accept negative input params
        [TestCaseSource(nameof(WithdrawFunds_NegativeCases_NegativeInput))]
        public async Task WithdrawFundsAsync_ShouldDeclineNegativeWithdrawalAmount(decimal withdrawal)
        {
            var onlineWalletRepository = Substitute.For<IOnlineWalletRepository>();

            var sut = new OnlineWalletService(onlineWalletRepository);

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                await sut.WithdrawFundsAsync(new Models.Withdrawal { Amount = withdrawal }),
                "WithdrawFundsAsync() should've thrown ArgumentOutOfRangeException on negative input data!");

            // Additionally checks that the method didn't produce a new OnlineWalletEntry
            await onlineWalletRepository.DidNotReceiveWithAnyArgs().InsertOnlineWalletEntryAsync(default);
        }
    }
}