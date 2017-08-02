using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;

namespace LuisBot.Dialogs
{
    using Microsoft.Bot.Builder.Dialogs;
    using System;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector;

    [Serializable]
    public class PayDialog : IDialog<int>
    {
        private string name;
        private int attempts = 3;

        public PayDialog(string name)
        {
            this.name = name;
        }

        public async Task StartAsync(IDialogContext context)
        {
            await context.SayAsync($"How much did { this.name } pay?");

            context.Wait(this.MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            int pay;

            if (Int32.TryParse(message.Text, out pay) && (pay > 0))
            {
                context.Done(pay);
            }
            else
            {
                --attempts;
                if (attempts > 0)
                {
                    await context.SayAsync("I'm sorry, I don't understand your reply. How much did you pay (e.g. '100')?");

                    context.Wait(this.MessageReceivedAsync);
                }
                else
                {
                    context.Fail(new TooManyAttemptsException("Message was not a valid quantity."));
                }
            }
        }
    }
}