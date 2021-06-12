using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using lastpass_to_json.BitwardenDto;
using Newtonsoft.Json;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;
using Uri = lastpass_to_json.BitwardenDto.Uri;

namespace lastpass_to_json
{
    internal class Program
    {
        private static void Main()
        {
            bool exists = File.Exists("lastpass_export.csv");
            if (!exists)
            {
                Console.WriteLine("lastpass_export.csv not found. Please export your data from LastPass and save as lastpass_export.csv");
                Environment.Exit(exitCode: 0);
            }

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Error                 += OnError;
            settings.MissingMemberHandling =  MissingMemberHandling.Error;

            Bitwarden bitwarden = new Bitwarden
            {
                folders = new List<Folder>(),
                items   = new List<BitwardenItem>()
            };
            Folder importedFromLastPassFolder = new Folder
            {
                id   = Guid.NewGuid().ToString(),
                name = "Imported from LastPass"
            };
            bitwarden.folders.Add(importedFromLastPassFolder);
            string[] lines = File.ReadAllLines("lastpass_export.csv");
            if (lines.Length == 0 || lines.First() != "url,username,password,totp,extra,name,grouping,fav")
            {
                Console.WriteLine("Invalid lastpass_export.csv. The expected first row is url,username,password,totp,extra,name,grouping,fav");
                Environment.Exit(exitCode: 0);
            }

            List<string> entries = new List<string>();

            foreach (string line in lines.Skip(1).Where(x => !string.IsNullOrEmpty(x)))
            {
                if (IsCompletedEntry(line) || IsCompletedEntry(entries[^1]))
                {
                    entries.Add(line);
                }
                else
                {
                    entries[^1] += Environment.NewLine;
                    entries[^1] += line;
                }
            }

            foreach (string entry in entries)
            {
                string[] fields = entry.Split(',');
                LastPassItem lastpassItem = new LastPassItem()
                {
                    url = fields[0],
                    username = fields[1],
                    password = fields[2],
                    totp = fields[3],
                    extra = fields[4],
                    name = fields[5],
                    grouping = fields[6],
                    fav = fields[7]
                };

                BitwardenItem bitwardenItem = new BitwardenItem();
                bitwardenItem.id       = Guid.NewGuid().ToString();
                bitwardenItem.folderId = importedFromLastPassFolder.id;
                bitwardenItem.name     = lastpassItem.name;
                bitwardenItem.notes    = lastpassItem.extra;
                bitwardenItem.fields = new List<Field>();
                if (!string.IsNullOrEmpty(lastpassItem.grouping))
                {
                    bitwardenItem.fields.Add(new Field() { name = "grouping", value = lastpassItem.grouping });
                }
                if (!string.IsNullOrEmpty(lastpassItem.fav) && lastpassItem.fav != "0")
                {
                    bitwardenItem.favorite = true;
                }
                bitwardenItem.login          = new Login();
                bitwardenItem.type           = 1; // login
                if (!string.IsNullOrEmpty(lastpassItem.totp))
                {
                    bitwardenItem.login.totp = lastpassItem.totp;
                }
                bitwardenItem.login.username = lastpassItem.username;
                bitwardenItem.login.password = lastpassItem.password;
                bitwardenItem.login.uris     = new List<Uri>();
                Uri uri = new Uri();
                uri.uri = lastpassItem.url;
                bitwardenItem.login.uris.Add(uri);
                   
                bitwarden.items.Add(bitwardenItem);
            }

            Console.WriteLine($"Read {bitwarden.items.Count} items.");

            File.WriteAllText("Bitwarden.json", JsonConvert.SerializeObject(bitwarden, Formatting.Indented).Replace("\r\n", "\n"));
            Console.WriteLine("Done. Saved to Bitwarden.json");
            Console.ReadKey();
        }

        private static bool IsCompletedEntry(string text)
        {
            return text.Split(',').Length == 8;
        }

        private static void OnError(object? sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.ErrorContext.Error);
        }
    }
}