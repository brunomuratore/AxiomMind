using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxiomMind.Models
{
    public class ChatRoom
    {
        public List<ChatMessage> Messages { get; set; }
        public HashSet<string> Users { get; set; }
        public bool HasGame { get; set; }

        public ChatRoom()
        {
            Messages = new List<ChatMessage>();
            Users = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HasGame = false;
        }
    }
}
