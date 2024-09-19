using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using TumeraAI.Main.API;
using TumeraAI.Main.Types;
using TumeraAI.Main.Utils;
using Windows.Media.Protection.PlayReady;
using Windows.System;

#pragma warning disable OPENAI001
namespace TumeraAI.Pages
{
    public sealed partial class ChatPage : Page
    {
        public ObservableCollection<ChatSession> Sessions { get; set; }
        public ObservableCollection<Model> Models { get; set; }
        public ChatPage()
        {
            this.InitializeComponent();
            Sessions = new ObservableCollection<ChatSession>();
            Models = new ObservableCollection<Model>();
        }

        private void RoleSwitchButton_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            switch ((Roles)Enum.Parse(typeof(Roles), RoleSwitchButton.Content.ToString().ToUpper()))
            {
                case Roles.USER:
                    RuntimeConfig.CurrentRole = Roles.ASSISTANT;
                    RoleSwitchButton.Content = "Assistant";
                    break;
                case Roles.ASSISTANT:
                    RuntimeConfig.CurrentRole = Roles.SYSTEM;
                    RoleSwitchButton.Content = "System";
                    break;
                case Roles.SYSTEM:
                    RuntimeConfig.CurrentRole = Roles.USER;
                    RoleSwitchButton.Content = "User";
                    break;
            }
        }

        private async void Inference()
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            if (!RuntimeConfig.IsConnected)
            {
                ContentDialog noOAI = new ContentDialog
                {
                    XamlRoot = MainWindow.GetRootGrid().XamlRoot,
                    Title = "Error",
                    Content = "No OpenAI-compatible endpoint connected.",
                    CloseButtonText = "OK"
                };
                ContentDialogResult result = await noOAI.ShowAsync();
                return;
            }
            if (ChatHistoryListView.SelectedIndex < 0)
            {
                ContentDialog noSession = new ContentDialog
                {
                    XamlRoot = MainWindow.GetRootGrid().XamlRoot,
                    Title = "Error",
                    Content = "No chat session selected.",
                    CloseButtonText = "OK"
                };
                ContentDialogResult result = await noSession.ShowAsync();
                return;
            }
            if (SelectedModelComboBox.SelectedIndex < 0)
            {
                ContentDialog noModel = new ContentDialog
                {
                    XamlRoot = MainWindow.GetRootGrid().XamlRoot,
                    Title = "Error",
                    Content = "No model selected.",
                    CloseButtonText = "OK"
                };
                ContentDialogResult result = await noModel.ShowAsync();
                return;
            }
            if (string.IsNullOrEmpty(PromptTextBox.Text))
            {
                ContentDialog noSession = new ContentDialog
                {
                    XamlRoot = MainWindow.GetRootGrid().XamlRoot,
                    Title = "Error",
                    Content = "You must enter a prompt before sending.",
                    CloseButtonText = "OK"
                };
                ContentDialogResult result = await noSession.ShowAsync();
                return;
            }
            Message message = new Message();
            message.Content = PromptTextBox.Text;
            message.ContentIndex = 0;
            switch ((Roles)Enum.Parse(typeof(Roles), RoleSwitchButton.Content.ToString().ToUpper()))
            {
                case Roles.USER:
                    message.Role = Roles.USER;
                    break;
                case Roles.ASSISTANT:
                    message.Role = Roles.ASSISTANT;
                    break;
                case Roles.SYSTEM:
                    message.Role = Roles.SYSTEM;
                    break;
            }
            Sessions[ChatHistoryListView.SelectedIndex].Messages.Add(message);
            PromptTextBox.Text = "";
            List<ChatMessage> messages = new List<ChatMessage>();
            if (!string.IsNullOrEmpty(RuntimeConfig.SystemPrompt))
            {
                messages.Add(ChatMessage.CreateSystemMessage(RuntimeConfig.SystemPrompt));
            }
            foreach (Message msg in Sessions[ChatHistoryListView.SelectedIndex].Messages.ToList())
            {
                switch (msg.Role)
                {
                    case Roles.USER:
                        messages.Add(ChatMessage.CreateUserMessage(msg.Content));
                        break;
                    case Roles.ASSISTANT:
                        messages.Add(ChatMessage.CreateAssistantMessage(msg.Content));
                        break;
                    case Roles.SYSTEM:
                        messages.Add(ChatMessage.CreateSystemMessage(msg.Content));
                        break;
                }
            }
            ChatCompletionOptions options = new ChatCompletionOptions();
            options.Seed = RuntimeConfig.Seed;
            options.Temperature = RuntimeConfig.Temperature;
            options.FrequencyPenalty = RuntimeConfig.FrequencyPenalty;
            options.PresencePenalty = RuntimeConfig.PresencePenalty;
            options.MaxTokens = RuntimeConfig.MaxTokens;
            if (RuntimeConfig.CurrentRole == Roles.USER)
            {
                RuntimeConfig.IsInferencing = true;
                TaskRing.IsIndeterminate = true;
                Message response = new Message();
                response.Role = Roles.ASSISTANT;
                var chatClient = RuntimeConfig.OAIClient.GetChatClient(Models[SelectedModelComboBox.SelectedIndex].Identifier);
                if (!RuntimeConfig.StreamResponse)
                {
                    ChatCompletion aiResponse = await chatClient.CompleteChatAsync(messages, options);
                    foreach (var i in aiResponse.Content)
                    {
                        response.Content = i.Text;
                    }
                    Sessions[ChatHistoryListView.SelectedIndex].Messages.Add(response);
                    RuntimeConfig.IsInferencing = false;
                    TaskRing.IsIndeterminate = false;
                }
                else
                {
                    response.Content = "";
                    var index = Sessions[ChatHistoryListView.SelectedIndex].Messages.Count;
                    Sessions[ChatHistoryListView.SelectedIndex].Messages.Add(response);
                    AsyncCollectionResult<StreamingChatCompletionUpdate> streamResponse = chatClient.CompleteChatStreamingAsync(messages, options);
                    await foreach (StreamingChatCompletionUpdate chunk in streamResponse)
                    {
                        foreach (ChatMessageContentPart chunkPart in chunk.ContentUpdate)
                        {
                            //a hacky method to stream response
                            //causes flickering atm, will figure out fix later
                            Message newRes = new Message();
                            newRes.Role = Roles.ASSISTANT;
                            newRes.Content = Sessions[ChatHistoryListView.SelectedIndex].Messages[index].Content + chunkPart.Text;
                            Sessions[ChatHistoryListView.SelectedIndex].Messages[index] = newRes;
                        }
                    }
                    RuntimeConfig.IsInferencing = false;
                    TaskRing.IsIndeterminate = false;
                }
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            Inference();
        }

