using System;
using System.IO; // Path
using System.Threading; // EventWaitHandle
using System.Collections.Generic; // Queue
using System.Runtime.InteropServices; // Guid, RegistrationServices
using Windows.ApplicationModel.Background; // IBackgroundTask

namespace LocalSync.BackgroundTask
{
    class ComTcpFileServerBackgroundTask
    {
        private int comRegistrationToken;
        private EventWaitHandle waitHandle;
        ComTcpFileServerBackgroundTask()
        {
            comRegistrationToken = 0;
            waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        }

        //~TcpFileServerBackgroundTaskServer()
        //{
        //    Stop();
        //}

        //public void Start()
        //{
        //    // 注册 TcpFileServerBackgroundTask 为 COM 服务器
        //    RegistrationServices registrationServices = new RegistrationServices();
        //    comRegistrationToken = registrationServices.RegisterTypeForComClients(
        //        typeof(TcpFileServerBackgroundTask),
        //        RegistrationClassContext.LocalServer,
        //        RegistrationConnectionType.MultipleUse
        //    );

        //    // 等待处理信号，保持服务器运行
        //    waitHandle.WaitOne();
        //}
    }
}
