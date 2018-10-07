using Microsoft.AspNetCore.Builder;

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
        public static IApplicationBuilder UseLINEChannel(this IApplicationBuilder appBuilder, LINEConfig config)
        {
            appBuilder.UseMiddleware<LineMiddleware>(config);
            return appBuilder;
        }
    }    
}
