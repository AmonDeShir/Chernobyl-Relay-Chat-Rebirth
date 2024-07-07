﻿using Octokit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Chernobyl_Relay_Chat
{
    class CRCGame
    {
        private const int SCRIPT_VERSION = 8;
        public static bool DEBUG = false;
        public static int ActorMoney = 0;
        public static bool IsInGame = false;
        public static bool FakeConnLost = false;
        private static readonly CRCGameWrapper wrapper = new CRCGameWrapper();
        private static readonly Encoding encoding = Encoding.GetEncoding(1251);
        private static readonly Regex outputRx = new Regex("^(.+?)(?:/(.+))?$");
        private static readonly Regex messageRx = new Regex("^(.+?)/(.+)$");
        private static readonly Regex deathRx = new Regex("^(.+?)/(.+?)/(.+?)/(.+)$");
        private static readonly Regex connLostRx = new Regex("^(.+?)/(.+)$");

        public static bool disable = false;
        public static int processID = -1;
        private static string gamePath;
        private static bool firstClear = false;
        private static StringBuilder sendQueue = new StringBuilder();
        private static object queueLock = new object();

        private static ClientDisplay display = new ClientDisplay();
        private static CRCClient client;

        public CRCGame(ClientDisplay clientDisplay, CRCClient crcClient)
        {
            display = clientDisplay;
            client = crcClient;
        }

        private static void Disable()
        {
            disable = true;
            MessageBox.Show(CRCStrings.Localize("game_file_error"), CRCStrings.Localize("crc_name"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void GameCheck()
        {
            if (disable) return;

            if (processID != -1)
            {
                try
                {
                    Process.GetProcessById(processID);
                }
                catch (ArgumentException)
                {
                    processID = -1;
                    IsInGame = false;
                    CRCClient.UpdateStatus();
                    lock (queueLock)
                    {
                        sendQueue.Clear();
                    }
                }
            }
            if (processID == -1)
            {
                IsInGame = false;
                CRCClient.UpdateStatus();
                foreach (Process process in Process.GetProcesses())
                {
                    if (process.MainWindowTitle == "S.T.A.L.K.E.R.: Anomaly")
                    {
                        try {
                            string path = Path.GetDirectoryName(process.GetProcessPath());
                            if (File.Exists(path + CRCOptions.InPath))
                            {
                                gamePath = path;
                                firstClear = false;
                                processID = process.Id;
                                IsInGame = true;
                                CRCClient.UpdateStatus();
                                UpdateSettings();
                                break;
                            }
                        }
                        catch 
                        {
                            continue;
                        }                        
                    }
                }
            }
        }

        public static void GameUpdate()
        {
            if (disable || processID == -1) return;

            // Wipe game output when first discovered
            if (!firstClear)
            {
                try
                {
                    File.WriteAllText(gamePath + CRCOptions.OutPath, "", encoding);
                    firstClear = true;
                }
                catch (IOException)
                {
                    return;
                }
                catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
                {
                    Disable();
                    return;
                }
            }

            // Get messages from game
            try
            {
                string[] lines = File.ReadAllLines(gamePath + CRCOptions.OutPath, encoding);
                File.WriteAllText(gamePath + CRCOptions.OutPath, "", encoding);
                foreach (string line in lines)
                {
                    Match typeMatch = outputRx.Match(line);
                    string type = typeMatch.Groups[1].Value;
                    if (type == "Handshake")
                    {
                        if (Convert.ToInt16(typeMatch.Groups[2].Value) < SCRIPT_VERSION)
                        {
                            AddError(CRCStrings.Localize("game_script_version_error"));
                            CRCDisplay.AddError(CRCStrings.Localize("game_script_version_error"));
                        }
                        UpdateSettings();
                        UpdateUsers();
                    }
                    else if (type == "Money")
                    {
                        int amount;
                        bool acceptable = int.TryParse(typeMatch.Groups[2].Value, out amount);
                            if (acceptable)
                        {
                            ActorMoney = amount;
                        }
                            else
                        {
                            ActorMoney = 1000001;
                        }
                    }
                    else if (type == "Message")
                    {
                        Match messageMatch = messageRx.Match(typeMatch.Groups[2].Value);
                        string faction = messageMatch.Groups[1].Value;
                        string message = messageMatch.Groups[2].Value;
                        if (message[0] == '/')
                            CRCCommands.ProcessCommand(message, wrapper);
                        else
                        {
                            CRCOptions.GameFaction = CRCStrings.ValidateFaction(faction);
                            CRCClient.UpdateSettings();
                            if (CRCOptions.GameFaction == "actor_zombied")
                                CRCClient.Send(CRCZombie.Generate());
                            else
                                CRCClient.Send(message);
                        }
                    }
                    else if (type == "Death" && CRCOptions.SendDeath)
                    {
                        Match deathMatch = deathRx.Match(typeMatch.Groups[2].Value);
                        string faction = deathMatch.Groups[1].Value;
                        string level = deathMatch.Groups[2].Value;
                        string xrClass = deathMatch.Groups[3].Value;
                        string section = deathMatch.Groups[4].Value;
                        CRCOptions.GameFaction = CRCStrings.ValidateFaction(faction);
                        CRCClient.UpdateSettings();
                        if (CRCOptions.GameFaction != "actor_zombied")
                        {
                            string message = CRCStrings.DeathMessage(CRCOptions.Name, level, xrClass, section);
                            CRCClient.SendDeath(message);
                        }
                    }
                    else if (type == "ConnLost")
                    {
                        Match connLostMatch = connLostRx.Match(typeMatch.Groups[2].Value);
                        if (connLostMatch.Groups[1].Value == "true" && FakeConnLost == false && CRCOptions.DisconnectWhenBlowoutOrUnderground)
                        {
                            CRCClient.OnSignalLost(connLostMatch.Groups[2].Value);
                            FakeConnLost = true;
                            new ConnLostForm().ShowDialog(ClientDisplay.staticVar);
                        }
                        else if (connLostMatch.Groups[1].Value == "false")
                        {
                            if (ConnLostForm.staticVar != null)
                            {
                                ConnLostForm.staticVar.Close();
                                CRCClient.OnSignalRestored();
                                FakeConnLost = false;
                            }
                        }
                    }
                    else if (type == "DEBUG")
                    {
                        DEBUG = typeMatch.Groups[2].Value == "true";
                    }
                    else if (type == "Channel")
                    {
                        bool acceptable = int.TryParse(typeMatch.Groups[2].Value, out int index);
                        if (acceptable)
                        {
                            CRCDisplay.OnChannelUpdateFromGame(index - 1);
                        }
                    }

                }
            }
            catch (IOException) { }
            catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
            {
                Disable();
                return;
            }

            // Send messages to game
            lock (sendQueue)
            {
                try
                {
                    File.AppendAllText(gamePath + CRCOptions.InPath, sendQueue.ToString(), encoding);
                    sendQueue.Clear();
                }
                catch (IOException) { }
                catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
                {
                    Disable();
                    return;
                }
            }
        }

        public static void UpdateSettings()
        {
            SendToGame("Setting/NewsDuration/" + (CRCOptions.NewsDuration * 1000));
            SendToGame("Setting/ChatKey/DIK_" + CRCOptions.ChatKey);
            SendToGame("Setting/NickAutoCompleteKey/DIK_" + CRCOptions.NickAutoCompleteKey);
            SendToGame("Setting/NewsSound/" + CRCOptions.NewsSound);
            SendToGame("Setting/CloseChat/" + CRCOptions.CloseChat);
            SendToGame("Setting/DisconnectWhenBlowoutOrUnderground/" + CRCOptions.DisconnectWhenBlowoutOrUnderground);
            SendToGame("Setting/ActorStatus/Get");
            SendToGame("Setting/Channel/" + ChannelConvertToGame());
        }

        private static string ChannelConvertToGame()
        {
            var index = "";
            if (CRCOptions.Channel == "#crcr_english")
                index = "1";
            if (CRCOptions.Channel == "#crcr_english_rp")
                index = "2";
            if (CRCOptions.Channel == "#crcr_english_shitposting")
                index = "3";
            if (CRCOptions.Channel == "#crcr_russian")
                index = "4";
            if (CRCOptions.Channel == "#crcr_russian_rp")
                index = "5";
            if (CRCOptions.Channel == "#crcr_tech_support")
                index = "6";
            return index;
        }

        public static void UpdateUsers()
        {
            string UserStatus = "";
            foreach (KeyValuePair<string, Userdata> item in CRCClient.userData)
            {
                if (!CRCOptions.IsNickBlocked(item.Key)) {
                    UserStatus += item.Key + ',' + item.Value.Faction + " = " + item.Value.IsInGame + "/";
                }
            }
            SendToGame("Users/" + UserStatus.TrimEnd('/'));
#if DEBUG
            System.Diagnostics.Debug.WriteLine(UserStatus.TrimEnd('/'));
#endif
            //SendToGame("Users/" + string.Join("/", CRCClient.Users));
        }

        private static void SendToGame(string line)
        {
            if (disable || processID == -1) return;

            lock (sendQueue)
            {
                sendQueue.AppendLine(line);
            }
        }

        public static void AddInformation(string message)
        {
            SendToGame("Information/" + message);
        }

        public static void AddError(string message)
        {
            SendToGame("Error/" + message);
        }



        public static void OnUpdate(string message)
        {
            AddInformation(message);
        }

        public static void OnHighlightMessage(string nick, string faction, string message)
        {
            SendToGame("Message/" + faction + "/" + nick + "/True/" + message);
        }

        public static void OnChannelMessage(string nick, string faction, string message)
        {
            SendToGame("Message/" + faction + "/" + nick + "/False/" + message);
        }

        public static void OnQueryMessage(string from, string to, string faction, string message)
        {
            SendToGame("Query/" + faction + "/" + from + "/" + to + "/" + message);
        }

        public static void OnMoneySent(string from, string to, string faction, string message)
        {
            SendToGame("Money/" + from + "/" + to + "/" + message);
        }
        
        public static void OnMoneyRecv(string from, string message)
        {
            SendToGame("MoneyRecv/" + from + "/" + message);
        }

        public static void OnChannelSwitch()
        {
            SendToGame("Setting/Channel/" + ChannelConvertToGame());
        }

        private static readonly Dictionary<int, string> gameIndexToChannelName = new Dictionary<int, string>()
        {
            [1] = "Main Channel (Eng)",
            [2] = "Roleplay Channel (Eng)",
            [3] = "Unmoderated Channel (Eng)",
            [4] = "Основной Канал (Русский)",
            [5] = "Ролевой Канал (Русский)",
            [6] = "Tech Support/Техподдержка",
        };
    }
}
