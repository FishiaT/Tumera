using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TumeraAI.Main.Types
{
    public class ChatSession
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public DateTime Time { get; set; }
        public Dictionary<String, object> Parameters { get; set; }
        public List<Message> Messages { get; set; }
    }
}