        private void PromptTextBox_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (!Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                {
                    Inference();
                    e.Handled = true;
                }
            }
        }

        private void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            ChatSession session = new ChatSession();
            session.Name = "New Chat";
            session.Time = DateTime.Now;
            session.Id = $"chat_{RandomNumberGenerator.GetHexString(8, true)}";
            session.Parameters = new Dictionary<string, object>();
            session.Messages = new ObservableCollection<Message>();
            Sessions.Add(session);
        }

        private void DuplicateSessionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            var item = (sender as FrameworkElement).DataContext;
            var session = item as ChatSession;
            ChatSession dupSession = new ChatSession();
            dupSession.Name = session.Name;
            dupSession.Time = DateTime.Now;
            dupSession.Id = $"chat_{RandomNumberGenerator.GetHexString(8, true)}";
            dupSession.Parameters = session.Parameters;
            dupSession.Messages = new ObservableCollection<Message>();
            Sessions.Add(dupSession);
        }

        private void DeleteSessionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            var item = (sender as FrameworkElement).DataContext;
            var session = item as ChatSession;
            Sessions[ChatHistoryListView.SelectedIndex].Messages.Clear();
            Sessions.Remove(session);
        }

        private async void APIConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            if (RuntimeConfig.IsConnected)
            {
                ModelTextBlock.Text = "Not Connected";
                RuntimeConfig.OAIClient = null;
                RuntimeConfig.EndpointURL = "";
                RuntimeConfig.EndpointAPIKey = "";
                RuntimeConfig.IsConnected = false;
                APIConnectButton.Content = "Connect";
                ConnectionStatus.Text = "No OpenAI-compatible endpoint connected";
                Models.Clear();
                return;
            }
            if (string.IsNullOrEmpty(URLTextBox.Text))
            {
                ModelTextBlock.Text = "URL missing!";
                return;
            }
            Dictionary<string, object> res = await OAIWrapper.CheckEndpointAndGetModelsAsync(RuntimeConfig.EndpointURL, RuntimeConfig.EndpointAPIKey);
            if ((bool)res["result"])
            {
                foreach (var m in (List<string>)res["models"])
                {
                    Model model = new Model();
                    model.Name = Path.GetFileNameWithoutExtension(m);
                    model.Identifier = m;
                    Models.Add(model);
                }
                string apiKey = RuntimeConfig.EndpointAPIKey;
                if (string.IsNullOrEmpty(RuntimeConfig.EndpointAPIKey))
                {
                    apiKey = "placeholder_just_for_this";
                }
                RuntimeConfig.OAIClient = new OpenAIClient(apiKey, new()
                {
                    Endpoint = new Uri(RuntimeConfig.EndpointURL)
                });
                ModelTextBlock.Text = "Connected";
                ConnectionStatus.Text = "Model";
                APIConnectButton.Content = "Disconnect";
                RuntimeConfig.IsConnected = true;
                
            }
            else
            {
                ModelTextBlock.Text = "Connect failed!";
                return;
            }
        }

        private void ChatHistoryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            MessagesListView.ItemsSource = Sessions[ChatHistoryListView.SelectedIndex].Messages;
        }

        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                ClearHistoryButton.Flyout.Hide();
                return;
            }
            ChatHistoryListView.Items.Clear();
            MessagesListView.Items.Clear();
            Sessions.Clear();
            ClearHistoryButton.Flyout.Hide();
        }
    }
}
