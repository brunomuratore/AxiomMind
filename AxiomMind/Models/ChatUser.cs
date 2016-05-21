using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxiomMind.Models
{
    [Serializable]
    public class ChatUser
    {
        public string ConnectionId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string CurrentGame { get; set; }

        public ChatUser()
        {
        }

        public ChatUser(string name)
        {
            Name = name;
            Id = Guid.NewGuid().ToString("d");
        }
    }
}
