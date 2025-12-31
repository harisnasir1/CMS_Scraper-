using CMS_Scrappers.Services.Interfaces;
using System.Text.Json;
using System;
using System.IO;
using System.Text;
namespace CMS_Scrappers.Services.Implementations;
public class ReadWriteFiles:IFileReadWrite
{
    public async Task<Stream> ReadJsonlFileAsStreamAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("JSONL file not found", filePath);

        return new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true
        );
    }

   
    public async Task<MemoryStream>  Convert_Obj_to_stream(object data)
    {
        var stream = new MemoryStream();
        if (data==null )
        {
            return stream;
        }
        await  JsonSerializer.SerializeAsync(stream, data);
        
      
        return stream;
    }
    

    public async Task<bool> Wrtie_data(List<object> data, string name)
    {
        string path = $"/home/haris/Projects/office/CMS/Backend/CMS_Scrappers/JSONL_files/{name}.jsonl";
       
        try
        {
            using (FileStream fs=File.Create(path))
            {
                foreach (var sd in data )
                { 
                    var stream = await Convert_Obj_to_stream(sd);

                    stream.Position = 0; // IMPORTANT FIX

                    await stream.CopyToAsync(fs);
                    await fs.WriteAsync(Encoding.UTF8.GetBytes("\n"));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        return true;
    }

  
}