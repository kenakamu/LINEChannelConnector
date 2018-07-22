using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using System;

namespace LINEChannelConnector
{
    public class LINEConnectorClientFactory : IConnectorClientFactory
    {
        readonly LINEConfig config;

        public LINEConnectorClientFactory(LINEConfig config)
        {
            this.config = config;
        }

        public IConnectorClient MakeConnectorClient()
        {
            return new LINEConnectorClient(config);
        }

        public IOAuthClient MakeOAuthClient()
        {
            throw new NotImplementedException();
        }

        public IStateClient MakeStateClient()
        {
            throw new NotImplementedException();
        }
    }
}