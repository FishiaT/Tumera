using System;
using System.Collections.Generic;
using TumeraAI.Main.Utils;

namespace TumeraAI.Main.Types
{
    public class Message
    {
        public Roles Role { get; set; }
        public string RoleName => Role.ToString();
        public DateTime Time => DateTime.Now;
        public string FormattedTime => Time.ToString("hh:mm tt");
        public List<string> Contents { get; set; }
        public int ContentIndex = 1;
        public string Content = "";
        public bool MultipleResponsesPanelVisible => (Contents?.Count ?? 0) > 1;
        public bool IsAIResponse => Role == Roles.ASSISTANT;
        public string ModelUsed = "";
    }
}
