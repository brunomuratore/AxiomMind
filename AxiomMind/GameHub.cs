using AxiomMind.Bot;
using AxiomMind.Models;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace AxiomMind
{
    public class GameHub : Hub
    {
        // Holds info of each connected user
        public static readonly ConcurrentDictionary<string, ChatUser> _users = new ConcurrentDictionary<string, ChatUser>(StringComparer.OrdinalIgnoreCase);
        // Holds info of which room is a user connected
        public static readonly ConcurrentDictionary<string, string> _userRooms = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Holds info of each created room 
        public static readonly ConcurrentDictionary<string, ChatRoom> _rooms = new ConcurrentDictionary<string, ChatRoom>(StringComparer.OrdinalIgnoreCase);
        // Holds info of each game in progress
        public static readonly ConcurrentDictionary<string, Game> _games = new ConcurrentDictionary<string, Game>();

        // Our pretty Bot Name
        public const string BotName = "AxiomBot";

        #region Public Chat
        /// <summary>
        /// Occurs when a client wants to connect to the server.
        /// Generate user cookies and wait for next request.
        /// </summary>
        /// <returns>True if the user could connect. False Otherwise.</returns>
        public bool Join()
        {
            // Check the user id cookie
            Cookie userIdCookie;

            if (!Context.RequestCookies.TryGetValue("userid", out userIdCookie))
            {
                return false;
            }

            ChatUser user = _users.Values.FirstOrDefault(u => u.Id == userIdCookie.Value);

            if (user != null)
            {
                // Update the users's client id mapping
                user.ConnectionId = Context.ConnectionId;

                // Set some client state
                Clients.Caller.id = user.Id;
                Clients.Caller.name = user.Name;

                // Leave all rooms
                string room;
                if (_userRooms.TryGetValue(user.Name, out room))
                {
                    Clients.Group(room).leave(user);
                    ChatRoom chatRoom = _rooms[room];
                    chatRoom.Users.Remove(user.Name);
                }

                _userRooms[user.Name] = "";

                // Add this user to the list of users
                Clients.Caller.addUser(user);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Occurs when a client sends some command via the console to the server.
        /// We handle this command and execute the proper action.
        /// Available commands:
        /// /nick nickName
        /// /join roomName
        /// /leaveGame
        /// /start
        /// /guess 12345678
        /// /hint
        /// If no command is found, sends a message to all users on the room.
        /// Html tags are not supported and will be encoded.
        /// </summary>
        /// <param name="content">The string representing the command</param>
        public void Send(string content)
        {
            content = content.Replace("<", "&lt;").Replace(">", "&gt;");

            if (!TryHandleCommand(content))
            {
                string roomName = Clients.Caller.room;
                string name = Clients.Caller.name;

                if (!EnsureUserAndRoom())
                    return;

                var chatMessage = new ChatMessage(name, content);

                _rooms[roomName].Messages.Add(chatMessage);

                Clients.Group(roomName).addMessage(chatMessage.Id, chatMessage.User, chatMessage.Text);

            }
        }

        /// <summary>
        /// Occurs when a user is disconnected from the server.
        /// We clean this user data and remove him from the game/room he was.
        /// </summary>
        /// <param name="stopCalled">SignalR parameter, don't change</param>
        /// <returns></returns>
        public override Task OnDisconnected(bool stopCalled)
        {
            ChatUser user = _users.Values.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);
            if (user != null)
            {
                var room = _userRooms[user.Name];
                LeaveGame(user.Name, room);

                ChatUser ignoredUser;
                _users.TryRemove(user.Name, out ignoredUser);

                Clients.Group(room).leave(user);
                ChatRoom chatRoom = _rooms[room];
                chatRoom.Users.Remove(user.Name);

                UpdateRooms();

                _userRooms.TryRemove(user.Name, out room);
            }

            return null;
        }

        /// <summary>
        /// Occurs when any user joins or leave a room.
        /// Updates the room information for all clients that were on that room.
        /// Get a list of users that are in the same room as the requester.
        /// </summary>
        /// <returns>A list of <c>ChatUser</c> representing each user.</returns>
        public IEnumerable<ChatUser> GetUsers()
        {
            string room = Clients.Caller.room;

            if (String.IsNullOrEmpty(room))
            {
                return Enumerable.Empty<ChatUser>();
            }

            return from name in _rooms[room].Users
                   select _users[name];
        }

        /// <summary>
        /// Occurs when some user joined / left a room. Updates room information for all connected users.
        /// </summary>
        public void UpdateRooms()
        {
            var rooms = _rooms.Select(r => new
            {
                Name = r.Key,
                Count = r.Value.Users.Count
            });

            Clients.All.showRooms(rooms);
        }
        #endregion

        #region Public Game

        /// <summary>
        /// Occurs when a user wants to start a new game.
        /// Starts a new game for all users in caller's room and inform all of them.
        /// </summary>
        /// <returns>True if the game could be started.</returns>
        public bool Start()
        {
            string roomName = Clients.Caller.room;
            string name = Clients.Caller.name;

            return StartGame(name, roomName);
        }
        
        /// <summary>
        /// Occurs when a player make a guess in a game.
        /// Add this guess to the game, and if all players made the guess, go to next game Round, informing all players in the game.
        /// </summary>
        /// <param name="guess">String containing player guess in format 12345678, where each number represents a color.</param>
        /// <returns></returns>
        public bool SendGuess(string guess)
        {
            string room = Clients.Caller.room;
            string name = Clients.Caller.name;

            return Guess(name, room, guess);
        }

        #endregion

        #region Private Chat

        /// <summary>
        /// Handle commands sent by user.
        /// </summary>
        /// <param name="message">The command</param>
        /// <returns>True if it was a valid command.</returns>
        private bool TryHandleCommand(string message)
        {
            string room = Clients.Caller.room;
            string name = Clients.Caller.name;

            message = message.Trim();
            if (message.StartsWith("/"))
            {
                string[] parts = message.Substring(1).Split(' ');
                string commandName = parts[0];

                if (commandName.Equals("nick", StringComparison.OrdinalIgnoreCase))
                {
                    string newUserName = String.Join(" ", parts.Skip(1));

                    if (String.IsNullOrEmpty(newUserName))
                    {
                        SendError("No username specified!");
                        return true;
                    }

                    if (newUserName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        SendError("That's already your username...");
                        return true;
                    }

                    return ChangeNickName(name, room, newUserName);
                }
                else
                {
                    if (!EnsureUser())
                        return false;

                    else if (commandName.Equals("join", StringComparison.OrdinalIgnoreCase))
                    {
                        if (parts.Length == 1)
                        {
                            SendError("Please specify the name of the room using \"/join nameOfTheRoom\"?");
                            return true;
                        }
                        return JoinRoom(name, room, parts[1]);
                    }
                    else if (commandName.Equals("guess", StringComparison.OrdinalIgnoreCase))
                    {
                        if (parts.Length == 1)
                        {
                            SendError("Please specify your guess using \"/guess 12345678\"?");
                            return true;
                        }
                        Guess(name, room, parts[1]);
                        return true;
                    }
                    else if (commandName.Equals("leaveGame", StringComparison.OrdinalIgnoreCase))
                    {
                        LeaveGame(name, room);
                        return true;
                    }
                    else if (commandName.Equals("hint", StringComparison.OrdinalIgnoreCase))
                    {
                        GiveHint(name, room);
                        return true;
                    }

                    else if (commandName.Equals("start", StringComparison.OrdinalIgnoreCase))
                    {
                        StartGame(name, room);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Changes user nickname.
        /// </summary>
        /// <param name="name">Current nickname</param>
        /// <param name="room">Room where the user is</param>
        /// <param name="newUserName">New nickname</param>
        /// <returns></returns>
        private bool ChangeNickName(string name, string room, string newUserName)
        {
            if (!_users.ContainsKey(newUserName))
            {
                if (String.IsNullOrEmpty(name) || !_users.ContainsKey(name))
                {
                    AddUser(newUserName);
                }
                else
                {
                    var oldUser = _users[name];
                    var newUser = new ChatUser
                    {
                        Name = newUserName,
                        Id = oldUser.Id,
                        ConnectionId = oldUser.ConnectionId
                    };

                    _users[newUserName] = newUser;
                    _userRooms[newUserName] = _userRooms[name];

                    var r = _userRooms[name];
                    if (!String.IsNullOrEmpty(r))
                    {
                        _rooms[r].Users.Remove(name);
                        _rooms[r].Users.Add(newUserName);
                        Clients.Group(r).changeUserName(oldUser, newUser);
                    }

                    string ignoredRoom;
                    ChatUser ignoredUser;
                    _userRooms.TryRemove(name, out ignoredRoom);
                    _users.TryRemove(name, out ignoredUser);

                    Clients.Caller.name = newUser.Name;

                    Clients.Caller.changeUserName(oldUser, newUser);
                }
            }
            else
            {
                SendError(String.Format("Username '{0}' is already taken!", newUserName));
                return true;
            }

            if (string.IsNullOrEmpty(room))
            {
                JoinRoom(newUserName, "", "General");
                Clients.Caller.addMessage(0, "AxiomMind", "You are in general room.");
                Clients.Caller.addMessage(0, "AxiomMind", "Now you can create or join a room typing \"/join roomname\".");                
            }
            return true;
        }

        /// <summary>
        /// Creates a new room and join the user on it. If the room already exists, just join it.
        /// </summary>
        /// <param name="name">Username of user that will join the room</param>
        /// <param name="room">Current room</param>
        /// <param name="newRoom">New room</param>
        /// <returns></returns>
        private bool JoinRoom(string name, string room, string newRoom)
        {
            ChatRoom chatRoom;
            // Create the room if it doesn't exist
            if (!_rooms.TryGetValue(newRoom, out chatRoom))
            {
                chatRoom = new ChatRoom();
                _rooms.TryAdd(newRoom, chatRoom);
            }


            // Remove the old room
            if (!String.IsNullOrEmpty(room))
            {
                _userRooms[name] = "";
                _rooms[room].Users.Remove(name);

                Clients.Group(room).leave(_users[name]);
                Groups.Remove(Context.ConnectionId, room);
            }

            if (room != "" && _rooms[room].Users.Contains(name))
            {
                SendError("You're already in that room!");
                return false;
            }

            if (!String.IsNullOrEmpty(_users[name].CurrentGame))
            {
                SendError("You can leave a room while you are in a game! You can leave a game by typing /leaveGame");
                return false;
            }

            _userRooms[name] = newRoom;
            chatRoom.Users.Add(name);

            Clients.Group(newRoom).addUser(_users[name]);

            // Set the room on the caller
            Clients.Caller.room = newRoom;

            Groups.Add(Context.ConnectionId, newRoom);

            Clients.Caller.refreshRoom(newRoom);
            UpdateRooms();

            return true;
        }

        /// <summary>
        /// Creates a new user when he joins the server.
        /// </summary>
        /// <param name="newUserName">Users nickname that will be created</param>
        /// <returns>A <c>ChatUser</c> object representing the new user.</returns>
        private ChatUser AddUser(string newUserName)
        {
            var user = new ChatUser(newUserName);
            user.ConnectionId = Context.ConnectionId;
            _users[newUserName] = user;
            _userRooms[newUserName] = "";

            Clients.Caller.name = user.Name;
            Clients.Caller.id = user.Id;

            Clients.Caller.addUser(user);

            return user;
        }

        /// <summary>
        /// Verifies if the caller is connected as user with a valid nickname, and is in a room.
        /// </summary>
        /// <returns>True or False</returns>
        private bool EnsureUserAndRoom()
        {
            if (!EnsureUser())
                return false;

            string room = Clients.Caller.room;
            string name = Clients.Caller.name;

            if (String.IsNullOrEmpty(room) || !_rooms.ContainsKey(room))
            {
                Clients.Caller.addMessage(0, "AxiomMind", "Use '/join room' to join a room.");
                return false;
            }

            if (_userRooms[name] != room)
            {
                SendError(String.Format("You're not in '{0}'. Use '/join {0}' to join it.", room));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sends an error message to the client, that will be shown on Console.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        private void SendError(string errorMessage)
        {
            Clients.Caller.addError(0, "AxiomMind", errorMessage);
        }

        /// <summary>
        /// Verifies if the caller is connected as user with a valid nickname.
        /// </summary>
        /// <returns>True or False</returns>
        private bool EnsureUser()
        {
            string name = Clients.Caller.name;
            if (String.IsNullOrEmpty(name) || !_users.ContainsKey(name))
            {
                Clients.Caller.addMessage(0, "AxiomMind", "You don't have a name. Pick a name using '/nick nickname'.");
                return false;
            }
            return true;
        }
        #endregion

        #region Private Game

        /// <summary>
        /// Starts a new game for all players in the same room as the caller.
        /// </summary>
        /// <param name="name">Username of the caller.</param>
        /// <param name="roomName">Current room of the caller.</param>
        /// <returns>True if the game was started.</returns>
        private bool StartGame(string name, string roomName)
        {
            if (!EnsureUserAndRoom())
            {
                SendError("You need to have a nickName to start a game.");
                return false;
            }

            if (_rooms[roomName].HasGame)
            {
                SendError("You can't start a game here. This room already has a game in progress.");
                return false;
            }

            if (!String.IsNullOrEmpty(_users[name].CurrentGame))
            {
                SendError("Game could not be started. You are already in a game.");
                return false;
            }

            if (_rooms[roomName].Users.Contains(BotName) && _rooms[roomName].Users.Count > 2)
            {
                SendError("You can only start a match with the Bot if you are alone in the room.");
                return false;
            }

            Game game = new Game(_rooms[roomName].Users);

            if (!_games.TryAdd(game.Guid, game))
            {
                SendError("Game could not be started. Please try again.");
                return false;
            }

            lock (_users)
            {
                foreach (var user in _rooms[roomName].Users)
                {
                    if (user == BotName)
                        game.HasBot = true;
                    else
                        _users[user].CurrentGame = game.Guid;
                }
            }

            _rooms[roomName].HasGame = true;
            Clients.Group(roomName).addMessage(0, "AxiomMind", $"User {name} has started a game for {_rooms[roomName].Users.Count()} users.");
            Clients.Group(roomName).gameCreated();
            StartRound(game.Round, roomName);

            return true;
        }

        /// <summary>
        /// If the user is in a game with the Bot, gives the user a hint of what would be the next best guess.
        /// If user continues asking for hints, the Bot should solve the problem sometime.
        /// </summary>
        /// <param name="name">Nickname of the caller user</param>
        /// <param name="room">Current room of the caller user</param>
        private void GiveHint(string name, string room)
        {
            if (!_rooms[room].Users.Contains(BotName))
            {
                SendError("You can only request a Hint if you are playing alone with the AxiomBot.");
                return;
            }

            AxiomBot bot = new AxiomBot();
            var hint = bot.CalculateGeneration(2000, 20);
            string sHint = "";
            foreach (var i in hint)
            {
                sHint += i.ToString();
            }

            Clients.Group(room).addMessage(0, "AxiomBot", $"Try guessing this combination: {sHint}");
            Clients.Group(room).hint(sHint);
        }

        /// <summary>
        /// When all players made their guesses, start a new round of the game.
        /// </summary>
        /// <param name="roundNumber">Number of the new round.</param>
        /// <param name="roomName">Room where the game is being held.</param>
        private void StartRound(int roundNumber, string roomName)
        {
            Clients.Group(roomName).addMessage(0, "AxiomMind", $"");
            Clients.Group(roomName).addMessage(0, "AxiomMind", $"**** ROUND {roundNumber} started ****");
            Clients.Group(roomName).addMessage(0, "AxiomMind", $"Make your guess typing \"/guess 12345678\"");
        }

        /// <summary>
        /// Computes the guess of a player.
        /// </summary>
        /// <param name="name">Caller player nickname.</param>
        /// <param name="room">Caller player room.</param>
        /// <param name="guess">Guess in format 12345678 where each number is a color.</param>
        /// <returns>True if the guess was computed</returns>
        private bool Guess(string name, string room, string guess)
        {
            if (!EnsureUser())
            {
                SendError("You must set a name");
                return false;
            }

            if (guess.Length != 8)
            {
                SendError("Your guess must have 8 characters");
                return false;
            }

            byte[] g = new byte[8];

            for (int i = 0; i < guess.Length; i++)
            {
                if (guess[i] - '0' <= 0 || guess[i] - '0' > 8)
                {
                    SendError("Your guess must contain only digitis from 1 to 8.");
                    return false;
                }

                g[i] = (byte)(guess[i] - '0');
            }

            var gameId = _users[name].CurrentGame;

            if (String.IsNullOrEmpty(gameId))
            {
                SendError("You are not in a game.");
                return false;
            }

            Game game = _games[gameId];

            if (game.CanGuess(name))
            {
                if (game.MakeGuess(guess, name))
                {
                    EndRound(game, room);
                }
                else
                {
                    Clients.Group(room).addMessage(0, "AxiomMind", $"User {name} made a Guess. Remaining players: {game.RemainingUsers}.");
                }
            }
            else
            {
                SendError("You have already sent your guess this round. Please wait for the other playsers.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Occurs when any player wins the game, or there are no more players left on the game.
        /// Inform all players of the winner.
        /// </summary>
        /// <param name="winners">List containing usernames of all the winners (If more than one guessed the code in the same round)</param>
        /// <param name="room">Room where the game was being held</param>
        /// <param name="game"><c>Game</c> object that represents the current game.</param>
        private void EndGame(List<string> winners, string room, Game game)
        {
            _games.TryRemove(game.Guid, out game);

            lock (_users)
            {
                foreach (var user in _rooms[room].Users)
                {
                    _users[user].CurrentGame = "";
                }
            }
            _rooms[room].HasGame = false;

            Clients.Group(room).endGame(winners);
            Clients.Group(room).addMessage(0, "AxiomMind", $"**** END OF GAME ****");
            if (winners.Count > 0)
                Clients.Group(room).addMessage(0, "AxiomMind", $"Our winner{(winners.Count > 1 ? "s" : "")} {(winners.Count > 1 ? "are" : "is")}: {string.Join<string>(" and ", winners)}");
        }

        /// <summary>
        /// Occurs when a user wants to leave the game, or was disconnect from the server.
        /// Remove the player from the game, and make the game continue for all other users.
        /// </summary>
        /// <param name="name">Nickname of player that will be removed from the game.</param>
        /// <param name="room">Room where this player is.</param>
        private void LeaveGame(string name, string room)
        {
            var gameId = _users[name].CurrentGame;
            if (String.IsNullOrEmpty(gameId))
            {
                SendError("You are not in a game.");
                return;
            }

            _games[gameId].Removeuser(name);
            if (!_games[gameId].HasUsers())
            {
                EndGame(new List<string>(), room, _games[gameId]);
            }
            else if (_games[gameId].HasHoRemainingUsers())
            {
                EndRound(_games[gameId], room);
            }

            _users[name].CurrentGame = "";

            Clients.Group(room).addMessage(0, "AxiomMind", $"{name} has left the game.");
        }
        
        /// <summary>
        /// Occurs when all players made a guess for the current round.
        /// Verify if there was a winner and ends the game, otherwise creates a new round.
        /// </summary>
        /// <param name="game"><c>Game</c> object representing the current game.</param>
        /// <param name="room">Room where the game is being held.</param>
        private void EndRound(Game game, string room)
        {
            List<string> winners = new List<string>();
            lock (_users)
            {
                foreach (var result in game.RoundOver())
                {
                    string recipientId = _users[result.UserName].ConnectionId;
                    Clients.Client(recipientId).addMessage(0, "AxiomMind", $"Your guess {result.Guess} had {result.Exactly} exact match(es) and {result.Near} near match(es).");
                    if (result.Exactly == 8)
                        winners.Add(result.UserName);
                    Clients.Client(recipientId).guessResult(result.Guess, result.Near, result.Exactly);
                    if (game.HasBot)
                    {
                        SendGuessToBot(game.Round, result);
                    }
                }
            }
            if (winners.Count == 0)
                StartRound(game.Round, room);
            else
                EndGame(winners, room, game);
        }

        /// <summary>
        /// Occurs when a player that is playing in the bot makes a guess.
        /// Inform the bot of what was the player's guess, and the guess results (exact and near matches).
        /// </summary>
        /// <param name="round">Number of current round of the game.</param>
        /// <param name="result">a <c>GuessResult</c> object representing the result of a players guess.</param>
        private void SendGuessToBot(int round, GuessResult result)
        {
            AxiomBot bot = new AxiomBot();
            var rowIndex = round - 2;

            bot.SetResult(rowIndex, result);
        }
        #endregion

    }
}