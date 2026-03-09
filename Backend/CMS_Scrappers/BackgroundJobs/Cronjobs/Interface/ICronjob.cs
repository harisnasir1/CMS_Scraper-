namespace CMS_Scrappers.BackgroundJobs.Cronjobs.Interface;

public interface ICronjob
{
    Task ExecuteAsync(CancellationToken stoppingToken);
}