# LINEChannelConnector

LINE Channel Connector for BotBuilder v4
You can find BotBuilder v3 version in [v3 branch](https://github.com/kenakamu/LINEChannelConnector/tree/v3)

The original idea and many source codes are from [BotBuilderChannelConnector](https://github.com/vossccp/BotBuilderChannelConnector/) which "Allows to diretly interface channels without using the Microsoft Bot Development portal."

There are several reasons that I couldn't simply add LINE Channel to the repository, thus I created new one.

This is yet early beta version.

## How to use
This channel connector works as part of OWIN pipeline. You need to tweak several places but See LineChannelConnectorV4Test project for how to use.

1. Update LINE Channel Secret and Channel Access Token in appsettings.json.

1. Update Startup.cs to include ASP.NET Core middleware in Configure method.

```csharp
public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
{
    _loggerFactory = loggerFactory;

    // UserLINEChannel middleware converts incoming LINE event to BotFramework Activity
    app.UseDefaultFiles()
        .UseLINEChannel(new LINEConfig()
        {
            ChannelSecret = Configuration.GetSection("ChannelSecret")?.Value,
            ChannelAccessToken = Configuration.GetSection("ChannelAccessToken")?.Value,
            Uri = Configuration.GetSection("Uri")?.Value,
        })
        .UseStaticFiles()
        .UseBotFramework();
}
```

1. Update Bot middleware in ConfigureServices method.

```csharp
services.AddBot<EchoWithCounterBot>(options =>
{
    // This bot middleware replace ConnectorClient and use LINEConnectorClient
    options.Middleware.Add(new LINEAdapterMiddleware(new LINEConfig()
    {
        ChannelSecret = Configuration.GetSection("ChannelSecret")?.Value,
        ChannelAccessToken = Configuration.GetSection("ChannelAccessToken.Value,
        Uri = Configuration.GetSection("Uri")?.Value,
    }));
...
```

## How this works.
There are two things this connector does.

**Convert incoming LINE request to BotBuilder Activity**
Obviously, when LINE sends request to the bot, it has LINE format. The connector works as ASP.NET core OWIN middleware to convert LINE format to BotBuilder Activity.

**Convert outgoing BotBuilder Activity to LINE reply**
This is implemented in LINEClient but specify LINEConnectorClient as IConnectorClient in TurnContext if it comes from LINE. See LineAdapterMiddleware.cs for detail.

**Limitation**
- You need to use either Developer mode or paid mode as it uses push a lot.
- LINE unique capabilities are not imeplemented yet.

## Future plans
There are many future plans if some people actually using this.

- Support LINE specific features, such as URL Schema, LIFF, etc.
- Support Flex Messages.