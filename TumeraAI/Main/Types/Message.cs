using Microsoft.UI.Content;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TumeraAI.Main.Utils;
using Windows.Storage.Search;

namespace TumeraAI.Main.Types
{
    public class Message
    {
        public Roles Role { get; set; }
        public string RoleName => Role.ToString();
        public DateTime Time => DateTime.Now;
        public string FormattedTime => Time.ToString("hh:mm tt");
        public List<string> Contents { get; set; }
        public int ContentIndex = 0;
        public string Content = "";
        public int VisibleContentIndex => ContentIndex + 1;
        public int RealContentCount
        {
            get
            {
                if (Contents != null) return Contents.Count - 1;
                return 0;
            }
        } 

        public bool MultipleResponsesPanelVisible
        {
            get
            {
                if (RealContentCount > 0)
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsAIResponse => Role == Roles.ASSISTANT;
        public string ModelUsed = "";
    }
}
