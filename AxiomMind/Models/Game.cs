using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxiomMind.Models
{
    public class Game
    {
        public string Guid { get; set; }
        private HashSet<string> Users { get; set; }
        private Dictionary<string,string> Guesses { get; set; }
        public int Round { get; set; }
        private byte[] Code { get; set; }

        public Game(HashSet<string> users)
        {
            this.Guid = System.Guid.NewGuid().ToString();
            this.Round = 1;
            this.Code = GetNewCode();
            Guesses = new Dictionary<string, string>();
            Users = new HashSet<string>();

            foreach(var user in users)
            {
                this.Users.Add(user);
            }
        }

        private byte[] GetNewCode()
        {
            var code = new byte[8];
            Random r = new Random(DateTime.Now.Millisecond);

            for(int i = 0; i < 8; i++)
            {
                code[i] = (byte)r.Next(1, 8);
            }

            return code;
        }
    }
}
