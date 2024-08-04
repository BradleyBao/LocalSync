using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LocalSync;
public class FileTransferManager
{
    private readonly int _tcpPort;
    private readonly int _maxConcurrentTransfers;
    public string savedFolderPath = (string)App.localSettings.Values["SaveFolderPath"] ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    public FileTransferManager(int tcpPort, int maxConcurrentTransfers = 5)
    {
        _tcpPort = tcpPort;
        _maxConcurrentTransfers = maxConcurrentTransfers;
    }

    public async Task TransferFilesAndFoldersAsync(string targetIp, List<string> paths, IProgress<double> progress)
    {
        var allFiles = new List<string>();
        var rootPaths = new List<string>();

        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                allFiles.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                rootPaths.Add(Directory.GetParent(path).FullName);
            }
            else if (File.Exists(path))
            {
                allFiles.Add(path);
                rootPaths.Add(Path.GetDirectoryName(path));
            }
        }

        double totalFiles = allFiles.Count;
        int fileCount = allFiles.Count;
        double totalBytes = allFiles.Sum(file => new FileInfo(file).Length);
        double bytesTransferred = 0;
        int filesTransferred = 0;

        var tasks = new List<Task>();
        foreach (var file in allFiles)
        {
            if (tasks.Count >= _maxConcurrentTransfers)
            {
                var completedTask = await Task.WhenAny(tasks);
                tasks.Remove(completedTask);
            }

            //var transferTask = Task.Run(async () =>
            //{
            //    long fileBytesTransferred = 0;
            //    var relativePath = GetRelativePath(file, rootPaths);
            //    await TransferFileAsync(targetIp, file, relativePath, fileCount, new Progress<long>(bytes =>
            //    {
            //        fileBytesTransferred = bytes;
            //        bytesTransferred += bytes;
            //        double overallProgress = (bytesTransferred + filesTransferred) / totalBytes;
            //        progress.Report(overallProgress);
            //    }));
            //    filesTransferred++;
            //    bytesTransferred += new FileInfo(file).Length - fileBytesTransferred; // Add the remaining bytes of the file
            //    progress.Report((bytesTransferred + filesTransferred) / totalBytes);
            //});

            var transferTask = Task.Run(async () =>
            {
                long fileBytesTransferred = 0;
                var relativePath = GetRelativePath(file, rootPaths);
                await TransferFileAsync(targetIp, file, relativePath, fileCount, new Progress<long>(bytes =>
                {
                    bytesTransferred += bytes - fileBytesTransferred;
                    fileBytesTransferred = bytes;
                    double overallProgress = bytesTransferred / totalBytes;
                    progress.Report(overallProgress);
                }));
            });


            tasks.Add(transferTask);
        }

        await Task.WhenAll(tasks);
        Console.WriteLine($"所有文件已传输到 {targetIp}。");
    }

    private string GetRelativePath(string file, List<string> rootPaths)
    {
        foreach (var rootPath in rootPaths)
        {
            if (file.StartsWith(rootPath))
            {
                return Path.GetRelativePath(rootPath, file);
            }
        }
        return Path.GetFileName(file);
    }

    private async Task TransferFileAsync(string targetIp, string filePath, string relativePath, int fileCount, IProgress<long> progress)
    {
        using (TcpClient client = new TcpClient())
        {
            await client.ConnectAsync(targetIp, _tcpPort);
            using (NetworkStream networkStream = client.GetStream())
            {
                var localIpAddress = ((IPEndPoint)client.Client.LocalEndPoint).Address.ToString();

                // 发送发送方IP地址长度和IP地址
                var ipAddressBytes = Encoding.UTF8.GetBytes(App._server._serverNickname);
                var ipAddressLengthBytes = BitConverter.GetBytes(ipAddressBytes.Length);
                await networkStream.WriteAsync(ipAddressLengthBytes, 0, ipAddressLengthBytes.Length);
                await networkStream.WriteAsync(ipAddressBytes, 0, ipAddressBytes.Length);

                // 发送总文件长度
                var fileCountBytes = Encoding.UTF8.GetBytes(fileCount.ToString());
                var fileCountLengthBytes = BitConverter.GetBytes(fileCountBytes.Length);
                await networkStream.WriteAsync(fileCountLengthBytes, 0, fileCountLengthBytes.Length);
                await networkStream.WriteAsync(fileCountBytes, 0, fileCountBytes.Length);

                // 发送相对路径长度和相对路径
                var relativePathBytes = Encoding.UTF8.GetBytes(relativePath);
                var relativePathLengthBytes = BitConverter.GetBytes(relativePathBytes.Length);
                await networkStream.WriteAsync(relativePathLengthBytes, 0, relativePathLengthBytes.Length);
                await networkStream.WriteAsync(relativePathBytes, 0, relativePathBytes.Length);

                // 发送文件内容
                //using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                //{
                //    var buffer = new byte[81920];
                //    int bytesRead;
                //    long totalBytesRead = 0;

                //    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                //    {
                //        await networkStream.WriteAsync(buffer, 0, bytesRead);
                //        totalBytesRead += bytesRead;
                //        progress.Report(totalBytesRead);
                //    }
                //}


                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var buffer = new byte[81920];
                    int bytesRead;
                    long totalBytesRead = 0;
                    long fileLength = fileStream.Length; // 文件总长度

                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await networkStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                        double progressPercentage = (double)totalBytesRead / fileLength;
                        progress.Report(totalBytesRead);
                    }
                }

            }
        }

        Console.WriteLine($"文件 {filePath} 已传输到 {targetIp}。");
        App.transferring_files = false;
    }

    public async Task ReceiveFilesAsync()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, _tcpPort);
        listener.Start();
        List<string> fileTimes = new List<string>();
        
        Console.WriteLine("等待文件传输...");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(async () =>
            {
                
                string length = "";
                string senderIp = "";
                using (NetworkStream networkStream = client.GetStream())
                {

                    // 接收发送方IP地址长度
                    var ipAddressLengthBuffer = new byte[sizeof(int)];
                    await networkStream.ReadAsync(ipAddressLengthBuffer, 0, ipAddressLengthBuffer.Length);
                    int ipAddressLength = BitConverter.ToInt32(ipAddressLengthBuffer, 0);

                    // 接收发送方IP地址
                    var ipAddressBuffer = new byte[ipAddressLength];
                    await networkStream.ReadAsync(ipAddressBuffer, 0, ipAddressBuffer.Length);
                    senderIp = Encoding.UTF8.GetString(ipAddressBuffer);

                    // 接受总文件长度长度
                    var fileCountLengthBuffer = new byte[sizeof(int)];
                    await networkStream.ReadAsync(fileCountLengthBuffer, 0, fileCountLengthBuffer.Length);
                    int fileCountLength = BitConverter.ToInt32(fileCountLengthBuffer, 0);

                    // 接受总文件长度
                    var fileCountBuffer = new byte[fileCountLength];
                    await networkStream.ReadAsync(fileCountBuffer, 0, fileCountBuffer.Length);
                    length = Encoding.UTF8.GetString(fileCountBuffer);

                    // 接收相对路径长度
                    var relativePathLengthBuffer = new byte[sizeof(int)];
                    await networkStream.ReadAsync(relativePathLengthBuffer, 0, relativePathLengthBuffer.Length);
                    int relativePathLength = BitConverter.ToInt32(relativePathLengthBuffer, 0);

                    // 接收相对路径
                    var relativePathBuffer = new byte[relativePathLength];
                    await networkStream.ReadAsync(relativePathBuffer, 0, relativePathBuffer.Length);
                    var relativePath = Encoding.UTF8.GetString(relativePathBuffer);

                    var targetFilePath = GetUniqueFilePath(relativePath);

                    fileTimes.Add(targetFilePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath));

                    //using (FileStream fileStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write))
                    //{
                    //    await networkStream.CopyToAsync(fileStream);

                    //}

                    using (FileStream fileStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write))
                    {
                        var buffer = new byte[81920];
                        int bytesRead;
                        while ((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                        }
                    }

                }

                Console.WriteLine($"文件已保存到 {Path.Combine(savedFolderPath, "LocalSync Transfer")}");
                client.Close();
                _ = int.TryParse(length, out int sendTimes);
                App.mainWindow.sendNotification(senderIp, sendTimes); 


            });
            
        }
    }

    private string GetUniqueFilePath(string relativePath)
    {
        var directory = Path.Combine(savedFolderPath, "LocalSync Transfer");
        var targetFilePath = Path.Combine(directory, relativePath);
        var uniqueFilePath = targetFilePath;
        var fileIndex = 1;

        while (File.Exists(uniqueFilePath))
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(relativePath);
            var fileExtension = Path.GetExtension(relativePath);
            uniqueFilePath = Path.Combine(Path.GetDirectoryName(targetFilePath), $"{fileNameWithoutExtension}_{fileIndex}{fileExtension}");
            fileIndex++;
        }

        return uniqueFilePath;
    }
}
