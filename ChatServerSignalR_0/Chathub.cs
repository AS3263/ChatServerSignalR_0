using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ChatHub : Hub
{
    // while running the server stores the msg
    private static List<Tuple<string, string>> messages = new List<Tuple<string, string>>();
    private const int maxMessages = 1000; // max limit of msg 1000 if exceeds deleteds automaticaly older msg

    
    /// <param name="user">Username of the sender.</param>
    /// <param name="message">Message content.</param>
    public async Task SendMessage(string user, string message)
    {
        string fullMessage = $"{user}: {message}";// msg format
        messages.Add(new Tuple<string, string>(user, fullMessage)); // stores msgs in memory

        // Enforce maximum message limit
        if (messages.Count > maxMessages)
        {
            messages = messages.Skip(messages.Count - maxMessages).ToList();
        }

        await Clients.All.SendAsync("ReceiveMessage", user, message, Context.ConnectionId);// bordcast all msg to client
        Console.WriteLine($"Message Sent: {fullMessage}");// debug
    }
    public Task<List<Tuple<string, string>>> GetAllMessages()
    { 
        return Task.FromResult(messages);// returns msg history
    }

    public async Task NotifyNewClientJoin(string username)
    {
        string notification = $"{username} has joined the chat!";
        await Clients.All.SendAsync("NotifyNewClient", notification);
    }
    public override async Task OnConnectedAsync()
    {
        // Notify the newly connected client with the chat history
        await Clients.Caller.SendAsync("ReceiveMessageHistory", messages);

        // Notify the newly connected client that they are successfully connected
        string welcomeMessage = $"Welcome to the chat, Client {Context.ConnectionId}!";
        await Clients.Caller.SendAsync("ReceiveMessage", "System", welcomeMessage);

        // Notify all other clients about the new connection
        string joinNotification = $"A new user (Client {Context.ConnectionId}) has joined the chat!";
        await Clients.Others.SendAsync("ReceiveMessage", "System", joinNotification);

        Console.WriteLine($"Client connected: {Context.ConnectionId}");

        await base.OnConnectedAsync();
    }


    /// <param name="exception">Exception that caused the disconnection, if any.</param>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Notify all clients that a client has disconnected
        string disconnectMessage = $"Client {Context.ConnectionId} has left the chat.";
        await Clients.All.SendAsync("ReceiveMessage", "System", disconnectMessage);

        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");

        if (exception != null)
        {
            Console.WriteLine($"Disconnection reason: {exception.Message}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
