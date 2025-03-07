﻿using HASS.Agent.Shared.Enums;
using HASS.Agent.Shared.Models.Config;
using HASS.Agent.Shared.HomeAssistant.Commands;
using HASS.Agent.Shared.HomeAssistant.Commands.CustomCommands;
using HASS.Agent.Shared.HomeAssistant.Commands.InternalCommands;
using HASS.Agent.Shared.HomeAssistant.Commands.KeyCommands;
using HASS.Agent.Satellite.Service.Models.HomeAssistant.Commands.InternalCommands;
using HASS.Agent.Shared.Models.HomeAssistant;
using Newtonsoft.Json;
using Serilog;

namespace HASS.Agent.Satellite.Service.Settings
{
    /// <summary>
    /// Handles loading and storing commands
    /// </summary>
    internal static class StoredCommands
    {
        /// <summary>
        /// Load all stored commands
        /// </summary>
        /// <returns></returns>
        internal static async Task<bool> LoadAsync()
        {
            try
            {
                // add an empty list
                Variables.Commands = new List<AbstractCommand>();

                // check for existing file
                if (!File.Exists(Variables.CommandsFile))
                {
                    // none yet
                    Log.Information("[SETTINGS_COMMANDS] Config not found, no entities loaded");
                    return true;
                }

                // read the content
                var commandsRaw = await File.ReadAllTextAsync(Variables.CommandsFile);
                if (string.IsNullOrWhiteSpace(commandsRaw))
                {
                    Log.Information("[SETTINGS_COMMANDS] Config is empty, no entities loaded");
                    return true;
                }

                // deserialize
                var configuredCommands = JsonConvert.DeserializeObject<List<ConfiguredCommand>>(commandsRaw);

                // null-check
                if (configuredCommands == null)
                {
                    Log.Error("[SETTINGS_COMMANDS] Error loading entites: returned null object");
                    return false;
                }

                // convert to abstract commands
                await Task.Run(delegate
                {
                    foreach (var abstractCommand in configuredCommands.Select(ConvertConfiguredToAbstract)) Variables.Commands.Add(abstractCommand!);
                });

                // all good
                Log.Information("[SETTINGS_COMMANDS] Loaded {count} entities", Variables.Commands.Count);
                return true;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "[SETTINGS_COMMANDS] Error loading entities: {err}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Convert a 'ConfiguredCommand' (local storage, UI) to an 'AbstractCommand' (MQTT)
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        internal static AbstractCommand? ConvertConfiguredToAbstract(ConfiguredCommand command)
        {
            AbstractCommand? abstractCommand = null;

            switch (command.Type)
            {
                case CommandType.ShutdownCommand:
                    abstractCommand = new ShutdownCommand(command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.RestartCommand:
                    abstractCommand = new RestartCommand(command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.HibernateCommand:
                    abstractCommand = new HibernateCommand(command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.SleepCommand:
                    abstractCommand = new SleepCommand(command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.LogOffCommand:
                    abstractCommand = new LogOffCommand(command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.LockCommand:
                    abstractCommand = new LockCommand(command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.CustomCommand:
                    abstractCommand = new CustomCommand(command.Command, command.RunAsLowIntegrity, command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.PowershellCommand:
                    abstractCommand = new PowershellCommand(command.Command, command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.MediaPlayPauseCommand:
                    abstractCommand = new MediaPlayPauseCommand(command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.MediaNextCommand:
                    abstractCommand = new MediaNextCommand(command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.MediaPreviousCommand:
                    abstractCommand = new MediaPreviousCommand(command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.MediaVolumeUpCommand:
                    abstractCommand = new MediaVolumeUpCommand(command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.MediaVolumeDownCommand:
                    abstractCommand = new MediaVolumeDownCommand(command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.MediaMuteCommand:
                    abstractCommand = new MediaMuteCommand(command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.KeyCommand:
                    abstractCommand = new KeyCommand(command.KeyCode, command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.PublishAllSensorsCommand:
                    abstractCommand = new PublishAllSensorsCommand(command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.CustomExecutorCommand:
                    abstractCommand = new CustomExecutorCommand(command.EntityName, command.Name, command.Command, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.MultipleKeysCommand:
                    abstractCommand = new MultipleKeysCommand(command.Keys, command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.SendWindowToFrontCommand:
                    abstractCommand = new SendWindowToFrontCommand(command.EntityName, command.Name, command.Command, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.MonitorSleepCommand:
                    abstractCommand = new MonitorSleepCommand(command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.MonitorWakeCommand:
                    abstractCommand = new MonitorWakeCommand(command.EntityName, command.Name, command.EntityType, command.Id.ToString());
                    break;
                case CommandType.SetVolumeCommand:
                    abstractCommand = new SetVolumeCommand(command.EntityName, command.Name, command.Command, command.EntityType, command.Id.ToString());
                    break;
                default:
                    Log.Error("[SETTINGS_COMMANDS] [{name}] Unknown configured command type: {type}", command.EntityName, command.Type.ToString());
                    break;
            }

            return abstractCommand;
        }

        /// <summary>
        /// Convert an 'AbstractCommand' (MQTT) to an 'ConfiguredCommand' (local storage, UI)
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        internal static ConfiguredCommand? ConvertAbstractToConfigured(AbstractCommand command)
        {
            switch (command)
            {
                case CustomCommand customCommand:
                    {
                        _ = Enum.TryParse<CommandType>(customCommand.GetType().Name, out var type);
                        return new ConfiguredCommand()
                        {
                            Id = Guid.Parse(customCommand.Id),
                            EntityName = customCommand.EntityName,
                            Name = customCommand.EntityName,
                            Type = type,
                            EntityType = command.EntityType,
                            Command = customCommand.Command,
                            RunAsLowIntegrity = customCommand.RunAsLowIntegrity
                        };
                    }

                case PowershellCommand powershellCommand:
                    {
                        _ = Enum.TryParse<CommandType>(powershellCommand.GetType().Name, out var type);
                        return new ConfiguredCommand()
                        {
                            Id = Guid.Parse(powershellCommand.Id),
                            EntityName = powershellCommand.EntityName,
                            Name = powershellCommand.EntityName,
                            Type = type,
                            EntityType = command.EntityType,
                            Command = powershellCommand.Command
                        };
                    }

                case KeyCommand customKeyCommand:
                    {
                        _ = Enum.TryParse<CommandType>(customKeyCommand.GetType().Name, out var type);
                        return new ConfiguredCommand()
                        {
                            Id = Guid.Parse(customKeyCommand.Id),
                            EntityName = customKeyCommand.EntityName,
                            Name = customKeyCommand.EntityName,
                            Type = type,
                            EntityType = command.EntityType,
                            KeyCode = customKeyCommand.KeyCode
                        };
                    }

                case InternalCommand internalCommand:
                    {
                        _ = Enum.TryParse<CommandType>(internalCommand.GetType().Name, out var type);
                        return new ConfiguredCommand()
                        {
                            Id = Guid.Parse(internalCommand.Id),
                            EntityName = internalCommand.EntityName,
                            Name = internalCommand.EntityName,
                            Command = internalCommand.CommandConfig ?? string.Empty,
                            Type = type,
                            EntityType = command.EntityType,
                        };
                    }

                case MultipleKeysCommand multipleKeysCommand:
                    {
                        _ = Enum.TryParse<CommandType>(multipleKeysCommand.GetType().Name, out var type);
                        return new ConfiguredCommand()
                        {
                            Id = Guid.Parse(multipleKeysCommand.Id),
                            EntityName = multipleKeysCommand.EntityName,
                            Name = multipleKeysCommand.EntityName,
                            Keys = multipleKeysCommand.Keys ?? new List<string>(),
                            Type = type,
                            EntityType = command.EntityType,
                        };
                    }
            }

            return null;
        }

        /// <summary>
        /// Store all current commands
        /// </summary>
        /// <returns></returns>
        internal static bool Store()
        {
            try
            {
                // check config dir
                if (!Directory.Exists(Variables.ConfigPath))
                {
                    // create
                    Directory.CreateDirectory(Variables.ConfigPath);
                }

                // convert commands
                var configuredCommands = Variables.Commands.Select(ConvertAbstractToConfigured).ToList();

                // serialize to file
                var commands = JsonConvert.SerializeObject(configuredCommands, Formatting.Indented);
                File.WriteAllText(Variables.CommandsFile, commands);

                // done
                Log.Information("[SETTINGS_COMMANDS] Stored {count} entities", Variables.Commands.Count);
                return true;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "[SETTINGS_COMMANDS] Error storing entities: {err}", ex.Message);
                return false;
            }
        }
    }
}
