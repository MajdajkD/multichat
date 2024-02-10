using MultiChat.StandaloneWASM.Logic.Model;

namespace MultiChat.StandaloneWASM.Logic
{
  public class Config
  {
    public List<Connection> Connections { get; set; } = new();

    public bool RandomUserColors { get; set; } = true;
    public string BackgroundColor { get; set; } = "18181b";
    public string TextColor { get; set; } = "efeff1";
    public string BorderColor { get; set; } = "efeff1";
  }

  public class Connection
  {
    public StreamService Service { get; set; } = StreamService.Twitch;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public bool Save { get; set; } = true;
    public string ChannelName { get; set; } = "";





  }
}
