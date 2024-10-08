using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualBasic;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Threading;
using TumeraAI.Main.API;
using TumeraAI.Main.Types;
using TumeraAI.Main.Utils;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Control;
using Windows.Media.Protection.PlayReady;
using Windows.System;

#pragma warning disable OPENAI001
namespace TumeraAI.Pages
{
    public sealed partial class ChatPage : Page
    {
        public static ObservableCollection<ChatSession> Sessions = new ObservableCollection<ChatSession>();
        public CollectionViewSource SessionsCollectionViewSource = new CollectionViewSource { Source = Sessions }; 
        public ObservableCollection<Model> Models = new ObservableCollection<Model>();
        public ChatPage()
        {
            this.InitializeComponent();
            var textBoxQuickSendKA = new KeyboardAccelerator
            {
                Key = VirtualKey.Enter,
                Modifiers = VirtualKeyModifiers.None
            };
            textBoxQuickSendKA.Invoked += TextBoxQuickSendKA_Invoked;
            PromptTextBox.KeyboardAccelerators.Add(textBoxQuickSendKA);
        }

        private void TextBoxQuickSendKA_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            Inference();
            args.Handled = true;
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

        private async void Inference(int msgIndex = 0, bool regenerate = false)
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
            if (string.IsNullOrEmpty(PromptTextBox.Text) && !regenerate)
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
            if (ChatSessionsListView.SelectedIndex < 0)
            {
                AddNewSession();
            }
            int currentIndex = ChatSessionsListView.SelectedIndex;
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
            if (!regenerate) Sessions[currentIndex].Messages.Add(message);
            PromptTextBox.Text = "";
            List<ChatMessage> messages = new List<ChatMessage>();
            if (!string.IsNullOrEmpty(RuntimeConfig.SystemPrompt))
            {
                messages.Add(ChatMessage.CreateSystemMessage(RuntimeConfig.SystemPrompt));
            }
            int curIndex = 0;
            foreach (Message msg in Sessions[currentIndex].Messages.ToList())
            {
                if (curIndex == msgIndex && regenerate) break;
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
                curIndex++;
            }
            if (RuntimeConfig.CurrentRole == Roles.USER && !regenerate || regenerate)
            {
                RuntimeConfig.IsInferencing = true;
                TaskRing.IsIndeterminate = true;
                Message response = new Message();
                response.Role = Roles.ASSISTANT;
                response.ModelUsed = Models[SelectedModelComboBox.SelectedIndex].Name;
                response.Contents = new List<string>();
                var chatClient = RuntimeConfig.OAIClient.GetChatClient(Models[SelectedModelComboBox.SelectedIndex].Identifier);
                ChatCompletionOptions options = new ChatCompletionOptions();
                options.Seed = RuntimeConfig.Seed;
                options.Temperature = RuntimeConfig.Temperature;
                options.FrequencyPenalty = RuntimeConfig.FrequencyPenalty;
                options.PresencePenalty = RuntimeConfig.PresencePenalty;
                options.MaxTokens = RuntimeConfig.MaxTokens;
                if (!RuntimeConfig.StreamResponse)
                {
                    int rIndex;
                    if (!regenerate)
                    {
                        rIndex = Sessions[currentIndex].Messages.Count;
                    }
                    else
                    {
                        rIndex = msgIndex;
                    }
                    if (!regenerate) Sessions[currentIndex].Messages.Add(response);
                    ChatCompletion aiResponse = await chatClient.CompleteChatAsync(messages, options);
                    var curMsg = Sessions[currentIndex].Messages[rIndex];
                    var newMsg = curMsg;
                    foreach (var i in aiResponse.Content)
                    {
                        newMsg.Content = i.Text;
                        newMsg.ModelUsed = Models[SelectedModelComboBox.SelectedIndex].Name;
                    }
                    newMsg.Contents.Add(newMsg.Content);
                    if (regenerate) newMsg.ContentIndex = newMsg.RealContentCount;
                    Sessions[currentIndex].Messages[rIndex] = newMsg;
                }
                else
                {
                    response.Content = "";
                    int rIndex;
                    if (!regenerate)
                    {
                        rIndex = Sessions[currentIndex].Messages.Count;
                    }
                    else
                    {
                        rIndex = msgIndex;
                    }
                    if (!regenerate) Sessions[currentIndex].Messages.Add(response);
                    AsyncCollectionResult<StreamingChatCompletionUpdate> streamResponse = chatClient.CompleteChatStreamingAsync(messages, options);
                    var curMsg = Sessions[currentIndex].Messages[rIndex];
                    curMsg.Content = "";
                    await foreach (StreamingChatCompletionUpdate chunk in streamResponse)
                    {
                        foreach (ChatMessageContentPart chunkPart in chunk.ContentUpdate)
                        {
                            curMsg.Content += chunkPart.Text;
                        }
                    }
                    Message newResS = new Message();
                    newResS = Sessions[currentIndex].Messages[rIndex];
                    newResS.Contents.Add(Sessions[currentIndex].Messages[rIndex].Content);
                    newResS.ModelUsed = Models[SelectedModelComboBox.SelectedIndex].Name;
                    if (regenerate) newResS.ContentIndex = newResS.RealContentCount;
                    Sessions[currentIndex].Messages[rIndex] = newResS;
                }
                RuntimeConfig.IsInferencing = false;
                TaskRing.IsIndeterminate = false;
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            Inference();
        }
        
