using Microsoft.JSInterop;

namespace MultiChat.StandaloneWASM.Logic.Interop
{
  public class LocalStorageJsInterop
  {
    private readonly IJSRuntime _jsRuntime;

    public LocalStorageJsInterop(IJSRuntime jsRuntime) =>
      _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    public async Task save(string itemId, string itemValue)
    {
      await _jsRuntime.InvokeVoidAsync("localStorage.setItem", itemId, itemValue);
    }

    public async Task<string> read(string itemId)
    {
      return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", itemId);
    }

    public async Task delete(string itemId)
    {
      await _jsRuntime.InvokeAsync<string>("localStorage.removeItem", itemId);
    }
  }
}
