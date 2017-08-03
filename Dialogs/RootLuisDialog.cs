namespace LuisBot.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.FormFlow;
    using Microsoft.Bot.Builder.Luis;
    using Microsoft.Bot.Builder.Luis.Models;
    using Microsoft.Bot.Connector;

    [LuisModel("1a8f1b9d-2326-4df5-b346-098ebd29ebbd", "c99516f757a94f6882b5485bba38e524", domain: "westus.api.cognitive.microsoft.com", staging: false)]
    [Serializable]
    public class RootLuisDialog : LuisDialog<object>
    {
        private const string EntityMoney = "builtin.money";
        private const string EntityNumber = "builtin.number";
        private const string EntityFriend = "Friend";

        private Dictionary<string, Double> amountPaid = new Dictionary<string, Double>();
        private Dictionary<string, int> participants = new Dictionary<string, int>();

        private Double billTotal = 0.0;
        private int numberShares = 0;

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = $"Sorry, I did not understand '{result.Query}'. Type 'help' if you need assistance.";

            await context.SayAsync(message, message);

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("SplitBill")]
        public async Task SplitBill(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;
            await context.SayAsync($"Welcome to Split It! We are analyzing your message: '{message.Text}'...", $"Welcome to Split It! We are analyzing your message: '{message.Text}'...");
            await context.SayAsync($"Who paid the bill and what was the amount?", $"Who paid the bill and what was the amount?");
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("AddParticipant")]
        public async Task AddParticipant(IDialogContext context, LuisResult result)
        {
            EntityRecommendation friendEntityRecommendation;
            EntityRecommendation numberEntityRecommendation;

            if (result.TryFindEntity(EntityFriend, out friendEntityRecommendation))
            {
                if (result.TryFindEntity(EntityNumber, out numberEntityRecommendation))
                {
                    participants.Add(friendEntityRecommendation.Entity, Int32.Parse(numberEntityRecommendation.Entity));
                    numberShares += Int32.Parse(numberEntityRecommendation.Entity);
                    await context.SayAsync($"You added '{friendEntityRecommendation.Entity}' who owes '{numberEntityRecommendation.Entity}' shares", $"You added '{friendEntityRecommendation.Entity}' who owes '{numberEntityRecommendation.Entity}' shares");
                }
                else
                {
                    participants.Add(friendEntityRecommendation.Entity, 1);
                    numberShares++;
                    await context.SayAsync($"You added '{friendEntityRecommendation.Entity}'", $"You added '{friendEntityRecommendation.Entity}'");
                }
            }

            await context.SayAsync($"Do you want to add more participants or shall I finalize the expense?", $"Do you want to add more participants or shall I finalize the expense?");
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("AddSpender")]
        public async Task AddSpender(IDialogContext context, LuisResult result)
        {
            EntityRecommendation friendEntityRecommendation;
            EntityRecommendation moneyEntityRecommendation;

            if (result.TryFindEntity(EntityFriend, out friendEntityRecommendation))
            {
                if (result.TryFindEntity(EntityNumber, out moneyEntityRecommendation))
                {
                    amountPaid.Add(friendEntityRecommendation.Entity, Double.Parse(moneyEntityRecommendation.Entity));
                    billTotal += Double.Parse(moneyEntityRecommendation.Entity);

                    participants.Add(friendEntityRecommendation.Entity, 1);
                    numberShares++;
                    await context.SayAsync($"You added '{friendEntityRecommendation.Entity}' with amount '{moneyEntityRecommendation.Entity}'", $"You added '{friendEntityRecommendation.Entity}' with amount '{moneyEntityRecommendation.Entity}'");
                }
            }

            await context.SayAsync($"Did anyone else contribute? If so, how much did he or she pay?", $"Did anyone else contribute? If so, how much did he or she pay?");
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("EndList")]
        public async Task EndList(IDialogContext context, LuisResult result)
        {
            await context.SayAsync($"Do you want to add someone to the expense?", $"Do you want to add someone to the expense?");
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("CalculateBill")]
        public async Task CalculateBill(IDialogContext context, LuisResult result)
        {
            await context.SayAsync($"Finalizing expenses", $"Finalizing expenses");

            // Calculate how much is owed per person
            Double amtPerShare = Math.Round(billTotal / numberShares, 2);

            await context.SayAsync($"Everyone owes '{amtPerShare}'", $"Everyone owes '{amtPerShare}'");

            var resultMessage = context.MakeMessage();
            resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            resultMessage.Attachments = new List<Attachment>();

            foreach (String participant in participants.Keys)
            {
                ThumbnailCard thumbnailCard = new ThumbnailCard()
                {
                    Title = participant,
                    Text = "$"+amtPerShare*participants[participant],
                    Images = new List<CardImage>()
                        {
                            new CardImage() { Url = "https://www.spreadshirt.com/image-server/v1/designs/10267597,width=178,height=178/white-dollar-sign.png" }
                        },
                };

                resultMessage.Attachments.Add(thumbnailCard.ToAttachment());
            }

            await context.PostAsync(resultMessage);

            amountPaid = new Dictionary<string, double>();
            participants = new Dictionary<string, int>();

            billTotal = 0.0;
            numberShares = 0;

            context.Done<object>(null);
        }

    }
}
