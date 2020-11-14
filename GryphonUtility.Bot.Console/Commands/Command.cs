﻿using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GryphonUtility.Bot.Console.Commands
{
    internal abstract class Command
    {
        protected abstract string Name { get; }

        internal bool Contains(Message message) => (message.Type == MessageType.Text) && message.Text.Contains(Name);

        internal abstract Task ExecuteAsync(ChatId chatId, ITelegramBotClient client);
    }
}
