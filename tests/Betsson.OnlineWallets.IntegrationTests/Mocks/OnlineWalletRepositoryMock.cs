using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;

namespace Betsson.OnlineWallets.IntegrationTests.Mocks
{
    internal class OnlineWalletRepositoryMock : IOnlineWalletRepository
    {
        private readonly List<OnlineWalletEntry> _onlineWalletEntries = new List<OnlineWalletEntry>();

        public OnlineWalletRepositoryMock(OnlineWalletEntry? walletEntry = null)
        {
            if (walletEntry is not null)
            {
                _onlineWalletEntries.Add(walletEntry);
            }
        }
        public Task<OnlineWalletEntry?> GetLastOnlineWalletEntryAsync()
        {
            return Task.FromResult(_onlineWalletEntries.LastOrDefault());
        }

        public Task InsertOnlineWalletEntryAsync(OnlineWalletEntry onlineWalletEntry)
        {
            _onlineWalletEntries.Add(onlineWalletEntry);
            return Task.CompletedTask;
        }
    }
}
