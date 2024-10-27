using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    private static TcpListener listener;
    private static Dictionary<string, TcpClient> connectedClients = new Dictionary<string, TcpClient>();
    private static Dictionary<string, string> activeGames = new Dictionary<string, string>();
    private static Dictionary<string, char[,]> gameBoards = new Dictionary<string, char[,]>();
    private static Dictionary<string, char> currentPlayer = new Dictionary<string, char>();

    static void Main(string[] args)
    {
        listener = new TcpListener(IPAddress.Loopback, 27001);
        listener.Start();
        Console.WriteLine("Server started");

        while (true)
        {
            var client = listener.AcceptTcpClient();
            Console.WriteLine("Client connected.");

            var clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    static void HandleClient(TcpClient client)
    {
        var networkStream = client.GetStream();
        var buffer = new byte[1024];
        int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
        var username = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

        if (connectedClients.ContainsKey(username))
        {
            Console.WriteLine($"Username {username} is already in use.");
            client.Close();
            return;
        }

        connectedClients[username] = client;
        Console.WriteLine($"{username} has joined.");

        try
        {
            while (true)
            {
                bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                var command = message.Split('|');
                if (command[0] == "invite")
                {
                    HandleInvite(username, command[1]);
                }
                else if (command[0] == "response")
                {
                    HandleInviteResponse(username, command[1], command[2]);
                }
                else if (command[0] == "move")
                {
                    HandleMove(username, command[2]);
                }
                else
                {
                    Console.WriteLine("Unknown command received.");
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error with {username}: {ex.Message}");
        }
        finally
        {
            connectedClients.Remove(username);
            client.Close();
            Console.WriteLine($"{username} has disconnected.");
        }
    }

    static void HandleInvite(string fromUser, string toUser)
    {
        if (connectedClients.ContainsKey(toUser))
        {
            var inviteMessage = $"invite|{fromUser}";
            SendMessage(toUser, inviteMessage);
            Console.WriteLine($"{fromUser} invited {toUser} to a game.");
        }
        else
        {
            Console.WriteLine($"User {toUser} not found.");
            SendMessage(fromUser, $"result|User {toUser} is not online.");
        }
    }

    static void HandleInviteResponse(string fromUser, string toUser, string response)
    {
        if (connectedClients.ContainsKey(toUser))
        {
            Console.WriteLine($"Received response '{response}' from {fromUser} for invitation to {toUser}.");
            if (response.Trim().ToLower() == "yes")
            {
                var gameKey = $"{fromUser}|{toUser}";
                activeGames[fromUser] = toUser;
                activeGames[toUser] = fromUser;
                gameBoards[gameKey] = new char[3, 3]; 
                currentPlayer[gameKey] = 'X';

                SendMessage(fromUser, $"start|{toUser}");
                SendMessage(toUser, $"start|{fromUser}");
                Console.WriteLine($"Game started between {fromUser} and {toUser}.");
            }
            else
            {
                SendMessage(toUser, $"result|{fromUser} declined the invitation.");
                SendMessage(fromUser, $"result|You declined the invitation from {toUser}.");
                Console.WriteLine($"{fromUser} declined the invitation from {toUser}.");
            }
        }
        else
        {
            Console.WriteLine($"User {toUser} not found for invitation response.");
            SendMessage(fromUser, $"result|User {toUser} is not online.");
        }
    }

    static void SendMessage(string toUser, string message)
    {
        if (connectedClients.TryGetValue(toUser, out var client))
        {
            try
            {
                var networkStream = client.GetStream();
                var buffer = Encoding.UTF8.GetBytes(message);
                networkStream.Write(buffer, 0, buffer.Length);
                Console.WriteLine($"Sent message to {toUser}: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to {toUser}: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"User {toUser} is not connected.");
        }
    }



    static void HandleMove(string player, string move)
    {
        if (!activeGames.ContainsKey(player)) return;

        var opponent = activeGames[player];
        var gameKey = $"{player}|{opponent}";
        if (!gameBoards.ContainsKey(gameKey)) gameKey = $"{opponent}|{player}";

        var board = gameBoards[gameKey];
        var parts = move.Split(',');
        if (parts.Length != 2 || !int.TryParse(parts[0], out int row) || !int.TryParse(parts[1], out int col)) return;

        if (board[row, col] == '\0')
        {
            var playerSymbol = currentPlayer[gameKey];
            board[row, col] = playerSymbol;

            SendMessage(player, $"move|{move}");
            SendMessage(opponent, $"move|{move}");

            if (CheckForWin(board, playerSymbol))
            {
                SendMessage(player, "result|You win!");
                SendMessage(opponent, "result|You lose.");
                Console.WriteLine($"Game over. {player} wins.");
                EndGame(player, opponent, gameKey);
            }
            else if (IsBoardFull(board))
            {
                SendMessage(player, "result|Draw.");
                SendMessage(opponent, "result|Draw.");
                Console.WriteLine("Game over. It's a draw.");
                EndGame(player, opponent, gameKey);
            }
            else
            {
                currentPlayer[gameKey] = playerSymbol == 'X' ? 'O' : 'X';
            }
        }
        else
        {
            SendMessage(player, "result|Invalid move, try again.");
        }
    }

   

    static bool CheckForWin(char[,] board, char player)
    {
        for (int i = 0; i < 3; i++)
        {
            if (board[i, 0] == player && board[i, 1] == player && board[i, 2] == player) return true;
            if (board[0, i] == player && board[1, i] == player && board[2, i] == player) return true;
        }
        if (board[0, 0] == player && board[1, 1] == player && board[2, 2] == player) return true;
        if (board[0, 2] == player && board[1, 1] == player && board[2, 0] == player) return true;
        return false;
    }

    static bool IsBoardFull(char[,] board)
    {
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                if (board[i, j] == '\0') return false;
        return true;
    }

    static void EndGame(string player1, string player2, string gameKey)
    {
        activeGames.Remove(player1);
        activeGames.Remove(player2);
        gameBoards.Remove(gameKey);
        currentPlayer.Remove(gameKey);
    }
}
