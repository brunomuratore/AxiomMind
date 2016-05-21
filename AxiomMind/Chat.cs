﻿using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AxiomMind
{
    public class Chat : Hub
    {
        private static readonly ConcurrentDictionary<string, ChatUser> _users = new ConcurrentDictionary<string, ChatUser>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, HashSet<string>> _userRooms = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, ChatRoom> _rooms = new ConcurrentDictionary<string, ChatRoom>(StringComparer.OrdinalIgnoreCase);
        
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
                Clients.Caller.hash = user.Hash;

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

        private string GetMD5Hash(string name)
        {
            return String.Join("", MD5.Create()
                         .ComputeHash(Encoding.Default.GetBytes(name))
                         .Select(b => b.ToString("x2")));
        }

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
                    }

                    if (newUserName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        SendError("That's already your username...");
                    }

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
                                Hash = GetMD5Hash(newUserName),
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

                            Clients.Caller.hash = newUser.Hash;
                            Clients.Caller.name = newUser.Name;

                            Clients.Caller.changeUserName(oldUser, newUser);
                        }
                    }
                    else
                    {
                        SendError(String.Format("Username '{0}' is already taken!", newUserName));
                    }

                    if (string.IsNullOrEmpty(room))
                    {
                        JoinRoom(newUserName, "", "General");
                        Clients.Caller.addMessage(0, "AxiomMind", "You are in general room.");
                        Clients.Caller.addMessage(0, "AxiomMind", "Now you can create/join a game room typing \"/join roomname\".");
                        Clients.Caller.addMessage(0, "AxiomMind", "You can see already created rooms on the right menu.");
                        Clients.Caller.addMessage(0, "AxiomMind", "To start a game, please join a game room.");
                    }
                    return true;
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
                        }
                        return JoinRoom(name, room, parts[1]);
                    }
                    else
                    {
                        if (!EnsureUserAndRoom())
                            return false;
                        if (commandName.Equals("me", StringComparison.OrdinalIgnoreCase))
                        {
                            if (parts.Length == 1)
                            {
                                throw new InvalidProgramException("You what?");
                            }
                            var content = String.Join(" ", parts.Skip(1));

                            Clients.Group(room).sendMeMessage(name, content);
                            return true;
                        }
                        else if (commandName.Equals("leave", StringComparison.OrdinalIgnoreCase))
                        {
                            ChatRoom chatRoom;
                            if (_rooms.TryGetValue(room, out chatRoom))
                            {
                                chatRoom.Users.Remove(name);
                                _userRooms[name].Remove(room);

                                Clients.Group(room).leave(_users[name]);
                            }

                            Groups.Remove(Context.ConnectionId, room);

                            Clients.Caller.room = null;

                            return true;
                        }

                        SendError(String.Format("'{0}' is not a valid command.", parts[0]));
                    }
                }
            }
            return false;
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

            _userRooms[name].Add(newRoom);
            if (!chatRoom.Users.Add(name))
            {
                SendError("You're already in that room!");
            }

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
            var user = new ChatUser(newUserName, GetMD5Hash(newUserName));
            user.ConnectionId = Context.ConnectionId;
            _users[newUserName] = user;
            _userRooms[newUserName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Clients.Caller.name = user.Name;
            Clients.Caller.hash = user.Hash;
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
               
        [Serializable]
        public class ChatMessage
        {
            public string Id { get; private set; }
            public string User { get; set; }
            public string Text { get; set; }
            public ChatMessage(string user, string text)
            {
                User = user;
                Text = text;
                Id = Guid.NewGuid().ToString("d");
            }
        }

        [Serializable]
        public class ChatUser
        {
            public string ConnectionId { get; set; }
            public string Id { get; set; }
            public string Name { get; set; }
            public string Hash { get; set; }

            public ChatUser()
            {
            }

            public ChatUser(string name, string hash)
            {
                Name = name;
                Hash = hash;
                Id = Guid.NewGuid().ToString("d");
            }
        }

        public class ChatRoom
        {
            public List<ChatMessage> Messages { get; set; }
            public HashSet<string> Users { get; set; }

            public ChatRoom()
            {
                Messages = new List<ChatMessage>();
                Users = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}