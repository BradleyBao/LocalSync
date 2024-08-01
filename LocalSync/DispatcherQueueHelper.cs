using Microsoft.UI.Dispatching;

public class DispatcherQueueHelper
{
    private DispatcherQueue _dispatcherQueue;
    private Microsoft.UI.Dispatching.DispatcherQueueController _dispatcherQueueController;

    public void EnsureDispatcherQueue()
    {
        if (DispatcherQueue.GetForCurrentThread() != null)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }
        else
        {
            _dispatcherQueueController = Microsoft.UI.Dispatching.DispatcherQueueController.CreateOnDedicatedThread();
            _dispatcherQueue = _dispatcherQueueController.DispatcherQueue;
        }
    }

    public DispatcherQueue DispatcherQueue => _dispatcherQueue;
}
