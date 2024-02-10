using MultiChat.StandaloneWASM.Logic.Model;

namespace MultiChat.StandaloneWASM.Logic
{
  public interface IStreamSubscriber
  {
    public event Func<ChatMessage, Task>? OnNewMessage;
  }
}
