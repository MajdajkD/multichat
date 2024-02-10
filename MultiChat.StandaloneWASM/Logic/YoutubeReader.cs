using MultiChat.StandaloneWASM.Logic.Model;
using MultiChat.StandaloneWASM.Logic.Model.Live;
using MultiChat.StandaloneWASM.Logic.Model.Channel;
using MultiChat.StandaloneWASM.Logic.Model.Broadcast;
using System.Net.Http.Json;
using MultiChat.StandaloneWASM.Logic.Model.Messages;

namespace MultiChat.StandaloneWASM.Logic
{
  internal class YoutubeReader(Connection conn, CancellationToken ct): IStreamSubscriber
  {
    string apiKey = conn.Password;

    string streamerHandle = conn.ChannelName;
    string channelqueryurl = "https://youtube.googleapis.com/youtube/v3/channels?part=snippet&forHandle={0}&key={1}";
    string videoqueryurl = "https://youtube.googleapis.com/youtube/v3/search?part=snippet&channelId={0}&eventType=live&type=video&key={1}";
    string broadcastqueryurl = "https://youtube.googleapis.com/youtube/v3/videos?part=liveStreamingDetails%2Csnippet&id={0}&key={1}";
    string messagequeryurl = "https://youtube.googleapis.com/youtube/v3/liveChat/messages?liveChatId={0}&part=snippet%2CauthorDetails&maxResults=1000&key={1}";
    string pagetokenparam = "&pageToken={0}";


    public event Func<ChatMessage, Task>? OnNewMessage;

    public int Quota { get; set; } = 10000;

    public async Task Start()
    {
      using HttpClient client = new();
      string? channelid = "";
      try
      {
        YTChannelSerarchResponse? channelSearchResponse = await client.GetFromJsonAsync<YTChannelSerarchResponse>(string.Format(channelqueryurl, streamerHandle, apiKey));
        channelid = channelSearchResponse?.items?.FirstOrDefault()?.id;
        Quota++;
        if (channelid == null) return;
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
      }

      bool streamFound = false;
      string? chatId = null;

      while (!streamFound)
      {
        YTLiveSearchResponse? liveSearchResponse = await client.GetFromJsonAsync<YTLiveSearchResponse>(string.Format(videoqueryurl, channelid, apiKey));
        var liveid = liveSearchResponse?.items?.FirstOrDefault()?.id?.videoId;
        Quota += 100;
        if (liveid == null)
        {
          await Task.Delay(60000);
          continue;
        }

        YTBroadcastSearchResponse? broadcastSearchResponse = await client.GetFromJsonAsync<YTBroadcastSearchResponse>(string.Format(broadcastqueryurl, liveid, apiKey));
        chatId = broadcastSearchResponse?.items?.FirstOrDefault()?.liveStreamingDetails?.activeLiveChatId;
        Quota++;
        if (chatId == null)
        {
          await Task.Delay(30000);
          continue;
        }
        else
        {
          streamFound = true;
        }
      }

      string nextPageToken = "";
      int interval = 2000;

      while (true && !ct.IsCancellationRequested)
      {
        try
        {
          var messagesUrl = string.Format(messagequeryurl, chatId, apiKey) + (nextPageToken != "" ? string.Format(pagetokenparam, nextPageToken) : "");

          YTMessagesResponse? messagesResponse = await client.GetFromJsonAsync<YTMessagesResponse>(messagesUrl);
          Quota += 5;
          nextPageToken = messagesResponse?.nextPageToken;
          if (nextPageToken == null) return;
          interval = messagesResponse?.pollingIntervalMillis ?? 5000;

          foreach (var messageitem in messagesResponse.items.Where(i => i.kind == "youtube#liveChatMessage"))
          {
            var chatMessage = new ChatMessage() { Service = StreamService.YouTube, Channel = channelid, Message = messageitem.snippet.displayMessage, User = messageitem.authorDetails.displayName };

            if (OnNewMessage != null)
              await OnNewMessage.Invoke(chatMessage);
          }
        }
        catch (Exception e)
        {
          Console.WriteLine(e);
        }

        await Task.Delay(10000);
      }
    }
  }
}
