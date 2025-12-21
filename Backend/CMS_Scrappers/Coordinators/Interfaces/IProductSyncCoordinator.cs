namespace CMS_Scrappers.Coordinators.Interfaces;

public interface IProductSyncCoordinator
{
    Task<bool> pushProductslive(Sdata data);
}