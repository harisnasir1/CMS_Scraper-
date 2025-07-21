public interface IBackgroundTaskQueue{
    void QueueBackgroundWorkItem(Func<IServiceProvider,CancellationToken,Task> workitem);
    Task <Func<IServiceProvider,CancellationToken,Task>> DequeueAsync (CancellationToken cancellationToken);

}