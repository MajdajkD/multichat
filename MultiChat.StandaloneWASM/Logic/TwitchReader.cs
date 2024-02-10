using MultiChat.StandaloneWASM.Logic.Model;
using System.Net.WebSockets;
using System.Text;

namespace MultiChat.StandaloneWASM.Logic
{
  internal class TwitchReader(Connection conn, CancellationToken ct) : IStreamSubscriber
  {
    readonly string password = conn.Password;
    readonly string botuser = conn.Password;
    readonly string channel = conn.ChannelName;


    public event Func<ChatMessage, Task>? OnNewMessage;

    public async Task Start()
    {
      try
      {
        var twitchUri = new Uri("wss://irc-ws.chat.twitch.tv:443");
        ClientWebSocket webSocket = new ClientWebSocket();
        await webSocket.ConnectAsync(twitchUri, ct);

        await SendString(webSocket, $"PASS {password}", ct);
        await SendString(webSocket, $"NICK {botuser}", ct);
        await SendString(webSocket, $"JOIN #{channel}", ct);

        while (true && !ct.IsCancellationRequested)
        {
          string line = await ReadString(webSocket);
          //Console.WriteLine(line);

          string[] split = line.Split(" ");
          if (line.StartsWith("PING"))
          {
            await SendString(webSocket, $"PONG {split[1]}", ct);
          }

          if (split.Length > 1 && split[1] == "PRIVMSG")
          {
            int exclamationPointPosition = split[0].IndexOf("!");
            string username = split[0].Substring(1, exclamationPointPosition - 1);
            int secondColonPosition = line.IndexOf(':', 1);
            string message = line.Substring(secondColonPosition + 1);
            string channelName = split[2].TrimStart('#');

            var chatMessage = new ChatMessage() { Service = StreamService.Twitch, Channel = channelName, Message = message, User = username };
            if (OnNewMessage != null)
              await OnNewMessage.Invoke(chatMessage);
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error: " + ex.ToString());
      }
    }


    private static Task SendString(ClientWebSocket ws, string data, CancellationToken cancellation)
    {
      var encoded = Encoding.UTF8.GetBytes(data);
      var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
      return ws.SendAsync(buffer, WebSocketMessageType.Text, true, cancellation);
    }

    private static async Task<string> ReadString(ClientWebSocket ws)
    {
      ArraySegment<byte> buffer = new(new byte[8192]);
      using var ms = new MemoryStream();

      WebSocketReceiveResult result;
      do
      {
        result = await ws.ReceiveAsync(buffer, CancellationToken.None);
        ms.Write(buffer.Array, buffer.Offset, result.Count);
      }
      while (!result.EndOfMessage);

      ms.Seek(0, SeekOrigin.Begin);

      using var reader = new StreamReader(ms, Encoding.UTF8);
      return reader.ReadToEnd();
    }


  }
}
