namespace CMS_Scrappers.Data.DTO;

public class StagedUploadParameter
{
    public string name { get; set; }
    public string value { get; set; }
}

public class UserError
{
    public string Field { get; set; }
    public string Message { get; set; }
}



public class StagedUploadResult
{
    public string url { get; set; }
    public string resourceUrl { get; set; }
    public List<StagedUploadParameter> Parameters { get; set; }
}

public class BulkOperationStartResult
{
    public string Id { get; set; }
    public string Status { get; set; }
}
