using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxiomMind.Models
{
    public class GuessResult
    {
        public bool Success { get; set; }
        public byte Exactly { get; set; }
        public byte Near { get; set; }
        public string UserName { get; set; }
        public string Guess { get; internal set; }

        public GuessResult()
        {
            Success = false;
        }

        public GuessResult(byte exactly, byte near, string userName)
        {
            Exactly = exactly;
            Near = near;
            UserName = userName;
            Success = true;
        }
    }
}
