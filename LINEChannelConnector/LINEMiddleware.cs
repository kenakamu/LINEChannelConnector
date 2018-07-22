using Line.Messaging.Webhooks;
using Microsoft.Owin;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LINEChannelConnector
{
    /// <summary>
    /// OWIN Middleware for LINE Channel.
    /// </summary>
    public class LineMiddleware : OwinMiddleware
    {

        readonly LINEConfig config;

        public LineMiddleware(OwinMiddleware next, LINEConfig config) : base(next)
        {
            this.config = config;
        }

        public async override Task Invoke(IOwinContext context)
        {
            // Only POST requests are expected.
            if (context.Request.Method == "POST")
            {
                if (context.Request.Headers.Keys.Contains("X-Line-Signature"))
                {
                    // Convert LINE messages to Bot Builder Activities.
                    var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Uri);
                    context.Request.Headers.ToList().ForEach(x => request.Headers.TryAddWithoutValidation(x.Key, x.Value));

                    using (Stream receiveStream = context.Request.Body)
                    {
                        using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                        {
                            var result = readStream.ReadToEnd();
                            request.Content = new StringContent(result, Encoding.UTF8, "application/json");
                            var events = await request.GetWebhookEventsAsync(config.ChannelSecret);
                            LINEClient client = new LINEClient(config);
                            var activities = await client.ConvertToActivity(events.ToList());
                            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(activities.First())));
                        }
                    }
                   
                }
            }
            await Next.Invoke(context);
        }
    }    
}
