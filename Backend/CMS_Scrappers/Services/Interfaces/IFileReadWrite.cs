namespace CMS_Scrappers.Services.Interfaces;

public interface IFileReadWrite
{
    public Task<MemoryStream> Convert_Obj_to_stream(object data);
    
    public Task<bool> Wrtie_data(List<object> stream, string name);
}