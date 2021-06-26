using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Maui
{
    public static class VirtualListViewHostBuilderExtensions
    {
        public static IAppHostBuilder UseVirtualListView(this IAppHostBuilder appHostBuilder)
            => appHostBuilder.ConfigureMauiHandlers(handlers =>
                handlers.AddHandler(typeof(IVirtualListView), typeof(VirtualListViewHandler)));
    }
}
