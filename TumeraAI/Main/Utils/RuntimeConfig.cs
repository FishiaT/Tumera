using OpenAI.Managers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TumeraAI.Main.Types;

namespace TumeraAI.Main.Utils
{
    class RuntimeConfig
    {
        public static string EndpointURL = "";
        public static string EndpointAPIKey = "";
        public static bool IsConnected = false;
        public static OpenAIService OAIClient;
        public static Roles CurrentRole = Roles.USER;
        public static bool IsInferencing = false;
        public static string SystemPrompt = "";
        public static bool StreamResponse = true;
        public static bool EnableAttachments = false;
        public static List<string> SupportedAttachments = new List<string>() { ".png", ".jpg" };
        public static int Seed = -1;
        public static float Temperature = 1;
        public static float FrequencyPenalty = 0;
        public static float PresencePenalty = 0;
        public static int MaxTokens = -1;
    }
}
