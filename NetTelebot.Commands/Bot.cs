﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTelebot.Commands
{
    public class Bot
    {
        public Bot(string name, string token)
        {
            Configuration = new BotConfiguration();
            Token = token;
            Client = new TelegramBotClient() { Token = token };
            Data = new BotData(this);
            Name = name;
        }
        public BotConfiguration Configuration { get; private set; }
        public TelegramBotClient Client { get; private set; }
        public string Token { get; private set; }
        public BotData Data { get; private set; }
        public string Name { get; private set; }
        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<MessageEventArgs> UnknownCommandReceived;

        public void Start()
        {
            Client.UpdatesReceived += Client_UpdatesReceived;
            Client.StartCheckingUpdates();
        }

        public void Stop()
        {
            Client.StopCheckUpdates();
            Client.UpdatesReceived -= Client_UpdatesReceived;
        }

        private void Client_UpdatesReceived(object sender, TelegramUpdateEventArgs e)
        {
            foreach (var update in e.Updates)
            {
                OnMessageReceived(update.Message);
                Process(update);
            }
        }

        protected virtual void OnMessageReceived(MessageInfo message)
        {
            if (MessageReceived != null)
                MessageReceived(this, new MessageEventArgs(message));
        }

        private void Process(UpdateInfo update)
        {
            if (update.IsCommand())
            {
                ProcessCommand(update.Message);
            }
            else
            {
                ProcessParameter(update.Message);
            }
        }

        private void ProcessParameter(MessageInfo message)
        {
            var commandState = Data.GetCommandState(message.From.Id, message.Chat.Id);
            if(commandState!=null)
            {
            }
            else
            {
                OnUnknownCommandReceived(message);
            }
        }

        protected virtual void ProcessCommand(MessageInfo message)
        {
            var commandInfo = FindCommand(message.Text);
            if (commandInfo != null)
            {
                Process(message, commandInfo);
            }
            else
            {
                OnUnknownCommandReceived(message);
                if (!string.IsNullOrEmpty(Configuration.StaticUnknownCommandMessage))
                {
                    Client.SendMessage(message.Chat.Id, Configuration.StaticUnknownCommandMessage);
                }
            }
        }

        protected virtual void Process(MessageInfo message, CommandInfo command)
        {
            if (command.Parameters.Any())
            {
                GetFirstParameter(message, command);
            }
            else
            {
                Data.DeleteCommandState(message.From.Id, message.Chat.Id);
                RunRequest(message, command);
            }
        }

        protected virtual void RunRequest(MessageInfo message, CommandInfo command)
        {
            Client.SendMessage(message.Chat.Id, command.StaticAcceptMessage);
        }

        protected virtual void GetFirstParameter(MessageInfo message, CommandInfo command)
        {
            var parameter = command.Parameters.First();
            Data.SetCommand(message.From.Id, message.Chat.Id, command.Text);
            Client.SendMessage(message.Chat.Id, parameter.StaticPrompt);
        }

        protected virtual void OnUnknownCommandReceived(MessageInfo message)
        {
            if (UnknownCommandReceived != null)
                UnknownCommandReceived(this, new MessageEventArgs(message));
        }

        private CommandInfo FindCommand(string text)
        {
            return Configuration.Commands.FirstOrDefault(ci => ci.Text.Equals(text, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
