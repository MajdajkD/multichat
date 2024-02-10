namespace MultiChat.StandaloneWASM.Logic.Model
{

  public enum StreamService
  {
    Twitch = 0,
    YouTube = 1,
    Discord = 2,
  }

  public class ChatMessage
  {
    public StreamService Service { get; set; }
    public string Channel { get; set; }
    public string User { get; set; }
    public string Message { get; set; }
  }
}
