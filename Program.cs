using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Threading;
using CG.Web.MegaApiClient;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CopyCloud
{
    //dotnet publish -c Release -r win10-x64
    class Program
    {
        static string AppName = Regex.Match(AppDomain.CurrentDomain.FriendlyName, @"[^\.]+").Value;
        static string PathCurrent = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\";
        static string cur = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\";

        static void Main(string[] args)
        {
            try
            {
                string login = "alexxx.cloud7@yandex.ru";
                string pass = "Aqwsxz13";
                string pathAccount = PathCurrent + "account.json";

                var api = new MegaApiClient();
                var token = api.Login(login, pass);
                var info = api.GetAccountInformation();
                Console.WriteLine("Всего: {0} Гб", info.TotalQuota / 1024 / 1024 / 1024);
                Console.WriteLine("Свободно: {0} Гб", Math.Round((info.TotalQuota - info.UsedQuota) / 1024d / 1024d / 1024d, 2));
                if (info.UsedQuota >= info.TotalQuota)
                {
                    Console.WriteLine("Диск заполнен");
                    Console.Read();
                }
                else
                {
                    string path = @"d:\Backup\Projects\";
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    var fis = new DirectoryInfo(path).GetFiles().OrderBy(x => x.LastWriteTime).ToList();

                    string pathDate = PathCurrent + "date.txt";
                    var d = File.Exists(pathDate) ? DateTime.Parse(File.ReadAllText(pathDate)) : DateTime.Now.AddDays(-1);

                    var dirs = new List<string>();
                    dirs.AddRange(HandleDir(@"d:\Projects\Unity", d));
                    dirs.AddRange(HandleDir(@"d:\Projects\Freelance", d));
                    dirs.AddRange(HandleDir(@"d:\Projects\Freelance\Totals", d));
                    dirs.AddRange(HandleDir(@"d:\Projects\Me", d));
                    dirs.AddRange(HandleDir(@"d:\Projects\_\", d));
                    if (dirs.Count == 0) return;
                    foreach (var dir in dirs)
                    {
                        Console.WriteLine("Добавление в архив {0}...", dir);
                        var ps = new ProcessStartInfo();
                        ps.FileName = @"C:\Program Files\WinRAR\rar.exe";
                        if (dir.Contains(@"\Unity"))
                        {
                            File.WriteAllLines($"{dir}\\Assets\\list.txt", Directory.GetDirectories($"{dir}\\Assets"));
                            var s = File.ReadAllLines($"{cur}\\in.txt");
                            for (int i = 0; i < s.Length; i++) s[i] = dir + s[i];
                            File.WriteAllLines($"{cur}\\_in.txt", s);
                            ps.Arguments = $"a -r -m5 \"d:\\Projects.rar\" @\"{cur}\\_in.txt\"";
                        }
                        else ps.Arguments = $"a -r -m5 -x@\"{cur}\\ex.txt\" \"d:\\Projects.rar\" \"{dir}\"";
                        ps.UseShellExecute = false;
                        ps.CreateNoWindow = true;
                        var process = Process.Start(ps);
                        process.WaitForExit();
                    }

                    d = DateTime.Now;
                    string name = string.Format("{0}{1:00}{2:00} {3}", d.Year, d.Month, d.Day, string.Join(",", dirs.Select(x => Regex.Match(x, @"[^\\]+$"))));
                    if (name.Length > 200) name = name.Substring(0, 200);
                    string dest = path + name + ".rar";
                    string source = @"d:\Projects.rar";
                    if (File.Exists(source))
                    {
                        Copyfiles(source, dest, ShowPercentProgress);
                        File.Delete(source);
                    }

                    string message = string.Format("Копирование в облако... ");
                    var node = api.GetNodes().SingleOrDefault(x => x.Type == NodeType.Root);
                    var progress = new Progress<double>();
                    progress.ProgressChanged += (s, progressValue) =>
                    {
                        if (progressValue < 100) ShowPercentProgress(message, Convert.ToInt32(Math.Floor(progressValue)), 100);
                    };
                    api.UploadFileAsync(dest, node, progress).Wait();

                    File.WriteAllText(pathDate, d.ToString("g"));
                }
            }
            catch (Exception ex) { Console.WriteLine(ex); Console.Read(); }
        }

        static double GetDirectorySize(string path)
        {
            double result = 0;

            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                var fi = new FileInfo(file);
                result += fi.Length;
            }

            return result;
        }

        static void GetDirectoryName(string path, XElement p)
        {
            try
            {
                var dd = new DirectoryInfo(path).GetDirectories();
                foreach (var d in dd)
                {
                    var c = new XElement("folder", new XAttribute("name", d.Name));
                    GetDirectoryName(d.FullName, c);
                    p.Add(c);
                }

                var ff = new DirectoryInfo(path).GetFiles();
                foreach (var f in ff)
                {
                    var c = new XElement("file", new XAttribute("name", f.Name));
                    p.Add(c);
                }
            }
            catch { }
        }

        static List<string> HandleDir(string name, DateTime d)
        {
            var r = new List<string>();
            var exdirs = new string[] { "Totals", "Discovery", "Архив", "ТЗ" };
            var exfulldirs = File.ReadAllLines($"{cur}\\ex.txt").Select(x => x.Replace("*", "")).ToArray();

            try
            {
                var dirs = new DirectoryInfo(name).GetDirectories();
                foreach (var dir in dirs)
                {
                    if (exdirs.Contains(dir.Name)) continue;
                    var files = dir.GetFiles("*.*", SearchOption.AllDirectories).Where(x => x.LastWriteTime > d && !exfulldirs.Any(z => x.FullName.Contains(z))).ToList();
                    if (files.Count > 0)
                    {
                        if (files.Count == 1 && files[0].Name == ".suo") continue;
                        r.Add(dir.FullName);
                    }
                }
            }
            catch { }

            return r;
        }

        static void Copyfiles(string source, string dest, Action<string, long, long> reportProgress, int blockSizeToRead = 4096)
        {
            FileInfo sourceFileInfo = new FileInfo(source);
            string message = string.Format("Копирование файла... ", source, dest);
            byte[] buffer = new byte[blockSizeToRead];
            using (var destfs = File.OpenWrite(dest))
            {
                using (var sourcefs = File.OpenRead(source))
                {
                    int bytesRead, totalBytesRead = 0;
                    while ((bytesRead = sourcefs.Read(buffer, 0, buffer.Length - 1)) > 0)
                    {
                        destfs.Write(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                        if (reportProgress != null)
                        {
                            reportProgress(message, totalBytesRead, sourceFileInfo.Length);
                        }
                    }
                }
            }
        }

        static void ShowPercentProgress(string message, long processed, long total)
        {

            long percent = (100 * (processed + 1)) / total;
            Console.Write("\r{0}{1}%", message, percent);
            /*if (processed >= total - 1)
            {
                Console.WriteLine(Environment.NewLine);
            }*/
        }
    }
}
