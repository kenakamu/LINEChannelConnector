using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace LINEChannelConnector
{
    public class LINEConnectorClient : IConnectorClient
    {
        public LINEConnectorClient(LINEConfig config)
        {
            Conversations = new LINEConversation(config);
        }

        public Uri BaseUri
        {
            get { return new Uri("https://line.me"); }
            set { throw new NotImplementedException(); }
        }

        public JsonSerializerSettings SerializationSettings
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public JsonSerializerSettings DeserializationSettings
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public ServiceClientCredentials Credentials
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IAttachments Attachments
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IConversations Conversations { get; }

        public void Dispose()
        {
        }
    }
}