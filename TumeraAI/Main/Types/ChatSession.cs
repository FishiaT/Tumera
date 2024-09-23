using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TumeraAI.Main.Types
{
    public class ChatSession
    {
        public string Name { get; set; }
        public DateTime Time { get; set; }
        public ObservableCollection<Message> Messages;
    }
}
