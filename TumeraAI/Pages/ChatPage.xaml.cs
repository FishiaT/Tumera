using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using TumeraAI.Main.API;
using TumeraAI.Main.Types;
using TumeraAI.Main.Utils;
using Windows.ApplicationModel.Contacts;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TumeraAI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChatPage : Page
    {
        public ChatPage()
        {
            this.InitializeComponent();
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

        private async void SendButton_Click(object sender, RoutedEventArgs e)
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
            if (string.IsNullOrEmpty(RuntimeConfig.SelectedSession))
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
            message.History = (ChatHistoryListView.SelectedItem as ChatSession).Messages;
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
            MessagesListView.Items.Add(message);
            (ChatHistoryListView.SelectedItem as ChatSession).Messages.Add(message);
            PromptTextBox.Text = "";
            if (RuntimeConfig.CurrentRole == Roles.USER)
            {
                RuntimeConfig.IsInferencing = true;
                TaskRing.IsIndeterminate = true;
                var response = await OpenAI.ChatCompletion((ChatHistoryListView.SelectedItem as ChatSession).Messages, RuntimeConfig.EndpointURL, RuntimeConfig.EndpointAPIKey);
                if ((bool)response["result"])
                {
                    Message genResponse = new Message();
                    genResponse.Content = (string)response["response"];
                    genResponse.Role = Roles.ASSISTANT;
                    MessagesListView.Items.Add(genResponse);
                    (ChatHistoryListView.SelectedItem as ChatSession).Messages.Add(genResponse);
                    RuntimeConfig.IsInferencing = false;
                    TaskRing.IsIndeterminate = false;
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
            session.Messages = new List<Message>();
            session.Parameters = new Dictionary<string, object>();
            ChatHistoryListView.Items.Add(session);
            RuntimeConfig.Sessions.Add(session.Id, session);
        }

        private void DuplicateSessionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            var item = (sender as FrameworkElement).DataContext;
            var session = item as ChatSession;
            session.Time = DateTime.Now;
            session.Id = $"chat_{RandomNumberGenerator.GetHexString(8, true)}";
            ChatHistoryListView.Items.Add(session);
            RuntimeConfig.Sessions.Add(session.Id, session);
        }

        private void DeleteSessionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            var item = (sender as FrameworkElement).DataContext;
            var session = item as ChatSession;
            if (ChatHistoryListView.SelectedItem == session)
            {
                MessagesListView.Items.Clear();
            }
            ChatHistoryListView.Items.Remove(session);
            RuntimeConfig.Sessions.Remove(session.Id);
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
                RuntimeConfig.EndpointURL = "";
                RuntimeConfig.EndpointAPIKey = "";
                RuntimeConfig.IsConnected = false;
                APIConnectButton.Content = "Connect";
                ConnectionStatus.Text = "No OpenAI-compatible endpoint connected";
                return;
            }
            if (string.IsNullOrEmpty(URLTextBox.Text))
            {
                ModelTextBlock.Text = "URL missing!";
                return;
            }
            Dictionary<string, object> res = await OpenAI.CheckEndpointAndGetModelsAsync(URLTextBox.Text, APIKeyTextBox.Text);
            if ((bool)res["result"])
            {
                ModelTextBlock.Text = "Connected";
                RuntimeConfig.EndpointURL = URLTextBox.Text;
                RuntimeConfig.EndpointAPIKey = APIKeyTextBox.Text;
                RuntimeConfig.IsConnected = true;
                APIConnectButton.Content = "Disconnect";
                ConnectionStatus.Text = $"Connected ({Path.GetFileNameWithoutExtension((string)res["model"])})";
                URLTextBox.Text = "";
                APIKeyTextBox.Text = "";
            }
            else
            {
                ModelTextBlock.Text = "Connect failed";
            }
        }

        private void ChatHistoryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                RuntimeConfig.SelectedSession = (ChatHistoryListView.SelectedItems[0] as ChatSession).Id;
                MessagesListView.Items.Clear();
                foreach (var item in (ChatHistoryListView.SelectedItems[0] as ChatSession).Messages)
                {
                    MessagesListView.Items.Add(item);
                }
            }
            catch (COMException ex)
            {
                if (RuntimeConfig.Sessions.ContainsKey(RuntimeConfig.SelectedSession))
                {
                    RuntimeConfig.SelectedSession = "";
                }
            }
        }

        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (RuntimeConfig.IsInferencing)
            {
                return;
            }
            ChatHistoryListView.Items.Clear();
            MessagesListView.Items.Clear();
            RuntimeConfig.SelectedSession = "";
            RuntimeConfig.Sessions.Clear();
            ClearHistoryButton.Flyout.Hide();
        }
    }
}
