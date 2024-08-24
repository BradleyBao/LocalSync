using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace LocalSync.Helper
{
    public class BackgroundTaskRegistrationHelper
    {
        public static async void RegisterTcpFileServerBackgroundTask(int tcpPort, int discoveryPort, string serverNickname)
        {
            var taskRegistered = false;
            var taskName = "LocalSyncTcpFileServerBackgroundTask";

            // 检查任务是否已经注册
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == taskName)
                {
                    taskRegistered = true;
                    break;
                }
            }

            if (!taskRegistered)
            {
                var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();

                if (backgroundAccessStatus == BackgroundAccessStatus.AlwaysAllowed || backgroundAccessStatus == BackgroundAccessStatus.AllowedSubjectToSystemPolicy)
                {
                    var builder = new BackgroundTaskBuilder
                    {
                        Name = taskName,
                        TaskEntryPoint = "LocalSync.BackgroundTask.TcpFileServerBackgroundTask"
                    };

                    var trigger = new ApplicationTrigger();
                    builder.SetTrigger(trigger);

                    var task = builder.Register();

                    // 使用 ValueSet 传递参数
                    var args = new ValueSet
                        {
                            { "TcpPort", tcpPort },
                            { "DiscoveryPort", discoveryPort },
                            { "ServerNickname", serverNickname }
                        };

                    await trigger.RequestAsync(args);
                }
            }
        }

    }
}
