// See https://aka.ms/new-console-template for more information
string CurrentDir = Directory.GetCurrentDirectory();
string ParentDir = Directory.GetParent(CurrentDir).FullName;
string filePath = Path.Combine(CurrentDir, "version.json");
if (!File.Exists(filePath)) //更新文件不存在，通常对于 beta-0.3 用户
{
    File.WriteAllText(filePath, @"
    {
    ""version"" : ""beta-0.3""
    }
    ");
}
var jsonObj = Json.JObject.Parse(File.ReadAllText(filePath));
//读取更新信息
using (HttpClient httpClient = new HttpClient())
{
    string remoteStr = await httpClient.GetStringAsync("http://files.glasscraft.org/version.json");
    var remoteJarray = Json.JArray.Parse(remoteStr);
    var remoteJsonObj = remoteJarray[0]; //最新版本
    Console.WriteLine($"当前版本为 {jsonObj["version"]}");
    if (jsonObj["version"].ToString() == remoteJsonObj["version"].ToString())
    {
        Console.WriteLine("不存在新版本，请按任意键退出！");
        Console.ReadKey();
    }
    else
    {
        Console.WriteLine($"存在新版本 {remoteJsonObj["version"]}");
        Console.WriteLine($"将为您更新游戏，请不要操作此界面！");
        Stack<Json.JObject> versionStack = new Stack<Newtonsoft.Json.Linq.JObject>(); //版本更新栈
        foreach (var item in remoteJarray)
        {
            var i = (Json.JObject)item;
            if (jsonObj["version"].ToString() != i["version"].ToString()) //未循环至当前版本号
            {
                versionStack.Push(i);
            } 
            else if(jsonObj["version"].ToString() == i["version"].ToString())
            {
                break;
            }
        }
        Json.JObject jitem;
        while (versionStack.TryPop(out jitem)) //版本低高依次出栈
        {
            if (jitem.ContainsKey("remove_files")) //更新中有删除项
            {
                foreach (var j in jitem["remove_files"])
                {
                    string toDelPath = Path.Combine(ParentDir, j.ToString());
                    if (Directory.Exists(toDelPath)) //如果是目录
                    {
                        Directory.Delete(toDelPath);
                        Console.WriteLine($"已删除 {Path.GetFileName(toDelPath)}");
                        continue;
                    }
                    if (File.Exists(toDelPath))
                    {
                        File.Delete(toDelPath);
                        Console.WriteLine($"已删除 {Path.GetFileName(toDelPath)}");
                    }

                }
            }
            if (jitem.ContainsKey("download_files")) //更新中有下载项
            {
                foreach (var j in jitem["download_files"])
                {
                    var jo = (Json.JObject)j;
                    string downloadLink = jo["download_link"].ToString();
                    string downloadPath = Path.Combine(ParentDir, jo["target_path"].ToString());
                    if (!Directory.Exists(downloadPath))
                    {
                        // Try to create the directory.
                        DirectoryInfo di = Directory.CreateDirectory(downloadPath);
                    }
                    Console.WriteLine($"正在下载 {Path.GetFileName(downloadLink)}");
                    Uri link = new Uri(downloadLink);
                    Task.Run(async () => await httpClient.DownloadFileTaskAsync(link, Path.Combine(downloadPath, Path.GetFileName(downloadLink)))).Wait();
                }
            }
            //当前版本更新完毕，写入版本号
            var tmp = new
            {
                version = jitem["version"].ToString()
            };
            File.WriteAllText(filePath, Json.JObject.FromObject(tmp).ToString());
        }
        Console.WriteLine("更新结束，请按任意键退出！您可以开始游戏了 :)");
        Console.ReadKey();
    }
}