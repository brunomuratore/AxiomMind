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
        private HashSet<string> CurrentUsersGuessed { get; set; }
        private Dictionary<string,string> Guesses { get; set; }
        public int Round { get; set; }
        private string Code { get; set; }
        public string RemainingUsers { get; private set; }

        public Game(HashSet<string> users)
        {
            this.Guid = System.Guid.NewGuid().ToString();
            this.Round = 1;
            this.Code = GetNewCode();
            Guesses = new Dictionary<string, string>();
            Users = new HashSet<string>();
            CurrentUsersGuessed = new HashSet<string>();
            RemainingUsers = "";

            foreach (var user in users)
            {
                this.Users.Add(user);
            }
        }

        private string GetNewCode()
        {
            var code = "";
            Random r = new Random(DateTime.Now.Millisecond);

            for(int i = 0; i < 8; i++)
            {
                code += r.Next(1, 8).ToString();
            }

            return code;
        }

        internal bool CanGuess(string name)
        {
            if (CurrentUsersGuessed.Contains(name))
                return false;
            else
                return true;
        }

        internal bool MakeGuess(string guess, string name)
        {
            CurrentUsersGuessed.Add(name);
            
            Guesses.Add(name, guess);

            if (Guesses.Count == Users.Count)
                return true;
            else
            {
                string remainingUsers = "";
                foreach (var user in Users)
                {
                    if (!CurrentUsersGuessed.Contains(user))
                        remainingUsers += user + ", ";
                }
                if (remainingUsers.Length > 0)
                    RemainingUsers = remainingUsers.Remove(remainingUsers.LastIndexOf(','));
                
                return false;
            }
        }

        internal IEnumerable<GuessResult> RoundOver()
        {
            var results = new List<GuessResult>();
            Round++;

            foreach (var guess in Guesses)
            {
                results.Add(AnalyzeGuess(guess.Value, guess.Key));                
            }

            Guesses = new Dictionary<string, string>();
            CurrentUsersGuessed = new HashSet<string>();

            return results;
        }

        private GuessResult AnalyzeGuess(string guess, string user)
        {
            var result = new GuessResult();
            result.Guess = guess;
            var iterator = guess;
            var codeCopy = Code;
            int hp = 0;
            for(int i = 0; i < iterator.Length; i++)
            {
                if (iterator[i] == Code[i])
                {
                    guess = guess.Remove(i - hp, 1);
                    codeCopy = codeCopy.Remove(i - hp, 1);
                    hp++;
                    result.Exactly++;
                }                    
            }
            iterator = guess;
            for (int i = 0; i < iterator.Length; i++)
            {
                if (codeCopy.Contains(iterator[i]))
                {
                    codeCopy = codeCopy.Remove(codeCopy.IndexOf(iterator[i]), 1);
                    result.Near++;
                }
            }

            result.Success = true;
            result.UserName = user;

            return result;
        }

        internal void Removeuser(string name)
        {
            Users.Remove(name);
            Guesses.Remove(name);
        }

        internal bool HasUsers()
        {
            return Users.Count > 0;
        }

        internal bool HasHoRemainingUsers()
        {
            return Guesses.Count() == Users.Count();
        }
    }
}
