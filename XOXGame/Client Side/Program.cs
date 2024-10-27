using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

var port = 27001;
var ep = new IPEndPoint(IPAddress.Loopback, port);
char[,] board = new char[3, 3];
char currentPlayer = 'X';
var client = new TcpClient();

try
{
    Console.WriteLine("Attempting to connect to the server");
    client.Connect(ep);
    Console.WriteLine("Connected to the server.");

    var networkStream = client.GetStream();

    Console.Write("Enter your username: ");
    var username = Console.ReadLine();
    if (string.IsNullOrEmpty(username))
    {
        Console.WriteLine("Username cannot be empty.");
        return;
    }

    var usernameBuffer = Encoding.UTF8.GetBytes(username);
    networkStream.Write(usernameBuffer, 0, usernameBuffer.Length);

    var receiveThread = new System.Threading.Thread(() => ReceiveData(networkStream));
    receiveThread.Start();

    while (true)
    {
        Console.WriteLine("Commands: invite [user], move [row,column]");
        var input = Console.ReadLine();
        var command = input?.Split(' ');

        if (command == null || command.Length == 0)
            continue;

        switch (command[0].ToLower())
        {
            case "invite":
                if (command.Length < 2)
                {
                    Console.WriteLine("Specify the user to invite.");
                    continue;
                }

                var targetUser = command[1];
                var inviteMessage = $"invite|{targetUser}";
                SendMessage(networkStream, inviteMessage);
                break;

            case "move":
                if (command.Length < 2)
                {
                    Console.WriteLine("Specify the row and column, e.g., move 0,1.");
                    continue;
                }

                var move = command[1];
                var moveCoordinates = move.Split(',');

                if (moveCoordinates.Length != 2 ||
                    !int.TryParse(moveCoordinates[0], out int row) ||
                    !int.TryParse(moveCoordinates[1], out int col) ||
                    row < 0 || row > 2 || col < 0 || col > 2)
                {
                    Console.WriteLine("Invalid move format. Use format: move row,col within 0 to 2.");
                    continue;
                }

                if (MakeMove(row, col, currentPlayer))
                {
                    var moveMessage = $"move|{username}|{move}";
                    SendMessage(networkStream, moveMessage);

                    if (CheckForWin(board, currentPlayer))
                    {
                        Console.WriteLine($"Player {currentPlayer} wins!");
                        break;
                    }
                    else if (IsBoardFull(board))
                    {
                        Console.WriteLine("The game is a draw!");
                        break;
                    }

                    currentPlayer = currentPlayer == 'X' ? 'O' : 'X';
                }
                else
                {
                    Console.WriteLine("This cell is already taken, try again.");
                }
                break;

            default:
                Console.WriteLine("Unknown command. Available commands: invite [user], move [row,column]");
                break;
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    client.Close();
}

void ReceiveData(NetworkStream networkStream)
{
    byte[] buffer = new byte[1024];

    try
    {
        while (true)
        {
            int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
            if (bytesRead <= 0)
                break;

            var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            var command = message.Split('|');

            switch (command[0])
            {
                case "invite":
                    Console.WriteLine($"{command[1]} has invited you to play. Do you accept? (yes/no)");
                    var response = Console.ReadLine();
                 
                        var responseMessage = $"response|{command[1]}|{response}";
                        SendMessage(networkStream, responseMessage);
break;
                    

                case "start":
                            Console.WriteLine($"Game started with {command[1]}. It's your turn!");
                            break;

                        case "move":
                            Console.WriteLine($"Opponent moved to {command[1]}.");
                            UpdateBoard(command[1], currentPlayer == 'X' ? 'O' : 'X');
                            PrintBoard();
                            break;

                        case "result":
                            Console.WriteLine($"Game over! Result: {command[1]}");
                            break;

                        default:
                            Console.WriteLine("Unknown message from server.");
                            break;
                        }
                    }
            }
        
    catch (Exception ex)
    {
        Console.WriteLine($"Error receiving data: {ex.Message}");
    }
}

bool MakeMove(int row, int col, char player)
{
    if (board[row, col] == '\0')
    {
        board[row, col] = player;
        PrintBoard();
        return true;
    }
    return false;
}

void PrintBoard()
{
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            Console.Write(board[i, j] == '\0' ? '.' : board[i, j]);
            if (j < 2) Console.Write(" | ");
        }
        Console.WriteLine();
        if (i < 2) Console.WriteLine("---------");
    }
}

bool CheckForWin(char[,] board, char currentPlayer)
{
    for (int i = 0; i < 3; i++)
    {
        if (board[i, 0] == currentPlayer && board[i, 1] == currentPlayer && board[i, 2] == currentPlayer)
            return true;
    }
    for (int i = 0; i < 3; i++)
    {
        if (board[0, i] == currentPlayer && board[1, i] == currentPlayer && board[2, i] == currentPlayer)
            return true;
    }
    if (board[0, 0] == currentPlayer && board[1, 1] == currentPlayer && board[2, 2] == currentPlayer)
        return true;
    if (board[0, 2] == currentPlayer && board[1, 1] == currentPlayer && board[2, 0] == currentPlayer)
        return true;
    return false;
}

bool IsBoardFull(char[,] board)
{
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            if (board[i, j] == '\0')
                return false;
        }
    }
    return true;
}

void SendMessage(NetworkStream networkStream, string message)
{
    var buffer = Encoding.UTF8.GetBytes(message);
    networkStream.Write(buffer, 0, buffer.Length);
}

void UpdateBoard(string move, char player)
{
    var parts = move.Split(',');
    if (parts.Length == 2 && int.TryParse(parts[0], out int row) && int.TryParse(parts[1], out int col))
    {
        board[row, col] = player;
    }
}
