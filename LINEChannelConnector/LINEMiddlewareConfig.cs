using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Owin;
using System;

namespace LINEChannelConnector
{
    /// <summary>
    /// Configuration Class to user use LINE Channel as part of OWIN pipeline
    /// </summary>
    public static class LINEMiddlewareConfig 
    {
        /// <summary>
        /// Add UseLINEChannel method to IAppBuilder
        /// </summary>
        /// <param name="appBuilder"></param>
        /// <param name="config">LINE Configuration</param>
        /// <returns></returns>
        public static IAppBuilder UseLINEChannel(this IAppBuilder appBuilder, LINEConfig config)
        {
            Configure(config);
            appBuilder.Use<LineMiddleware>(config);
            return appBuilder;
        }

        /// <summary>
        /// Configure Autofac to resolve custom objects.
        /// </summary>
        /// <param name="config">LINE Configuration</param>
        public static void Configure(LINEConfig config)
        {
            var builder = new ContainerBuilder();
            builder.Register(c => config)
                .SingleInstance();

            builder
                .Register(c =>
                {
                    var lineConfig = c.Resolve<LINEConfig>();

                    if (lineConfig == null)
                    {
                        string msg = $"No LINE Configuration";
                        throw new Exception(msg);
                    }

                    return new LINEConnectorClientFactory(lineConfig);
                })
                .As<IConnectorClientFactory>()
                .InstancePerLifetimeScope();

            builder.Update(Conversation.Container);
        }
    }    
}
