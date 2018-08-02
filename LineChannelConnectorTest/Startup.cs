using Autofac;
using LINEChannelConnector;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.Owin;
using Owin;
using System.Configuration;
using System.Web.Http;

[assembly: OwinStartup(typeof(LineChannelConnectorTest.Startup))]

namespace LineChannelConnectorTest
{
    public class Startup
    {
        private readonly HttpConfiguration configuration = new HttpConfiguration();
        public void Configuration(IAppBuilder app)
        {
            RegisterInMemoryBotStore();
            var lineConfig = new LINEConfig()
            {
                ChannelAccessToken = ConfigurationManager.AppSettings["ChannelAccessToken"].ToString(),
                ChannelSecret = ConfigurationManager.AppSettings["ChannelAccessSecret"].ToString()
            };
#if DEBUG
            lineConfig.Uri = ConfigurationManager.AppSettings["Uri"].ToString();
#endif
            app.UseLINEChannel(config: lineConfig);
            app.UseWebApi(configuration);
            WebApiConfig.Register(configuration);
        }

        static void RegisterInMemoryBotStore()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<InMemoryDataStore>()
                .AsSelf()
                .SingleInstance();

            builder.Register(c => new CachingBotDataStore(c.Resolve<InMemoryDataStore>(), CachingBotDataStoreConsistencyPolicy.ETagBasedConsistency))
                .As<IBotDataStore<BotData>>()
                .AsSelf()
                .InstancePerLifetimeScope();

            builder.Update(Conversation.Container);
        }
    }
}
