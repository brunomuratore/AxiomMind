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
        public bool HasBot { get; set; }

        /// <summary>
        /// Creates a new game containing all the provided users.
        /// </summary>
        /// <param name="users">A hashset containing the nickname of all players that will participate in the game.</param>
        public Game(HashSet<string> users)
        {
            this.Guid = System.Guid.NewGuid().ToString();
            this.Round = 1;
            this.Code = GetNewCode();
            Guesses = new Dictionary<string, string>();
            Users = new HashSet<string>();
            CurrentUsersGuessed = new HashSet<string>();
            RemainingUsers = "";
            HasBot = false;

            foreach (var user in users)
            {
                if(user != GameHub.BotName)
                    this.Users.Add(user);
            }
        }

        #region Internal Methods

        /// <summary>
        /// Verifies if an user can make a guess.
        /// </summary>
        /// <param name="name">Username of the user</param>
        /// <returns>True if he can make a guess</returns>
        internal bool CanGuess(string name)
        {
            if (CurrentUsersGuessed.Contains(name))
                return false;
            else
                return true;
        }

        /// <summary>
        /// Computes a guess for a user.
        /// </summary>
        /// <param name="guess">User's guess in format 12345678, where each number represents a color</param>
        /// <param name="name">User's nickname</param>
        /// <returns></returns>
        internal bool MakeGuess(string guess, string name)
        {
            CurrentUsersGuessed.Add(name);
            
            Guesses.Add(name, guess);

            if (Guesses.Count == Users.Count)
                return true;
            else
            {
                string remainingUsers = "";
                lock(Users)
                {
                    foreach (var user in Users)
                    {
                        if (!CurrentUsersGuessed.Contains(user))
                            remainingUsers += user + ", ";
                    }
                    if (remainingUsers.Length > 0)
                        RemainingUsers = remainingUsers.Remove(remainingUsers.LastIndexOf(','));
                }
                
                return false;
            }
        }

        /// <summary>
        /// Remove a user from the game
        /// </summary>
        /// <param name="name">User's nickname</param>
        internal void Removeuser(string name)
        {
            Users.Remove(name);
            Guesses.Remove(name);
        }

        /// <summary>
        /// Verify if the game contains on or more users.
        /// </summary>
        /// <returns>True if there are at least one user playing.</returns>
        internal bool HasUsers()
        {
            return Users.Count > 0;
        }

        /// <summary>
        /// Verifies if there are no more remaining users to make their guess in the current round.
        /// If no, the next round can begin.
        /// </summary>
        /// <returns>True if the next round can begin. False if there are still players that needs to play.</returns>
        internal bool HasHoRemainingUsers()
        {
            return Guesses.Count() == Users.Count();
        }

        /// <summary>
        /// Ends a round and compute players guesses, informing the results.
        /// </summary>
        /// <returns>A collection of each user guess result.</returns>
        internal IEnumerable<GuessResult> RoundOver()
        {
            var results = new List<GuessResult>();
            Round++;

            lock (Guesses)
            {
                foreach (var guess in Guesses)
                {
                    results.Add(AnalyzeGuess(guess.Value, guess.Key));
                }
            }

            Guesses = new Dictionary<string, string>();
            CurrentUsersGuessed = new HashSet<string>();

            return results;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Compute the scores of a guess
        /// </summary>
        /// <param name="guess">User's guess in format 12345678</param>
        /// <param name="user">User's name</param>
        /// <returns>A <c>GuessResult</c> object containnig the results for the user guess.</returns>
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
        
        /// <summary>
        /// Generates a new random code for the game.
        /// </summary>
        /// <returns></returns>
        private string GetNewCode()
        {
            var code = "";
            Random r = new Random(DateTime.Now.Millisecond);

            for (int i = 0; i < 8; i++)
            {
                code += r.Next(1, 8).ToString();
            }

            return code;
        }

        #endregion
    }
}
