using MultiChat.StandaloneWASM.Logic.Model;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MultiChat.StandaloneWASM.Logic
{
  internal class DiscordReader(Connection conn, CancellationToken ct) : IStreamSubscriber
  {
    readonly string botuser = conn.Password;
    public event Func<ChatMessage, Task>? OnNewMessage;
    public List<DiscordServer> Servers { get; set; } = new();

    public async Task Start()
    {
      string line = "";
      try
      {
        var discourduri = new Uri("wss://gateway.discord.gg/?v=10&encoding=json");
        ClientWebSocket ws = new();
        await ws.ConnectAsync(discourduri, ct);

        var message = new DiscordMessage()
        {
          op = 2,
          d = new D()
          {
            token = botuser,
            intents = 1 + 512 + 2048 + 32768, //guild create, read messages, send messages, message content
            properties = new Properties()
            {
              browser = "chrome",
              device = "chrome",
              os = "windows"
            }
          }
        };

        await SendString(ws, JsonSerializer.Serialize(message), ct);

        while (true)
        {
          line = await ReadString(ws);
          if (string.IsNullOrEmpty(line)) continue;

          var jsonDoc = JsonDocument.Parse(line);
          if (jsonDoc.RootElement.GetProperty("op").GetInt32() == 10)
          {
            int interval = jsonDoc.RootElement.GetProperty("d").GetProperty("heartbeat_interval").GetInt32();
            SendHeartbeat(ws, interval);
          }
          else if (jsonDoc.RootElement.GetProperty("t").GetString() == "MESSAGE_CREATE")
          {
            var channel = jsonDoc.RootElement.GetProperty("d").GetProperty("channel_id").GetString();

            if (Servers.Any(x => x.Channels.Any(y => y.Id == channel && y.Listening)))
            {
              var chatMessage = new ChatMessage()
              {
                Channel = jsonDoc.RootElement.GetProperty("d").GetProperty("channel_id").GetString(),
                User = jsonDoc.RootElement.GetProperty("d").GetProperty("member").GetProperty("nick").GetString() ?? jsonDoc.RootElement.GetProperty("d").GetProperty("author").GetProperty("username").GetString(),
                Message = jsonDoc.RootElement.GetProperty("d").GetProperty("content").GetString(),
                Service = StreamService.Discord
              };
              await OnNewMessage.Invoke(chatMessage);
            }
          }
          else if (jsonDoc.RootElement.GetProperty("t").GetString() == "GUILD_CREATE")
          {
            var newServer = new DiscordServer()
            {
              Id = jsonDoc.RootElement.GetProperty("d").GetProperty("id").GetString(),
              Name = jsonDoc.RootElement.GetProperty("d").GetProperty("name").GetString(),
              Channels = jsonDoc.RootElement.GetProperty("d").GetProperty("channels").EnumerateArray()
                                .Where(x => x.GetProperty("type").GetInt32() == 0)
                                .Select(x => new DiscordChannel()
                                {
                                  Id = x.GetProperty("id").GetString(),
                                  Name = x.GetProperty("name").GetString(),
                                  Listening = true
                                }).ToList()
            };
            Servers.Add(newServer);
          }
          else
          {
            Console.WriteLine(line);
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error: " + ex.ToString() + "\n\n" + line);
      }
    }


    private async Task SendHeartbeat(ClientWebSocket ws, int ms)
    {
      await Task.Delay(ms / 10);
      while (true)
      {
        Console.WriteLine("Sending HB");
        await SendString(ws, "{\"op\":1, \"d\": null}", ct);
        await Task.Delay(ms);
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


    public class DiscordServer
    {
      public string Name { get; set; }
      public string Id { get; set; }

      public List<DiscordChannel> Channels { get; set; } = new();
    }

    public class DiscordChannel
    {
      public string Name { get; set; }
      public string Id { get; set; }
      public bool Listening { get; set; }
    }

    public class DiscordMessage
    {
      public int op { get; set; }
      public D d { get; set; }
    }

    public class D
    {
      public string token { get; set; }
      public Properties properties { get; set; }
      public int intents { get; set; }
    }

    public class Properties
    {
      public string os { get; set; }
      public string browser { get; set; }
      public string device { get; set; }
    }
  }
}

