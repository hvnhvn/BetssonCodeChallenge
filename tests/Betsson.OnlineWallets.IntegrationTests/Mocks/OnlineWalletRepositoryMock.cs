using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Betsson.OnlineWallets.IntegrationTests.Mocks
{
    internal class OnlineWalletRepositoryMock : IOnlineWalletRepository
    {
        private readonly List<OnlineWalletEntry> _entries = new List<OnlineWalletEntry>();

        public OnlineWalletRepositoryMock(OnlineWalletEntry? walletEntry = null)
        {
            if (walletEntry is not null)
            {
                _entries.Add(walletEntry);
            }
        }
        public Task<OnlineWalletEntry?> GetLastOnlineWalletEntryAsync()
        {
            return Task.FromResult(_entries.LastOrDefault());
        }

        public Task InsertOnlineWalletEntryAsync(OnlineWalletEntry onlineWalletEntry)
        {
            _entries.Add(onlineWalletEntry);
            return Task.CompletedTask;
        }
    }
}
