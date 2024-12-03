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

   public override async Task OnConnectedAsync()
{
    await Clients.Caller.SendAsync("ReceiveMessageHistory", messages); // Send history to the new client

    await Clients.Caller.SendAsync("ReceiveMessage", "System", "Connected to chat server!");// Notify the new client that they are connected

        string joinMessage = $"Client {Context.ConnectionId} has joined the chat!";// Send the "Client {ConnectionId} has joined" message to all clients
        await Clients.All.SendAsync("ReceiveJoinMessage", joinMessage);

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
