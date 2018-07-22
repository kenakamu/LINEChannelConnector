using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace LineChannelConnectorTest.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            if (activity.ChannelData != null) 
            {
                var reply = activity.CreateReply("Yo");
                reply.ChannelData = activity.ChannelData;
                await context.PostAsync(reply);
            }
            else if (activity.Entities.Count != 0)
            {
                var reply = activity.CreateReply();
                reply.Entities = activity.Entities;
                await context.PostAsync(reply);
            }
            else if(activity.Attachments.Count!=0)
            {
                var reply = activity.CreateReply();
                reply.Attachments = activity.Attachments;
                await context.PostAsync(reply);
            }
            else{
                // calculate something for us to return
                int length = (activity.Text ?? string.Empty).Length;

                // return our reply to the user
                await context.PostAsync($"You sent {activity.Text} which was {length} characters");
                PromptDialog.Text(context, Complete, "What's yourname?");
            }
        }

        private async Task Complete(IDialogContext context, IAwaitable<string> result)
        {
            var name = await result;
            await context.PostAsync($"Welcome {name}!!");

            context.Wait(MessageReceivedAsync);
        }
    }
}