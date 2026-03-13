using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;

namespace Betsson.OnlineWallets.IntegrationTests
{
    namespace Betsson.OnlineWallets.Web.ApiTests
    {
        public class OnlineWalletsAppFactory : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                var mock = Substitute.For<IOnlineWalletRepository>();
                var onlineWalletEntry = new OnlineWalletEntry { Amount = 0m, BalanceBefore = 10m };
                mock.GetLastOnlineWalletEntryAsync().Returns(onlineWalletEntry);

                builder.ConfigureServices(services =>
                {
                    services.AddScoped<IOnlineWalletRepository>(p => mock);
                });
                base.ConfigureWebHost(builder);
            }
        }
    }

}
