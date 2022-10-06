using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows.Forms;

namespace ModPackUpdate
{
    internal static class Program
    {
        private const string BaseUrl = "https://xspeed.pl/mc/";
        private const string MetaFile = "modpack.txt";
        private const string TmpFile = "newpack.zip";

        private static void Main()
        {
            string mcRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");

            if (!Directory.Exists(mcRoot))
            {
                ShowMsg("Install regular Minecraft first", true);
                return;
            }

            string profilesPath = Path.Combine(mcRoot, "launcher_profiles.json");
            string profilesData = File.Exists(profilesPath) ? File.ReadAllText(profilesPath) : "";

            if (!profilesData.Contains("quilt-loader"))
            {
                ShowMsg("Install Quilt Loader first", true);
                return;
            }

            string modpackPath = Path.Combine(mcRoot, MetaFile);
            string modpackData = File.Exists(modpackPath) ? File.ReadAllText(modpackPath) : "V0";
            string modpackTmp = Path.Combine(mcRoot, TmpFile);

            using (var client = new WebClient())
            {
                string[] onlineData;

                try { onlineData = client.DownloadString(BaseUrl + MetaFile).Split('\n'); }
                catch
                {
                    ShowMsg("Online version check failed", true);
                    return;
                }

                if (onlineData[1] != modpackData)
                {
                    ShowMsg($"Your version {modpackData} and online version {onlineData[1]}, time to update");

                    client.DownloadFile(BaseUrl + onlineData[0], modpackTmp);
                    modpackData = onlineData[1];
                }
            }

            if (File.Exists(modpackTmp))
            {
                ShowMsg("Downloaded new pack, time to extract");

                try
                {
                    string modsPath = Path.Combine(mcRoot, "mods");
                    if (Directory.Exists(modsPath)) Directory.Delete(modsPath, true);
                }
                catch
                {
                    ShowMsg("Could not delete old mods folder", true);
                    return;
                }

                try
                {
                    using var archive = ZipFile.OpenRead(modpackTmp);
                    foreach (var entry in archive.Entries)
                    {
                        string entryPath = Path.Combine(mcRoot, entry.FullName);

                        if (string.IsNullOrEmpty(entry.Name)) Directory.CreateDirectory(Path.GetDirectoryName(entryPath));
                        else entry.ExtractToFile(entryPath, true);
                    }
                }
                catch
                {
                    ShowMsg("Could not extract the new pack", true);
                    return;
                }

                File.WriteAllText(modpackPath, modpackData);
                File.Delete(modpackTmp);
            }

            ShowMsg($"Using version {modpackData}, you're good to go");
        }

        private static void ShowMsg(string msg, bool error = false) =>
            MessageBox.Show(msg + " bro", "Mod Pack Updater", MessageBoxButtons.OK, error ? MessageBoxIcon.Error : MessageBoxIcon.Information);
    }
}