using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using System.Threading;
using System.Threading.Tasks;

namespace LINEChannelConnector
{
    public class LINEAdapterMiddleware : IMiddleware
    {
        private LINEConfig config;
        public LINEAdapterMiddleware(LINEConfig config)
        {
            this.config = config;
        }
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            // If the channel is LINE, then replace ConnectorClient 
            if (turnContext.Activity.ServiceUrl.Contains("https://line"))
            {
                if (turnContext.TurnState["Microsoft.Bot.Connector.IConnectorClient"] is ConnectorClient)
                {
                    turnContext.TurnState.Remove("Microsoft.Bot.Connector.IConnectorClient");
                    turnContext.TurnState.Add("Microsoft.Bot.Connector.IConnectorClient",
                        new LINEConnectorClient(config)
                    );
                }
            }
            
            await next.Invoke(cancellationToken);
        }
    }
}