        private void AddNewSession()
        {
            var currentCount = Sessions.Count;
            ChatSession session = new ChatSession();
            session.Name = "New Chat";
            session.Time = DateTime.Now;
            session.Id = $"chat_{RandomNumberGenerator.GetHexString(8, true)}";
            session.Messages = new ObservableCollection<Message>();
            Sessions.Add(session);
            ChatSessionsListView.SelectedIndex = currentCount;
        }

        private void NewSessionButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewSession();
        }

        private void DuplicateSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            var currentCount = Sessions.Count;
            var item = (sender as FrameworkElement).DataContext;
            var session = item as ChatSession;
            ChatSession dupSession = new ChatSession();
            dupSession.Name = session.Name;
            dupSession.Time = DateTime.Now;
            dupSession.Id = $"chat_{RandomNumberGenerator.GetHexString(8, true)}";
            dupSession.Messages = new ObservableCollection<Message>();
            Sessions.Add(dupSession);
            ChatSessionsListView.SelectedIndex = currentCount;
        }

        private void DeleteSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            var item = (sender as FrameworkElement).DataContext;
            var session = item as ChatSession;
            var curIndex = Sessions.IndexOf(session);
            Sessions[ChatSessionsListView.SelectedIndex].Messages.Clear();
            Sessions.Remove(session);
            if (Sessions.Count >= 1)
            {
                ChatSessionsListView.SelectedIndex = curIndex - 1;
            }
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
                APIConnectButton.Content = "Disconnect";
                RuntimeConfig.IsConnected = true;
                
            }
            else
            {
                ModelTextBlock.Text = "Connect failed!";
                return;
            }
        }

        private void ChatSessionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatSessionsListView.Items.Count > 0 && ChatSessionsListView.SelectedIndex >= 0)
            {
                MessagesListView.ItemsSource = Sessions[ChatSessionsListView.SelectedIndex].Messages;
            }
        }

        private void DeleteAllSessionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                DeleteAllSessionsButton.Flyout.Hide();
                return;
            }
            Sessions.Clear();
            MessagesListView.ItemsSource = null;
            DeleteAllSessionsButton.Flyout.Hide();
        }

        private void CopyContentButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext;
            var message = item as Message;
            var pkg = new DataPackage();
            pkg.SetText(message.Content);
            Clipboard.SetContent(pkg);
        }

        private void RegenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            var item = (sender as FrameworkElement).DataContext;
            var message = item as Message;
            var index = Sessions[ChatSessionsListView.SelectedIndex].Messages.IndexOf(message);
            Inference(index, true);
        }

        private void DeleteMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            var item = (sender as FrameworkElement).DataContext;
            var message = item as Message;
            Sessions[ChatSessionsListView.SelectedIndex].Messages.Remove(message);
        }

        private void PreviousIterationButton_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            var item = (sender as FrameworkElement).DataContext;
            var message = item as Message;
            var index = Sessions[ChatSessionsListView.SelectedIndex].Messages.IndexOf(message);
            var newMsg = new Message();
            newMsg = message;
            if (newMsg.ContentIndex -1 > -1)
            {
                newMsg.ContentIndex = newMsg.ContentIndex - 1;
                newMsg.Content = newMsg.Contents[newMsg.ContentIndex];
                Sessions[ChatSessionsListView.SelectedIndex].Messages[index] = newMsg;
            }
        }

        private void NextIterationButton_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            var item = (sender as FrameworkElement).DataContext;
            var message = item as Message;
            var index = Sessions[ChatSessionsListView.SelectedIndex].Messages.IndexOf(message);
            var newMsg = new Message();
            newMsg = message;
            if (newMsg.ContentIndex + 1 <= newMsg.RealContentCount)
            {
                newMsg.ContentIndex = newMsg.ContentIndex + 1;
                newMsg.Content = newMsg.Contents[newMsg.ContentIndex];
                Sessions[ChatSessionsListView.SelectedIndex].Messages[index] = newMsg;
            }
        }

        private async void EditContentButton_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            var item = (sender as FrameworkElement).DataContext;
            var message = item as Message;
            var index = Sessions[ChatSessionsListView.SelectedIndex].Messages.IndexOf(message);
            TextBox msgTextBox = new TextBox
            {
                MaxHeight = 177.74,
                Width = 450,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap
            };
            ScrollViewer.SetVerticalScrollBarVisibility(msgTextBox, ScrollBarVisibility.Auto);
            msgTextBox.Text = message.Content;
            ContentDialog dialog = new ContentDialog
            {
                XamlRoot = MainWindow.GetRootGrid().XamlRoot,
                Title = "Edit message",
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Content = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                    },
                    Children =
                    {
                        msgTextBox
                    }
                },
                PrimaryButtonText = "Confirm",
                CloseButtonText = "Cancel"
            }; 

            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (!msgTextBox.Text.Equals(message.Content))
                {
                    var newMsg = message;
                    newMsg.Content = msgTextBox.Text;
                    Sessions[ChatSessionsListView.SelectedIndex].Messages[index] = newMsg;
                }
            }
        }
    }
}
