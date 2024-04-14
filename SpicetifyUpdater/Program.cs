using System.Diagnostics;
using System.Text.Json;
using Pastel;

namespace SpicetifyUpdater;

public abstract class Vars {
    public const string Version = "3.0.0";
    public static bool IsDebug { get; set; }
    public static bool IsWindows => Environment.OSVersion.ToString().Contains("windows", StringComparison.CurrentCultureIgnoreCase);
    public const string UserAgent = "SpicetifyUpdater/" + Version;
}

public static class Program {
    private static string? _lastOutput;
    
    public static void Main(string[]? args) {
        Vars.IsDebug = Environment.CommandLine.Contains("--debug");
#if DEBUG
        Vars.IsDebug = true;
#endif
        Console.Title = "Spicetify Updater v" + Vars.Version;
        start:
        var firstTimeUpdateCheck = IsSpicetifyUpToDate();
        Console.WriteLine("Spicetify Updater v" + Vars.Version.Pastel("00FFFF"));
        Console.WriteLine();
        Console.WriteLine("Choose an option to get started");
        if (IsSpicetifyInstalled) {
            Console.WriteLine("1. " + "Reinstall".Pastel("7AB8D8") + " Spicetify");
            if (firstTimeUpdateCheck)
                Console.WriteLine("2. " + "Update Spicetify".Pastel("696F75"));
            Console.WriteLine("3. " + "Uninstall".Pastel("FDAD94") + " Spicetify");
        }
        else {
            Console.WriteLine("1. " + "Install".Pastel("7AB8D8") + " Spicetify");
            Console.WriteLine("2. " + "Update Spicetify".Pastel("696F75"));
            Console.WriteLine("3. " + "Uninstall Spicetify".Pastel("696F75"));
        }
        Console.WriteLine("4. Exit");
        Console.WriteLine("i.".Pastel("ffff00") + " Information, Credits, and License");
        Console.WriteLine();
        Console.Write("Enter an option: ");
        var input = Console.ReadKey();
        
        switch (input.KeyChar) {
            case '1':
                var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", Vars.UserAgent);
                var output = http.GetByteArrayAsync("https://raw.githubusercontent.com/spicetify/spicetify-cli/master/install.ps1").GetAwaiter().GetResult();
                var ps1File = Path.Combine(Path.GetTempPath(), "SpicetifyUpdater", "install_spicetify.ps1");
                File.WriteAllBytes(ps1File, output);
                var pStartInfo = new ProcessStartInfo {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy unrestricted -File \"{ps1File}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                var p = Process.Start(pStartInfo);
                var standardOutput = p!.StandardOutput.ReadToEnd();
                // var standardError = p.StandardError.ReadToEnd();
                Console.WriteLine(standardOutput);
                Console.WriteLine();
                goto start;
            case '2':
                SpicetifyCmd("update");
                if (IsSpicetifyUpToDate()) 
                    Console.WriteLine("Spicetify is up to date".Pastel("00FF00"));
                goto start;
            case '3':
                if (!firstTimeUpdateCheck) {
                    Console.WriteLine("Spicetify is not installed".Pastel("FF0000"));
                    goto start;
                }
                SpicetifyCmd("restore");
                var spicetifyAppData = Vars.IsWindows ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "spicetify") : "~/.spicetify";
                Directory.Delete(spicetifyAppData, true);
                var spicetifyLocalAppData = Vars.IsWindows ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "spicetify") : "~/.config/spicetify";
                Directory.Delete(spicetifyLocalAppData, true);
                Console.WriteLine("Spicetify uninstalled successfully".Pastel("00FF00"));
                goto start;
            case '4':
                Environment.Exit(0);
                break;
            case 'i':
                Console.Clear();
                Console.WriteLine("Spicetify Updater v" + Vars.Version.Pastel("00FFFF"));
                Console.WriteLine();
                Console.WriteLine("About:".Pastel("ffff00"));
                Console.WriteLine("This program is a simple updater for " + "Spicetify CLI.");
                Console.WriteLine("It is written in C# and uses the " + ".NET 8.0".Pastel("00FF00") + " framework.");
                Console.WriteLine();
                Console.WriteLine("Credits:".Pastel("ffff00"));
                Console.WriteLine("Spicetify CLI: " + "Spicetify Team".Pastel("EE6533") + " - " + "https://github.com/spicetify/".Pastel("ff00ff"));
                Console.WriteLine("Spicetify Updater: " + "MintLily".Pastel("9fffe3"));
                Console.WriteLine("Original Program Creator: " + "BaranDev".Pastel("E0F2FC") + " - " + "https://github.com/BaranDev/spicetify-updater".Pastel("ff00ff"));
                Console.WriteLine("\tTheir current version of the program is now written in Python, no longer in C#.\n" +
                                  "\tI am here to maintain a C# version, and I am not affiliated with the original creator.\n" +
                                  "\tI have chosen to remove BaranDev's 'Littleroot Town.wav' media and ASCII art from the program.");
                Console.WriteLine();
                Console.WriteLine("License:".Pastel("ffff00"));
                Console.WriteLine("This program is licensed under the " + "MIT License.".Pastel("00FF00"));
                Console.WriteLine("You can view the full license at " + "https://github.com/MintLily/spicetify-updater/blob/master/LICENSE".Pastel("ff00ff"));
                Console.WriteLine();
                goto start;
            default:
                Console.WriteLine("Invalid option");
                goto start;
        }
    }
    
    private static bool IsSpicetifyInstalled => Directory.Exists(Vars.IsWindows ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "spicetify") : "~/.spicetify");

    private static bool IsSpicetifyUpToDate(bool showResult = true) {
        var http = new HttpClient();
        Api apiResponseJson;
        try {
            http.DefaultRequestHeaders.Add("User-Agent", Vars.UserAgent);
            var output = http.GetStringAsync("https://api.github.com/repos/spicetify/spicetify-cli/releases/latest").GetAwaiter().GetResult();
            apiResponseJson = JsonSerializer.Deserialize<Api>(output)!;
        }
        catch (Exception e) {
            Console.WriteLine("An error occurred while checking for Spicetify's GitHub API response.".Pastel("ff0000"));
            Console.WriteLine(e.Message.Pastel("ff0000"));
            Console.WriteLine(e.StackTrace.Pastel("ff0000"));
            return false;
        }
        var latestVersion = apiResponseJson?.tag_name.Split('v')[1].Trim() ?? "null";
        http.Dispose();
        
        SpicetifyCmd("-v");
        Thread.Sleep(500);
        
        if (showResult || Vars.IsDebug) {
            Console.WriteLine("Latest version: " + latestVersion.Pastel("00FF00"));
            Console.WriteLine("Current version: " + _lastOutput?.Trim().Pastel(_lastOutput?.Trim() == latestVersion ? "00FF00" : "FF0000"));
            Console.WriteLine();
        }

        return latestVersion == (_lastOutput?.Trim() ?? "0.0.0");
    }

    private static void SpicetifyCmd(string arg) {
        if (Vars.IsWindows) {
            var spicetifyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "spicetify");
            var spicetifyExe = Path.Combine(spicetifyPath, "spicetify.exe");
            var pStartInfo = new ProcessStartInfo {
                FileName = spicetifyExe,
                Arguments = arg,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            var p = Process.Start(pStartInfo);
            _lastOutput = p!.StandardOutput.ReadToEnd();
            // var standardError = p.StandardError.ReadToEnd();
            p.WaitForExit();
        }
        else {
            var pStartInfo = new ProcessStartInfo {
                FileName = "/bin/bash",
                Arguments = $"spicetify {arg}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            var p = Process.Start(pStartInfo);
            _lastOutput = p!.StandardOutput.ReadToEnd();
            // var standardError = p.StandardError.ReadToEnd();
            p.WaitForExit();
        }
    }
}