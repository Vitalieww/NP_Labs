using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace lab_2.Protocol
{
    // The Envelope that wraps every message sent over the wire
    public class NetworkMessage
    {
        [JsonProperty("t")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("p")]
        public JToken? Payload { get; set; }
    }

    public class MessageFramer(Socket socket)
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Socket _socket = socket;
        private StringBuilder _stringBuffer = new();

        public void Disconnect()
        {
            try
            {
                if (_socket.Connected)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
            }
            catch
            {
                // Socket may already be disconnected or in invalid state
            }
            finally
            {
                _socket.Close();
            }
        }

        public async Task SendMessageAsync(NetworkMessage msg, CancellationToken cancellationToken = default)
        {
            string messageString = JsonConvert.SerializeObject(msg);
            messageString = string.Concat(messageString, "\n");
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageString);
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await _socket.SendAsync(messageBytes, cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<NetworkMessage?> ReceiveMessageAsync()
        {
            byte[] byteBuffer = new byte[1024];
            string message;
            while (true)
            {
                int index = CheckForNewLine();
                if (index != -1)
                {
                    message = _stringBuffer.ToString(0, index);
                    NetworkMessage? npMessage;
                    try
                    {
                        npMessage = JsonConvert.DeserializeObject<NetworkMessage>(message);
                        _stringBuffer = _stringBuffer.Remove(0, index + 1);
                        return npMessage;
                    }
                    catch (Exception ex) when (ex is JsonReaderException or JsonSerializationException)
                    {
                        Error invalidCommand = new()
                        {
                            ErrorMessage = "Unknown JSON!"
                        };
                        NetworkMessage networkMessage = new()
                        {
                            Type = MessageTypes.Error,
                            Payload = JToken.FromObject(invalidCommand)
                        };
                        _stringBuffer = _stringBuffer.Remove(0, index + 1);
                        await SendMessageAsync(networkMessage);
                        continue;
                    }
                }
                int bytesRead = await _socket.ReceiveAsync(byteBuffer);
                if (bytesRead == 0)
                {
                    return null;
                }
                _stringBuffer.Append(Encoding.UTF8.GetString(byteBuffer, 0, bytesRead));
                if (_stringBuffer.Length > (64 * 1024)) // 65,536 bytes)
                {
                    Error messageLenghtExceeded = new()
                    {
                        ErrorMessage = "Message too long!"
                    };
                    NetworkMessage networkMessage = new()
                    {
                        Type = MessageTypes.Error,
                        Payload = JToken.FromObject(messageLenghtExceeded)
                    };
                    await SendMessageAsync(networkMessage);
                    _stringBuffer.Remove(0, _stringBuffer.Length);
                }
            }

        }
        private int CheckForNewLine()
        {
            int index = -1;
            // Search for '\n' in the StringBuilder
            for (int i = 0; i < _stringBuffer.Length; i++)
            {
                if (_stringBuffer[i] == '\n')
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
    }

    public static class MessageTypes
    {
        public const string MenuRequest = "MENU_REQ";
        public const string MenuResponse = "MENU_RES";
        public const string OrderRequest = "ORDER_REQ";
        public const string OrderResponse = "ORDER_RES";
        public const string OrderReady = "ORDER_RED";
        public const string ProviderDelivery = "PROV_DELIV";
        public const string LowStockAlert = "LOW_STOCK";
        public const string RegisterProvider = "REG_PROV";
        public const string Error = "ERROR";

    }
    public class OrderRequest
    {
        public Dictionary<string, int> Order { get; set; } = [];
    }

    public class MenuResponse
    {
        public Dictionary<string, int> Menu { get; set; } = [];

    }

    public class OrderResponse : Error
    {
        public bool Success;
        public int OrderId;
        public int PreparationTime;
        public Dictionary<string, int> RemainingStock { get; set; } = [];
    }

    public class OrderReady
    {
        public int OrderId;
        public string OrderDoneMessage { get; set; } = string.Empty;
    }

    public class ProviderDelivery
    {
        public Dictionary<string, int> Products { get; set; } = [];

    }

    public class LowStockAlert
    {
        public Dictionary<string, int> RemainingStock { get; set; } = [];
    }

    public class Error
    {
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class RegisterProvider
    {

    }
}