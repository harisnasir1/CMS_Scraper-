public interface IScrapperRepository{
    Task<bool> Startrun(string name);
    Task <bool> Stoprun(string timetake,string name);
    Task Storerrors(string name,string e);
    Task<Guid> Giveidbyname(string name);
    Task<List<Scrapper>> Getallscrapers();
    Task<string> Get_Status_by_id(Guid id);
}