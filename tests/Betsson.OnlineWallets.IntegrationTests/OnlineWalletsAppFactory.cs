using Betsson.OnlineWallets.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Betsson.OnlineWallets.IntegrationTests
{
    namespace Betsson.OnlineWallets.Web.ApiTests
    {
        public class OnlineWalletsAppFactory : WebApplicationFactory<Program>
        {
            private readonly Action<IServiceCollection> _configureServices;

            public OnlineWalletsAppFactory(Action<IServiceCollection> configureServices)
            {
                _configureServices = configureServices;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.ConfigureServices(_configureServices);
                base.ConfigureWebHost(builder);
            }
        }
    }
}
