using Line.Messaging;
using Line.Messaging.Webhooks;
using Microsoft.Bot.Connector;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace LINEChannelConnector
{
    /// <summary>
    /// Handles LINE related activities.
    /// </summary>
    public class LINEClient
    {
        private LINEConfig config;
        private LineMessagingClient messagingClient;

        public LINEClient(LINEConfig config)
        {
            this.config = config;
            messagingClient =
                new LineMessagingClient(
                    config.ChannelAccessToken,
                    config.Uri
                );
        }

        /// <summary>
        /// Convert Bot Connector activity to LINE message and send.
        /// </summary>
        /// <param name="activity">Activity</param>
        /// <returns></returns>
        public async Task<HttpOperationResponse<ResourceResponse>> SendAsync(Activity activity)
        {
            var result = ConvertToLineMessages(activity);
            var userId = result.userId;
            var userParams = CacheService.caches[userId] as Dictionary<string, object>;
            var replyToken = userParams["ReplyToken"].ToString();
            try
            {
                await SendAsync(result.messages, userId, replyToken);

                HttpOperationResponse<ResourceResponse> response = new HttpOperationResponse<ResourceResponse>();
                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Send LINE messages to LINE Channel. If reply fails, try push instead.
        /// </summary>
        /// <param name="messages">LINE messages</param>
        /// <param name="replyToken">ReplyToken</param>
        /// <param name="userId">UserId</param>
        /// <returns></returns>
        public async Task SendAsync(List<ISendMessage> messages, string userId, string replyToken)
        {
            try
            {
                // If messages contain more than 5 items, then do reply for first 5, then push the rest.
                for (int i = 0; i < (double)messages.Count / 5; i++)
                {
                    if (i == 0)
                        await messagingClient.ReplyMessageAsync(replyToken, messages.Take(5).ToList());
                    else
                        await messagingClient.PushMessageAsync(replyToken, messages.Skip(i * 5).Take(5).ToList());
                }
            }
            catch (LineResponseException ex)
            {
                if (ex.Message == "Invalid reply token")
                    try
                    {
                        for (int i = 0; i < (double)messages.Count / 5; i++)
                        {
                            await messagingClient.PushMessageAsync(userId, messages.Skip(i * 5).Take(5).ToList());
                        }
                    }
                    catch (LineResponseException innerEx)
                    {
                        await messagingClient.PushMessageAsync(userId, innerEx.Message);
                    }
            }
            catch (Exception ex)
            {
                await messagingClient.PushMessageAsync(userId, ex.Message);
            }
        }

        /// <summary>
        /// Convert Bot Connector Activity to LINE Messages.
        /// </summary>
        /// <param name="activity">Activity</param>
        /// <returns>User ID and LINE Messages</returns>
        public (string userId, List<ISendMessage> messages) ConvertToLineMessages(Activity activity)
        {
            List<ISendMessage> messages = new List<ISendMessage>();

            if (activity.Attachments != null && activity.Attachments.Count != 0 && (activity.AttachmentLayout == null || activity.AttachmentLayout == "list"))
            {
                foreach (var attachment in activity.Attachments)
                {
                    if (attachment.ContentType.Contains("card.animation"))
                    {
                        // https://docs.botframework.com/en-us/core-concepts/reference/#animationcard
                        // Use TextMessage for title and use Image message for image. Not really an animation though.
                        AnimationCard card = attachment.Content as AnimationCard;
                        messages.Add(new TextMessage($"{card.Title}\r\n{card.Subtitle}\r\n{card.Text}"));
                        foreach (var media in card.Media)
                        {
                            var originalContentUrl = media.Url;
                            var previewImageUrl = card.Image?.Url;
                            messages.Add(new ImageMessage(originalContentUrl, previewImageUrl));
                        }
                    }
                    else if (attachment.ContentType.Contains("card.audio"))
                    {
                        // https://docs.botframework.com/en-us/core-concepts/reference/#audiocard
                        // Use TextMessage for title and use Audio message for image.
                        AudioCard card = attachment.Content as AudioCard;
                        messages.Add(new TextMessage($"{card.Title}\r\n{card.Subtitle}\r\n{card.Text}"));

                        foreach (var media in card.Media)
                        {
                            var originalContentUrl = media.Url;
                            var durationInMilliseconds = 1;

                            messages.Add(new AudioMessage(originalContentUrl, (long)durationInMilliseconds));
                        }
                    }
                    else if (attachment.ContentType.Contains("card.hero") || attachment.ContentType.Contains("card.thumbnail"))
                    {
                        // https://docs.botframework.com/en-us/core-concepts/reference/#herocard
                        // https://docs.botframework.com/en-us/core-concepts/reference/#thumbnailcard
                        HeroCard hcard = null;

                        if (attachment.ContentType.Contains("card.hero"))
                            hcard = attachment.Content as HeroCard;
                        else if (attachment.ContentType.Contains("card.thumbnail"))
                        {
                            ThumbnailCard tCard = attachment.Content as ThumbnailCard;
                            hcard = new HeroCard(tCard.Title, tCard.Subtitle, tCard.Text, tCard.Images, tCard.Buttons, null);
                        }

                        if ((double)hcard.Buttons.Count == 2)
                        {
                            ConfirmTemplate confirmTemplate = new ConfirmTemplate(
                                string.IsNullOrEmpty(hcard.Title) ? hcard.Text : hcard.Title);
                            foreach (var button in hcard.Buttons)
                            {
                                confirmTemplate.Actions.Add(GetAction(button));
                            }

                            messages.Add(new TemplateMessage("Confirm template", confirmTemplate));
                        }
                        else
                        {
                            // Get four buttons per template.
                            for (int i = 0; i < (double)hcard.Buttons.Count / 4; i++)
                            {
                                ButtonsTemplate buttonsTemplate = new ButtonsTemplate(
                                title: string.IsNullOrEmpty(hcard.Title) ? hcard.Text : hcard.Title,
                                thumbnailImageUrl: hcard.Images?.FirstOrDefault()?.Url,
                                text: hcard.Subtitle ?? hcard.Text
                                );

                                if (hcard.Buttons != null)
                                {
                                    foreach (var button in hcard.Buttons.Skip(i * 4).Take(4))
                                    {
                                        buttonsTemplate.Actions.Add(GetAction(button));
                                    }
                                }
                                else
                                {
                                    // Action is mandatory, so create from title/subtitle.
                                    var actionLabel = hcard.Title?.Length < hcard.Subtitle?.Length ? hcard.Title : hcard.Subtitle;
                                    actionLabel = actionLabel.Substring(0, Math.Min(actionLabel.Length, 20));
                                    buttonsTemplate.Actions.Add(new PostbackTemplateAction(actionLabel, actionLabel, actionLabel));
                                }

                                messages.Add(new TemplateMessage("Buttons template", buttonsTemplate));
                            }
                        }
                    }
                    else if (attachment.ContentType.Contains("receipt"))
                    {
                        // https://docs.botframework.com/en-us/core-concepts/reference/#receiptcard
                        // Use TextMessage and Buttons. As LINE doesn't support thumbnail type yet.

                        ReceiptCard card = attachment.Content as ReceiptCard;
                        var text = card.Title + "\r\n\r\n";
                        foreach (var fact in card.Facts)
                        {
                            text += $"{fact.Key}:{fact.Value}\r\n";
                        }
                        text += "\r\n";
                        foreach (var item in card.Items)
                        {
                            text += $"{item.Title}\r\nprice:{item.Price},quantity:{item.Quantity}";
                        }

                        messages.Add(new TextMessage(text));

                        ButtonsTemplate buttonsTemplate = new ButtonsTemplate(
                            text: $"total:{card.Total}", title: $"tax:{card.Tax}");

                        foreach (var button in card.Buttons)
                        {
                            buttonsTemplate.Actions.Add(GetAction(button));
                        }

                        messages.Add(new TemplateMessage("Buttons template", buttonsTemplate));
                    }
                    else if (attachment.ContentType.Contains("card.signin"))
                    {
                        // https://docs.botframework.com/en-us/core-concepts/reference/#signincard
                        // Line doesn't support auth button yet, so simply represent link.
                        SigninCard card = attachment.Content as SigninCard;

                        ButtonsTemplate buttonsTemplate = new ButtonsTemplate(text: card.Text);

                        foreach (var button in card.Buttons)
                        {
                            buttonsTemplate.Actions.Add(GetAction(button));
                        }

                        messages.Add(new TemplateMessage("Buttons template", buttonsTemplate));
                    }
                    else if (attachment.ContentType.Contains("card.video"))
                    {
                        // https://docs.botframework.com/en-us/core-concepts/reference/#videocard
                        // Use Video message for video and buttons for action.

                        VideoCard card = attachment.Content as VideoCard;

                        foreach (var media in card.Media)
                        {
                            var originalContentUrl = media?.Url;
                            var previewImageUrl = card.Image?.Url;

                            messages.Add(new VideoMessage(originalContentUrl, previewImageUrl));
                        }

                        ButtonsTemplate buttonsTemplate = new ButtonsTemplate(
                            title: card.Title, text: $"{card.Subtitle}\r\n{card.Text}");
                        foreach (var button in card.Buttons)

                        {
                            buttonsTemplate.Actions.Add(GetAction(button));
                        }
                        messages.Add(new TemplateMessage("Buttons template", buttonsTemplate));
                    }
                    else if (attachment.ContentType.Contains("image"))
                    {
                        var originalContentUrl = attachment.ContentUrl;
                        var previewImageUrl = string.IsNullOrEmpty(attachment.ThumbnailUrl) ? attachment.ContentUrl : attachment.ThumbnailUrl;

                        messages.Add(new ImageMessage(originalContentUrl, previewImageUrl));
                    }
                    else if (attachment.ContentType.Contains("audio"))
                    {
                        var originalContentUrl = attachment.ContentUrl;
                        var durationInMilliseconds = 0;

                        messages.Add(new AudioMessage(originalContentUrl, durationInMilliseconds));
                    }
                    else if (attachment.ContentType.Contains("video"))
                    {
                        var originalContentUrl = attachment.ContentUrl;
                        var previewImageUrl = attachment.ThumbnailUrl;

                        messages.Add(new VideoMessage(originalContentUrl, previewImageUrl));
                    }
                }
            }
            else if (activity.Attachments != null && activity.Attachments.Count != 0 && activity.AttachmentLayout != null)
            {
                CarouselTemplate carouselTemplate = new CarouselTemplate();

                foreach (var attachment in activity.Attachments)
                {
                    HeroCard hcard = null;

                    if (attachment.ContentType == "application/vnd.microsoft.card.hero")
                        hcard = attachment.Content as HeroCard;
                    else if (attachment.ContentType == "application/vnd.microsoft.card.thumbnail")
                    {
                        ThumbnailCard tCard = attachment.Content as ThumbnailCard;
                        hcard = new HeroCard(tCard.Title, tCard.Subtitle, tCard.Text, tCard.Images, tCard.Buttons, null);
                    }
                    else
                        continue;

                    CarouselColumn tColumn = new CarouselColumn(
                        title: hcard.Title,
                        thumbnailImageUrl: hcard.Images.FirstOrDefault()?.Url,
                        text: string.IsNullOrEmpty(hcard.Subtitle) ?
                            hcard.Text : hcard.Subtitle);

                    if (hcard.Buttons != null)
                    {
                        foreach (var button in hcard.Buttons.Take(3))
                        {
                            tColumn.Actions.Add(GetAction(button));
                        }
                    }
                    else
                    {
                        // Action is mandatory, so create from title/subtitle.
                        var actionLabel = hcard.Title?.Length < hcard.Subtitle?.Length ? hcard.Title : hcard.Subtitle;
                        actionLabel = actionLabel.Substring(0, Math.Min(actionLabel.Length, 20));
                        tColumn.Actions.Add(new PostbackTemplateAction(actionLabel, actionLabel, actionLabel));
                    }

                    carouselTemplate.Columns.Add(tColumn);
                }

                messages.Add(new TemplateMessage("Carousel template", carouselTemplate));
            }
            else if (activity.Entities != null && activity.Entities.Count != 0)
            {
                foreach (var entity in activity.Entities)
                {
                    switch (entity.Type)
                    {
                        case "Place":
                            Place place = entity.Properties.ToObject<Place>();
                            GeoCoordinates geo = JsonConvert.DeserializeObject<GeoCoordinates>(place.Geo.ToString());
                            messages.Add(new LocationMessage(place.Name, place.Address.ToString(), (decimal)geo.Latitude, (decimal)geo.Longitude));
                            break;
                        case "GeoCoordinates":
                            GeoCoordinates geoCoordinates = entity.Properties.ToObject<GeoCoordinates>();
                            messages.Add(new LocationMessage(activity.Text, geoCoordinates.Name, (decimal)geoCoordinates.Latitude, (decimal)geoCoordinates.Longitude));
                            break;
                    }
                }
            }
            else if (activity.ChannelData != null)
            {
                var stickerids = Enumerable.Range(1, 17)
                .Concat(Enumerable.Range(21, 1))
                .Concat(Enumerable.Range(100, 139 - 100 + 1))
                .Concat(Enumerable.Range(401, 430 - 400 + 1)).ToArray();

                var rand = new Random(Guid.NewGuid().GetHashCode());
                var stickerId = stickerids[rand.Next(stickerids.Length - 1)].ToString();

                messages.Add(new StickerMessage("1", stickerId));
            }
            else if (!string.IsNullOrEmpty(activity.Text))
            {
                if (activity.Text.Contains("\n\n*"))
                {
                    var lines = activity.Text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                    ButtonsTemplate buttonsTemplate = new ButtonsTemplate(text: lines[0]);

                    foreach (var line in lines.Skip(1))
                    {
                        buttonsTemplate.Actions.Add(new PostbackTemplateAction(line, line.Replace("* ", ""), line.Replace("* ", "")));
                    }

                    messages.Add(new TemplateMessage("Buttons template", buttonsTemplate));
                }
                else if (activity.Text.Contains("location"))
                {
                    ButtonsTemplate buttonsTemplate = new ButtonsTemplate(
                           text: activity.Text);

                    buttonsTemplate.Actions.Add(new UriTemplateAction("Send Location", "line://nv/location"));

                    messages.Add(new TemplateMessage("Buttons template", buttonsTemplate));
                }
                else
                    messages.Add(new TextMessage(activity.Text));
            }

            return (activity.ReplyToId, messages);
        }

        /// <summary>
        /// Create TemplateAction from CardAction.
        /// </summary>
        /// <param name="button">CardAction</param>
        /// <returns>TemplateAction</returns>
        private ITemplateAction GetAction(CardAction button)
        {
            switch (button.Type)
            {
                case "openUrl":
                case "playAudio":
                case "playVideo":
                case "showImage":
                case "signin":
                case "downloadFile":
                    return new UriTemplateAction(button.Title.Substring(0, Math.Min(button.Title.Length, 20)), button.Value.ToString());
                case "imBack":
                    return new MessageTemplateAction(button.Title.Substring(0, Math.Min(button.Title.Length, 20)), button.Value.ToString());
                case "postBack":
                    return new PostbackTemplateAction(button.Title.Substring(0, Math.Min(button.Title.Length, 20)), button.Value.ToString(), button.Value.ToString());
                default:
                    return null;
            }
        }

        /// <summary>
        /// Convert LINE messages to Bot Connector Activity
        /// </summary>
        /// <param name="events">LINE Messages</param>
        /// <returns>Activities</returns>
        public async Task<List<IMessageActivity>> ConvertToActivity(List<WebhookEvent> events)
        {
            List<IMessageActivity> activities = new List<IMessageActivity>();
            foreach (var ev in events)
            {
                var replyToken = JObject.Parse(JsonConvert.SerializeObject(ev))["replyToken"];
                Initialize(ev, replyToken.ToString());
                switch (ev.Type)
                {
                    case WebhookEventType.Message:
                        var messageEv = (MessageEvent)ev;
                        // Handle type by type
                        switch (messageEv.Message.Type)
                        {
                            case EventMessageType.Text:
                                var activity = HandleText(((TextEventMessage)messageEv.Message).Text, ev.Source.UserId);
                                activities.Add(activity);
                                break;
                            case EventMessageType.Sticker:
                                var stickerActivity = HandleSticker((StickerEventMessage)messageEv.Message, ev.Source.UserId);
                                activities.Add(stickerActivity);
                                break;
                            case EventMessageType.Location:
                                var locationActivity = HandleLocation((LocationEventMessage)messageEv.Message, ev.Source.UserId);
                                activities.Add(locationActivity);
                                break;
                            case EventMessageType.Video:
                            case EventMessageType.Audio:
                            case EventMessageType.Image:
                                var mediaActivity = await HandleMediaAsync(messageEv.Message.Id, ev.Source.UserId);
                                activities.Add(mediaActivity);
                                break;
                        }
                        break;
                    case WebhookEventType.Beacon:
                        var beaconActivity = HandleBeacon(((BeaconEvent)ev), ev.Source.UserId);
                        activities.Add(beaconActivity);
                        break;
                    case WebhookEventType.Follow:
                        var followActivity = HandleFollow(((FollowEvent)ev), ev.Source.UserId);
                        activities.Add(followActivity);
                        break;
                    case WebhookEventType.Unfollow:
                        var unfollowActivity = HandleUnfollow(((UnfollowEvent)ev), ev.Source.UserId);
                        activities.Add(unfollowActivity);
                        break;
                    case WebhookEventType.Join:
                        var joinActivity = HandleJoin(((JoinEvent)ev), ev.Source.UserId);
                        activities.Add(joinActivity);
                        break;
                    case WebhookEventType.Leave:
                        var leaveActivity = HandleLeave(((LeaveEvent)ev), ev.Source.UserId);
                        activities.Add(leaveActivity);
                        break;
                }
            }

            return activities;
        }

        /// <summary>
        /// Save User related information.
        /// </summary>
        /// <param name="ev"></param>
        private void Initialize(WebhookEvent ev, string replyToken)
        {
            var lineId = ev.Source.Id;

            if (CacheService.caches.Keys.Contains(lineId))
            {
                var userParams = CacheService.caches[lineId] as Dictionary<string, object>;
                userParams["ReplyToken"] = replyToken;
            }
            else
            {
                // If no cache, then create new one.
                var userParams = new Dictionary<string, object>();
                userParams["ReplyToken"] = replyToken;
                CacheService.caches[lineId] = userParams;
            }
        }

        #region Handlers


        #endregion

        /// <summary>
        /// Handles beacon event
        /// </summary>
        /// <param name="ev">BeaconEvent</param>
        /// <param name="userId">UserId</param>
        /// <returns>IMessageActivity</returns>
        private IMessageActivity HandleBeacon(BeaconEvent ev, string userId)
        {
            var activity = GetActivityBase(userId);
            activity.ChannelData = ev;
            activity.Text = "beacon";
            return activity;
        }


        /// <summary>
        /// Handles Follow event
        /// </summary>
        /// <param name="ev">FollowEvent</param>
        /// <param name="userId">UserId</param>
        /// <returns>IMessageActivity</returns>
        private IMessageActivity HandleFollow(FollowEvent ev, string userId)
        {
            var activity = GetActivityBase(userId);
            activity.ChannelData = ev;
            activity.Text = "follow";
            return activity;
        }

        /// <summary>
        /// Handles Unfollow event
        /// </summary>
        /// <param name="ev">UnfollowEvent</param>
        /// <param name="userId">UserId</param>
        /// <returns>IMessageActivity</returns>
        private IMessageActivity HandleUnfollow(UnfollowEvent ev, string userId)
        {
            var activity = GetActivityBase(userId);
            activity.ChannelData = ev;
            activity.Text = "unfollow";
            return activity;
        }

        /// <summary>
        /// Handles Join event
        /// </summary>
        /// <param name="ev">JoinEvent</param>
        /// <param name="userId">UserId</param>
        /// <returns>IMessageActivity</returns>
        private IMessageActivity HandleJoin(JoinEvent ev, string userId)
        {
            var activity = GetActivityBase(userId);
            activity.ChannelData = ev;
            activity.Text = "join";
            return activity;
        }

        /// <summary>
        /// Handles Leave event
        /// </summary>
        /// <param name="ev">LeaveEvent</param>
        /// <param name="userId">UserId</param>
        /// <returns>IMessageActivity</returns>
        private IMessageActivity HandleLeave(LeaveEvent ev, string userId)
        {
            var activity = GetActivityBase(userId);
            activity.ChannelData = ev;
            activity.Text = "leave";
            return activity;
        }

        /// <summary>
        /// Convert Text type
        /// </summary>
        /// <param name="replyToken"></param>
        /// <param name="userMessage"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private IMessageActivity HandleText(string userMessage, string userId)
        {
            var activity = GetActivityBase(userId);
            activity.Text = userMessage;
            return activity;
        }

        /// <summary>
        /// Save the received data to local and returns the path
        /// </summary>
        private async Task<IMessageActivity> HandleMediaAsync(string messageId, string userId)
        {
            var stream = await messagingClient.GetContentStreamAsync(messageId);
            var ext = GetFileExtension(stream.ContentHeaders.ContentType.MediaType);

            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            var path = HttpContext.Current.Server.MapPath("~/Temp/");
            FileStream file = new FileStream($"{path}{messageId}.{ext}", FileMode.Create, FileAccess.ReadWrite);
            ms.WriteTo(file);
            file.Close();
            var serverUrl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port;

            var activity = GetActivityBase(userId);
            activity.Attachments = new List<Attachment>();
            activity.Attachments.Add(new Attachment()
            {
                ContentUrl = $"{serverUrl}/Temp/{messageId}.{ext}",
                ContentType = stream.ContentHeaders.ContentType.MediaType
            });

            return activity;
        }

        /// <summary>
        /// Reply the location user send.
        /// </summary>
        private IMessageActivity HandleLocation(LocationEventMessage location, string userId)
        {
            var activity = GetActivityBase(userId);
            var entity = new Entity()
            {
                Type = "Place",
                Properties = JObject.FromObject(new Place(address: location.Address,
                                geo: new GeoCoordinates(
                                    latitude: (double)location.Latitude,
                                    longitude: (double)location.Longitude,
                                    name: location.Title),
                                name: location.Title))
            };
            activity.Text = location.Title;
            activity.Entities = new List<Entity>() { entity };

            return activity;
        }

        /// <summary>
        /// Replies random sticker
        /// Sticker ID of bssic stickers (packge ID =1)
        /// see https://devdocs.line.me/files/sticker_list.pdf
        /// </summary>
        private IMessageActivity HandleSticker(StickerEventMessage sticker, string userId)
        {
            var activity = GetActivityBase(userId);

            activity.ChannelData = sticker;
            activity.Text = sticker.ToString();
            return activity;
        }

        /// <summary>
        /// Create an Activity object
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <returns>Activity</returns>
        private Activity GetActivityBase(string userId)
        {
            Activity activity = new Activity()
            {
                Type = "message",
                Id = userId,
                Timestamp = DateTime.UtcNow,
                Conversation = new ConversationAccount()
                {
                    IsGroup = false,
                    Id = userId
                },
                ServiceUrl = "https://line.com",
                ChannelId = "LINE",
                From = new ChannelAccount(userId, userId),
                Recipient = new ChannelAccount("Bot", "Bot")
            };

            return activity;
        }

        private string GetFileExtension(string mediaType)
        {
            switch (mediaType)
            {
                case "image/jpeg":
                    return ".jpeg";
                case "audio/x-m4a":
                    return ".m4a";
                case "video/mp4":
                    return ".mp4";
                default:
                    return "";
            }
        }
    }
}