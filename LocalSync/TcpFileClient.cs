using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using LocalSync.Modules;
using LocalSync;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading;

public class TcpFileClient
{
    private readonly string _serverIp;
    private readonly int _tcpPort;

    public TcpFileClient(string serverIp, int tcpPort)
    {
        _serverIp = serverIp;
        _tcpPort = tcpPort;
    }

    public async Task SendFileAsync(string filePath)
    {
        TcpClient client = new TcpClient();
        await client.ConnectAsync(_serverIp, _tcpPort);
        NetworkStream networkStream = client.GetStream();

        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            await fileStream.CopyToAsync(networkStream);
        }

        Console.WriteLine("文件已发送。");
        client.Close();
    }
}

public class UdpDiscoveryClient
{
    private readonly int _discoveryPort;
    public event Action DevicesUpdated;
    public List<OtherComputersGrid> _newDevices = new List<OtherComputersGrid>();
    private DispatcherTimer _resyncTimer;
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private const int _heartbeatInterval = 5000; // 心跳间隔为5秒

    public UdpDiscoveryClient(int discoveryPort)
    {
        _discoveryPort = discoveryPort;
        StartHeartbeat();
    }

    private async void StartHeartbeat()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            await SendHeartbeatAsync();
            await Task.Delay(_heartbeatInterval);
        }
    }

    public async Task SendHeartbeatAsync()
    {
        using (UdpClient udpClient = new UdpClient())
        {
            udpClient.EnableBroadcast = true;
            IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, _discoveryPort);
            string heartbeatMessage = $"HEARTBEAT:{App._server._serverIp}:{App._server._serverNickname}";
            byte[] heartbeatData = Encoding.UTF8.GetBytes(heartbeatMessage);

            await udpClient.SendAsync(heartbeatData, heartbeatData.Length, broadcastEndPoint);
            Console.WriteLine("心跳消息已发送。");
        }
    }

    public async Task SendCloseRequestAsync(string deviceIp, string deviceName)
    {
        using (UdpClient udpClient = new UdpClient())
        {
            udpClient.EnableBroadcast = true;
            IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, _discoveryPort);
            string closeMessage = $"CLOSE_REQUEST:{deviceIp}:{deviceName}";
            byte[] closeData = Encoding.UTF8.GetBytes(closeMessage);

            bool acknowledged = false;
            int retries = 0;
            const int maxRetries = 3;
            while (!acknowledged && retries < maxRetries)
            {
                await udpClient.SendAsync(closeData, closeData.Length, broadcastEndPoint);
                Console.WriteLine("关闭请求已发送。");

                // 等待服务器确认接收
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                string responseData = Encoding.UTF8.GetString(result.Buffer);
                if (responseData == "CLOSE_ACK")
                {
                    acknowledged = true;
                    Console.WriteLine("服务器已确认关闭请求。");
                }
                else
                {
                    retries++;
                    await Task.Delay(1000); // 等待1秒后重试
                }
            }
        }
    }
}
