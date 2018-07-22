# LINEChannelConnector
LINE Channel Connector for BotBuilder

The original idea and many source codes are from [BotBuilderChannelConnector](https://github.com/vossccp/BotBuilderChannelConnector/) which "Allows to diretly interface channels without using the Microsoft Bot Development portal."

There are several reasons that I couldn't simply add LINE Channel to the repository, thus I created new one.

This is yet early beta version.

## How to use
This channel connector works as part of OWIN pipeline, thus current BotFramework template won't be perfect for you. See LineChannelConnectorTest project for how to use.

```
app.UseLINEChannel(config: new LINEConfig()
    {
        ChannelAccessToken = ConfigurationManager.AppSettin["ChannelAccessToken"].ToString(),
        ChannelSecret = ConfigurationManager.AppSettin["ChannelAccessSecret"].ToString()
    });
```

## How this works.
There are two things this connector does.

**Convert incoming LINE request to BotBuilder Activity**
Obviously, when LINE sends request to the bot, it has LINE format. The connector works as OWIN middleware to convert LINE format to BotBuilder Activity.

**Convert outgoing BotBuilder Activity to LINE reply**
This is implemented by inheriting IConversation interface and register custom classes as part of BotBuilder Conversation. It converts BotBuilder Activity to LINE reply.

**Limitation**
- You need to use either Developer mode or paid mode as it uses push a lot.
- LINE unique capabilities are not imeplemented yet.

## Future plans
There are many future plans if some people actually using this.

- Support LINE specific features, such as URL Schema, LIFF, etc.
- Support Flex Messages.