using System;
using System.Collections.Generic;
using TumeraAI.Main.Types;
using Windows.ApplicationModel.Chat;

namespace TumeraAI.Main.Utils
{
    class RuntimeConfig
    {
        public static string EndpointURL = "";
        public static string EndpointAPIKey = "";
        public static bool IsConnected = false;
        public static Roles CurrentRole = Roles.USER;
        public static Dictionary<string, ChatSession> Sessions = new Dictionary<string, ChatSession>();
        public static string SelectedSession = "";
        public static bool IsInferencing = false;
        public static string SystemPrompt = "You are a helpful AI assistant.";
        public static bool StreamResponse = false;
        public static int Seed = -1;
        public static double Temperature = 1;
        public static double FrequencyPenalty = 0;
        public static double PresencePenalty = 0;
        public static int MaxTokens = -1;
    }
}
