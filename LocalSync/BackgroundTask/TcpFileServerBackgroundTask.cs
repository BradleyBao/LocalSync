using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LocalSync.Modules;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using System.Runtime.InteropServices;

namespace LocalSync.BackgroundTask
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("F4EFAAE6-788A-4431-96C9-D5A8ABC881BE")]
    [ComSourceInterfaces(typeof(IBackgroundTask))]
    public sealed class TcpFileServerBackgroundTask : IBackgroundTask
    {
        private int _tcpPort;
        private int _discoveryPort;
        private TcpListener _listener;
        public string _serverNickname;
        public string _serverIp;
        public List<OtherComputersGrid> _discoveredDevices = new List<OtherComputersGrid>();
        public event Action DevicesUpdated;
        private BackgroundTaskDeferral _deferral;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // 获取任务的deferred，防止任务被系统终止
            _deferral = taskInstance.GetDeferral();

            // 获取触发器传递的参数
            if (taskInstance.TriggerDetails is ApplicationTriggerDetails details && details.Arguments is ValueSet args)
            {
                _tcpPort = (int)args["TcpPort"];
                _discoveryPort = (int)args["DiscoveryPort"];
                _serverNickname = (string)args["ServerNickname"];
            }

            _serverIp = GetLocalIPAddress();
            _ = StartAsync(); // 启动服务器
        }

        private async Task StartAsync()
        {
            try
            {
                // 启动TCP服务器
                _listener = new TcpListener(IPAddress.Parse(_serverIp), _tcpPort);
                _listener.Start();
                Console.WriteLine($"TCP服务器已启动，监听端口: {_tcpPort}");

                // 启动UDP发现服务器
                _ = StartUdpDiscoveryServerAsync();

                while (true)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client);
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    Console.WriteLine("TCP服务器已经在运行中。");
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            Console.WriteLine("客户端已连接。");
            NetworkStream networkStream = client.GetStream();

            using (var fileStream = new FileStream(@"C:\SharedFolder\ReceivedFile.txt", FileMode.Create, FileAccess.Write))
            {
                await networkStream.CopyToAsync(fileStream);
            }

            Console.WriteLine("文件已接收。");
            client.Close();
        }

        private async Task StartUdpDiscoveryServerAsync()
        {
            using (UdpClient udpClient = new UdpClient(_discoveryPort))
            {
                Console.WriteLine("UDP 发现服务器已启动...");

                while (true)
                {
                    UdpReceiveResult result = await udpClient.ReceiveAsync();
                    IPAddress senderIpAddress = result.RemoteEndPoint.Address;
                    string senderIpString = senderIpAddress.ToString();
                    string receivedData = Encoding.UTF8.GetString(result.Buffer);
                    Console.WriteLine($"收到消息: {receivedData}");

                    if (receivedData.StartsWith("DISCOVER_SERVER"))
                    {
                        string[] parts = receivedData.Split(':');
                        if (parts.Length == 4 && int.TryParse(parts[2], out int tcpPort))
                        {
                            string clientServerIP = parts[1];
                            string clientServerNickname = parts[3];
                            string responseMessage = $"SERVER_RESPONSE:{_serverIp}:{_tcpPort}:{_serverNickname}";
                            byte[] responseData = Encoding.UTF8.GetBytes(responseMessage);
                            await udpClient.SendAsync(responseData, responseData.Length, result.RemoteEndPoint);
                            Console.WriteLine("已响应客户端。");

                            if (!clientServerIP.Equals(_serverIp))
                            {
                                OtherComputersGrid newDevice = new OtherComputersGrid(clientServerNickname, clientServerIP);
                                _discoveredDevices.Add(newDevice);
                            }
                        }
                    }
                    else if (receivedData.StartsWith("HEARTBEAT"))
                    {
                        string[] parts = receivedData.Split(':');
                        if (parts.Length == 3)
                        {
                            string deviceIp = parts[1];
                            string deviceName = parts[2];
                            Console.WriteLine($"心跳消息 - IP: {deviceIp}, 名称: {deviceName}");

                            OtherComputersGrid existingDevice = _discoveredDevices.FirstOrDefault(d => d.deviceIP == deviceIp);
                            if (existingDevice != null)
                            {
                                existingDevice.deviceName = deviceName;
                                existingDevice.LastHeartbeat = DateTime.Now;
                            }
                            else
                            {
                                OtherComputersGrid newDevice = new OtherComputersGrid(deviceName, deviceIp);
                                _discoveredDevices.Add(newDevice);
                            }
                        }
                    }
                    else if (receivedData.StartsWith("CLOSE_REQUEST"))
                    {
                        string[] parts = receivedData.Split(':');
                        if (parts.Length == 3)
                        {
                            string deviceIp = parts[1];
                            string deviceName = parts[2];
                            Console.WriteLine($"设备关闭请求 - IP: {deviceIp}, 名称: {deviceName}");

                            OtherComputersGrid deviceToRemove = _discoveredDevices.FirstOrDefault(d => d.deviceIP == deviceIp && d.deviceName == deviceName);
                            if (deviceToRemove != null)
                            {
                                _discoveredDevices.Remove(deviceToRemove);
                            }

                            byte[] ackData = Encoding.UTF8.GetBytes("CLOSE_ACK");
                            await udpClient.SendAsync(ackData, ackData.Length, result.RemoteEndPoint);
                        }
                    }
                }
            }
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("没有找到IPv4地址。");
        }

        public bool IsRunning()
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(IPAddress.Loopback, _tcpPort);
                    return true;
                }
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}
