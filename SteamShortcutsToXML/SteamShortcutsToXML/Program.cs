/*
 * Copyright (C) 2012 by Giovanni Capuano <webmaster@giovannicapuano.net>
 *
 * SteamShortcutsToXML is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * SteamShortcutsToXML is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with SteamShortcutsToXML.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Win32;

namespace SteamShortcutsToXML
{
    class Program
    {
        static string GetSteamFolder()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Valve\\Steam");
            if (key == null)
                if ((key = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Valve\\Steam")) == null)
                    return null;
            return key.GetValue("InstallPath").ToString();
        }

        static string GetSteamUserID(string path)
        {
            return Directory.GetDirectories(path)[0];
        }

        static void Main(string[] args)
        {
            string steam = GetSteamFolder();
            if (steam == null)
            {
                Console.WriteLine("Steam folder not found.");
                Console.ReadLine();
                return;
            }

            string input = GetSteamUserID(steam + "\\userdata\\") + "\\config\\shortcuts.vdf";
            string output = steam + "\\shortcuts.xml";

            string file = File.ReadAllText(input);
            file = Regex.Replace(file, @"\x01", "<tag>");
            file = Regex.Replace(file, @"\x00", "\n");
            file = Regex.Replace(file, @"\b", "");

            string[] games = Regex.Split(file, "\n");
            string[] tmp = new string[games.Length];

            Hashtable gamelist = new Hashtable();
            string go = "";

            for (int line = 0, length = games.Length; line < length; ++line)
            {
                if (go == "")
                {
                    if (games[line] == "<tag>AppName")
                        go = "AppName";
                    else if (games[line] == "<tag>Exe")
                        go = "Exe";
                    else if (games[line] == "<tag>StartDir")
                        go = "StartDir";
                    continue;
                }

                if (go == "AppName")
                {
                    tmp[0] = games[line];
                    go = "";
                }
                else if (go == "Exe")
                {
                    tmp[1] = games[line].Replace("\"", "");
                    go = "";
                }
                else if (go == "StartDir")
                {
                    tmp[2] = games[line].Replace("\"", "");
                    gamelist.Add(tmp[0], new string[] { tmp[1], tmp[2] });
                    tmp = new string[length];
                    go = "";
                }
            }

            string[] value;
            string xml = "<games>\n";
            foreach (DictionaryEntry pair in gamelist)
            {
                value = (string[])pair.Value;
                xml += "\t<game>\n";
                xml += "\t\t<name>" + pair.Key.ToString().Trim() + "</name>\n";
                xml += "\t\t<exe>" + value[0].Trim() + "</exe>\n";
                xml += "\t\t<dir>" + value[1].Trim() + "</dir>\n";
                xml += "\t</game>\n";
            }
            xml += "</games>\n";

            using (StreamWriter sw = File.CreateText(output))
            {
                sw.Write(xml);
                sw.Close();
            }
        }
    }
}