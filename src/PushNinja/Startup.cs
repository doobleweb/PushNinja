using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PushNinja.Startup))]
namespace PushNinja
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
