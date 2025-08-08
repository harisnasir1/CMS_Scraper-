using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CMS_Scrappers.BackgroundJobs.Interfaces;

//High priority queue
public class BackgroundQueue: IHighPriorityTaskQueue{
    private readonly SemaphoreSlim _signal=new(0);
    private readonly ConcurrentQueue<Func<IServiceProvider,CancellationToken,Task>> _workitem=new(); 

    public void QueueBackgroundWorkItem(Func<IServiceProvider,CancellationToken,Task> workitem)
    {
        if(workitem==null) throw new ArgumentNullException(nameof(workitem));
         
         _workitem.Enqueue(workitem);

        _signal.Release();
    }

    public async Task <Func<IServiceProvider,CancellationToken,Task>> DequeueAsync (CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        _workitem.TryDequeue(out var workitme);
        return workitme!;
    }
}