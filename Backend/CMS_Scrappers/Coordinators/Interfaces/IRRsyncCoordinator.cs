namespace CMS_Scrappers.Coordinators.Interfaces;

public interface IRRsyncCoordinator
{
    Task<bool> Syncportal(DateTime scrapeStartedAt,string scrapername);
     Task DeleteStaleFromRRSync();
}