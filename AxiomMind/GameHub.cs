using System;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace AxiomMind
{
    public class GameHub : Hub
    {
        public void Send(string name, string message)
        {
            Console.WriteLine($"{name} sent: {message}");
            // Call the broadcastMessage method to update clients.
            Clients.All.addMessage(name, message);
        }
    }
}