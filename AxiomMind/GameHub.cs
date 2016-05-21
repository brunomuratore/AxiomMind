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
        public static readonly ConcurrentDictionary<string, ChatUser> _users = new ConcurrentDictionary<string, ChatUser>(StringComparer.OrdinalIgnoreCase);
        public static readonly ConcurrentDictionary<string, HashSet<string>> _userRooms = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        public static readonly ConcurrentDictionary<string, ChatRoom> _rooms = new ConcurrentDictionary<string, ChatRoom>(StringComparer.OrdinalIgnoreCase);
        public static readonly ConcurrentDictionary<string, Game> _games = new ConcurrentDictionary<string, Game>();

        #region Public Chat
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
                HashSet<string> rooms;
                if (_userRooms.TryGetValue(user.Name, out rooms))
                {
                    foreach (var room in rooms)
                    {
                        Clients.Group(room).leave(user);
                        ChatRoom chatRoom = _rooms[room];
                        chatRoom.Users.Remove(user.Name);
                    }
                }

                _userRooms[user.Name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Add this user to the list of users
                Clients.Caller.addUser(user);
                return true;
            }

            return false;
        }

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

        public override Task OnDisconnected(bool stopCalled)
        {
            ChatUser user = _users.Values.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);
            if (user != null)
            {
                ChatUser ignoredUser;
                _users.TryRemove(user.Name, out ignoredUser);

                // Leave all rooms
                HashSet<string> rooms;
                if (_userRooms.TryGetValue(user.Name, out rooms))
                {
                    foreach (var room in rooms)
                    {
                        Clients.Group(room).leave(user);
                        ChatRoom chatRoom = _rooms[room];
                        chatRoom.Users.Remove(user.Name);
                    }
                }
                UpdateRooms();

                HashSet<string> ignoredRoom;
                _userRooms.TryRemove(user.Name, out ignoredRoom);
            }

            return null;
        }

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
        public void Start()
        {
            if (!EnsureUserAndRoom())
            {
                SendError("You need to have a nickName to start a game.");
                return;
            }

            string roomName = Clients.Caller.room;
            string name = Clients.Caller.name;

            if (_rooms[roomName].HasGame)
            {
                SendError("You can't start a game here. This room already has a game in progress.");
                return;
            }

            if (!String.IsNullOrEmpty(_users[name].CurrentGame))
            {
                SendError("Game could not be started. You are already in a game.");
                return;
            }

            Game game = new Game(_rooms[roomName].Users);

            if (!_games.TryAdd(game.Guid, game))
            {
                SendError("Game could not be started. Please try again.");
                return;
            }                

            foreach(var user in _rooms[roomName].Users)
            {
                _users[user].CurrentGame = game.Guid;
            }

            _rooms[roomName].HasGame = true;
            Clients.Group(roomName).addMessage(0, "AxiomMind", $"User {name} has started a game for {_rooms[roomName].Users.Count()} users.");
            StartRound(game.Round, roomName);
        }

        #endregion

        #region Private Chat

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
                }
            }
            return false;
        }
        
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
                    _userRooms[newUserName] = new HashSet<string>(_userRooms[name]);

                    if (_userRooms[name].Any())
                    {
                        foreach (var r in _userRooms[name])
                        {
                            _rooms[r].Users.Remove(name);
                            _rooms[r].Users.Add(newUserName);
                            Clients.Group(r).changeUserName(oldUser, newUser);
                        }
                    }
                    HashSet<string> ignoredRoom;
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
                Clients.Caller.addMessage(0, "AxiomMind", "Now you can create/join a game room typing \"/join roomname\".");
                Clients.Caller.addMessage(0, "AxiomMind", "You can see already created rooms on the right menu.");
                Clients.Caller.addMessage(0, "AxiomMind", "To start a game, please click start game.");
            }
            return true;
        }

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
                _userRooms[name].Remove(room);
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

            _userRooms[name].Add(newRoom);
            chatRoom.Users.Add(name);

            Clients.Group(newRoom).addUser(_users[name]);

            // Set the room on the caller
            Clients.Caller.room = newRoom;

            Groups.Add(Context.ConnectionId, newRoom);

            Clients.Caller.refreshRoom(newRoom);
            UpdateRooms();

            return true;
        }

        private ChatUser AddUser(string newUserName)
        {
            var user = new ChatUser(newUserName);
            user.ConnectionId = Context.ConnectionId;
            _users[newUserName] = user;
            _userRooms[newUserName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Clients.Caller.name = user.Name;
            Clients.Caller.id = user.Id;

            Clients.Caller.addUser(user);

            return user;
        }

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

            HashSet<string> rooms;
            if (!_userRooms.TryGetValue(name, out rooms) || !rooms.Contains(room))
            {
                SendError(String.Format("You're not in '{0}'. Use '/join {0}' to join it.", room));
                return false;
            }

            return true;
        }

        private void SendError(string errorMessage)
        {
            Clients.Caller.addMessage(0, "AxiomMind", errorMessage);
        }

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

        private void StartRound(int roundNumber, string roomName)
        {
            Clients.Group(roomName).addMessage(0, "AxiomMind", $"");
            Clients.Group(roomName).addMessage(0, "AxiomMind", $"**** ROUND {roundNumber} started ****");
            Clients.Group(roomName).addMessage(0, "AxiomMind", $"Make your guess typing \"/guess 12345678\"");
        }

        private void Guess(string name, string room, string guess)
        {
            if (guess.Length != 8)
            {
                SendError("Your guess must have 8 characters");
                return;
            }

            byte[] g = new byte[8];

            for (int i = 0; i < guess.Length; i++)
            {
                if (guess[i] - '0' <= 0 || guess[i] - '0' > 8)
                {
                    SendError("Your guess must contain only digitis from 1 to 8.");
                    return;
                }

                g[i] = (byte)(guess[i] - '0');
            }

            var gameId = _users[name].CurrentGame;

            if (String.IsNullOrEmpty(gameId))
            {
                SendError("You are not in a game.");
                return;
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
                SendError("You have already sent your guess this round. Please wait for the other playsers.");
        }

        private void EndGame(List<string> winners, string room, Game game)
        {
            _games.TryRemove(game.Guid, out game);

            foreach (var user in _rooms[room].Users)
            {
                _users[user].CurrentGame = "";
            }

            _rooms[room].HasGame = false;

            Clients.Group(room).addMessage(0, "AxiomMind", $"**** END OF GAME ****");
            if(winners.Count > 0)
                Clients.Group(room).addMessage(0, "AxiomMind", $"Our winner{(winners.Count > 1 ? "s" : "")} {(winners.Count > 1 ? "are" : "is")}: {string.Join<string>(" and ", winners)}");
        }

        private void LeaveGame(string name, string room)
        {
            var gameId = _users[name].CurrentGame;
            if (String.IsNullOrEmpty(gameId))
            {
                SendError("You are not in a game.");
                return;
            }

            _games[gameId].Removeuser(name);
            if(!_games[gameId].HasUsers())
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


        private void EndRound(Game game, string room)
        {
            List<string> winners = new List<string>();
            foreach (var result in game.RoundOver())
            {
                string recipientId = _users[result.UserName].ConnectionId;
                Clients.Client(recipientId).addMessage(0, "AxiomMind", $"Your guess {result.Guess} had {result.Exactly} exact match(es) and {result.Near} near match(es).");
                if (result.Exactly == 8)
                    winners.Add(result.UserName);
            }
            if (winners.Count == 0)
                StartRound(game.Round, room);
            else
                EndGame(winners, room, game);
        }
        #endregion

    }
}