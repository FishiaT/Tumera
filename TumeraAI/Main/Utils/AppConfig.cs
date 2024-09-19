using System;
using System.IO;

namespace TumeraAI.Main.Utils
{
    internal class AppConfig
    {
        public static string AppName = "Tumera";
        public static string AppVersion = "0.1.0a2";
        public static string DefaultDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"{AppName}");
        public static string ModelsPath = Path.Combine(DefaultDataPath, "models");
        public static string BackendsPath = Path.Combine(DefaultDataPath, "backends");
        public static string ChatPath = Path.Combine(DefaultDataPath, "chats");
        public static string ProfilesPath = Path.Combine(DefaultDataPath, "profiles");
        public static string ConfigFile = "config.json";
    }
}
