﻿using CodeDeck.Models;
using CodeDeck.Models.Configuration;
using Microsoft.Extensions.Logging;
using OpenMacroBoard.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CodeDeck
{
    public class ConfigurationProvider
    {
        private StreamDeckConfiguration _loadedConfiguration;
        private List<FlatKeyConfiguration> _loadedFlatConfiguration;
        private readonly ILogger<ConfigurationProvider> _logger;
        private readonly FileSystemWatcher _fileSystemWatcher;

        public event EventHandler? ConfigurationChanged;
        public event EventHandler? DeckCheckConfiguration;

        public const string CONFIGURATION_FILE_NAME = "deck.json";
        public const string CONFIGURATION_FOLDER_NAME = ".codedeck";

        public static readonly string UserFolder;
        public static readonly string ConfigFolder;
        public static readonly string ConfigFile;

        public StreamDeckConfiguration LoadedConfiguration => _loadedConfiguration;

        public List<FlatKeyConfiguration> LoadedFlatConfiguration => _loadedFlatConfiguration;

        static ConfigurationProvider()
        {
            UserFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            ConfigFolder = Path.Combine(UserFolder, CONFIGURATION_FOLDER_NAME);
            ConfigFile = Path.Combine(ConfigFolder, CONFIGURATION_FILE_NAME);
        }

        public ConfigurationProvider(ILogger<ConfigurationProvider> logger)
        {
            _logger = logger;
            // Make sure configuration folder exists
            if (!Directory.Exists(ConfigFolder))
            {
                Directory.CreateDirectory(ConfigFolder);
            }

            // Load configuration from file or load a default configuration
            ReloadConfig(EventArgs.Empty);


            // Set up file system watcher
            _fileSystemWatcher = new FileSystemWatcher(ConfigFolder)
            {
                Filter = CONFIGURATION_FILE_NAME,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite
            };

            // Register file system watcher changed event
            _fileSystemWatcher.Changed += Handle_FileSystemWatcher_Changed;
        }

        private async void Handle_FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            // TODO: Wait for file access ready in a better way
            await Task.Delay(500); // Wait for file to finish writing

            ReloadConfig(e);
        }

        public async void ReloadConfig(EventArgs e)
        {
            // Load configuration from file or load a default configuration
            _loadedConfiguration = LoadConfiguration() ?? CreateDefaultConfiguration();
            DeckCheckConfiguration?.Invoke(this, e);
            _loadedFlatConfiguration = GetFlatConfiguration(_loadedConfiguration);

            // Invoke event handlers
            ConfigurationChanged?.Invoke(this, e);
        }

        private StreamDeckConfiguration? LoadConfiguration()
        {
            try
            {
                if (!DoesConfigurationFileExists())
                {
                    _logger.LogError($"Configuration file does not exist. Filename: {ConfigFile}");
                    var defaultConfiguration = CreateDefaultConfiguration();
                    var defaultJson = JsonSerializer.Serialize(defaultConfiguration,
                        new JsonSerializerOptions()
                        {
                            WriteIndented = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        });
                    File.WriteAllText(ConfigFile, defaultJson);
                    _logger.LogWarning($"Default configuration file created. Filename: {ConfigFile}");
                }

                var json = File.ReadAllText(ConfigFile);

                var configuration = JsonSerializer.Deserialize<StreamDeckConfiguration>(json, new JsonSerializerOptions()
                {
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });


                if (configuration is not null)
                {
                    return configuration;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not load configuration. Exception: {e.Message}");
            }

            return null;
        }


        private static List<FlatKeyConfiguration> GetFlatConfiguration(StreamDeckConfiguration configuration)
        {
            Console.WriteLine($"Getting flat config for {configuration.Profiles[0].Pages[0].Keys.Count()} keys");
            var flatConfiguration = (
                from profile in configuration.Profiles
                from page in profile.Pages
                from key in page.Keys
                select new FlatKeyConfiguration(key, page, profile)
            ).ToList();

            return flatConfiguration;
        }

        public static bool DoesConfigurationFileExists()
        {
            return File.Exists(ConfigFile);
        }

        public static StreamDeckConfiguration CreateDefaultConfiguration()
        {
            var config = new StreamDeckConfiguration
            {
                Brightness = 100,
                Rotation = 0,
                Profiles = new List<Profile>()
                {
                    new Profile()
                    {
                        Name = Profile.PROFILE_DEFAULT_NAME,
                        Pages = new List<Page>()
                        {
                            new Page()
                            {
                                Name = Page.PAGE_DEFAULT_NAME,
                                Keys = new List<Key>()
                                {
                                    new Key()
                                    {
                                        ID = new KeyIdentifier(1,1),
                                        Text = "CODE",
                                        BackgroundColor = "#0b367f"
                                    },
                                }
                            }
                        }
                    }
                }
            };

            return config;
        }
    }
}
