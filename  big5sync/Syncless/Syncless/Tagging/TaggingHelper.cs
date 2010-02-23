﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using Syncless.Logging;

namespace Syncless.Tagging
{
    static class TaggingHelper
    {
        public static string[] TrimEnd(string[] tempPathTokens)
        {
            string[] pathTokens = new string[tempPathTokens.Length - 1];
            if (tempPathTokens[tempPathTokens.Length - 1].Equals(""))
            {
                for (int i = 0; i < pathTokens.Length; i++)
                {
                    pathTokens[i] = tempPathTokens[i];
                }
                return pathTokens;
            }
            return tempPathTokens;
        }

        public static string GetLogicalID(string path)
        {
            string[] tokens = path.Split('\\');
            return (tokens[0].TrimEnd(':'));
        }

        public static long GetCurrentTime()
        {
            return DateTime.Now.Ticks;
        }

        public static string GetCurrentTimeString()
        {
            DateTime currenttime = DateTime.Now;
            string day = (currenttime.Day < 10) ? ("0" + currenttime.Day.ToString()) : currenttime.Day.ToString();
            string month = (currenttime.Month < 10) ? ("0" + currenttime.Month.ToString()) : currenttime.Month.ToString();
            string year = currenttime.Year.ToString();
            string hour = (currenttime.Hour < 10) ? ("0" + currenttime.Hour.ToString()) : currenttime.Hour.ToString();
            string minute = (currenttime.Minute < 10) ? ("0" + currenttime.Minute.ToString()) : currenttime.Minute.ToString();
            string second = (currenttime.Second < 10) ? ("0" + currenttime.Second.ToString()) : currenttime.Second.ToString();
            string datestring = string.Format("{0}/{1}/{2} {3}:{4}:{5}", day, month, year, hour, minute, second);
            return datestring;
        }

        public static string CreatePath(int trailingIndex, string[] pathTokens, bool isFolder)
        {
            string trailingPath = null;
            for (int i = trailingIndex; i < pathTokens.Length - 1; i++)
            {
                trailingPath += (pathTokens[i] + "\\");
            }
            if (isFolder)
            {
                trailingPath += (pathTokens[pathTokens.Length - 1] + "\\");
            }
            else
            {
                trailingPath += pathTokens[pathTokens.Length - 1];
            }
            return trailingPath;
        }

        public static bool CheckRecursiveDirectory(FolderTag folderTag, string path)
        {
            foreach (TaggedPath p in folderTag.PathList)
            {
                if (TrimEnd(path.Split('\\')).Length != TrimEnd(p.Path.Split('\\')).Length)
                {
                    if (p.Path.StartsWith(path) || path.StartsWith(p.Path))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static int Match(string[] pathTokens, string[] pTokens)
        {
            int trailingIndex = 0;
            for (int i = 0; i < pTokens.Length; i++)
            {
                if (pTokens[i].Equals(pathTokens[i]))
                {
                    trailingIndex++;
                }
                else
                {
                    break;
                }
            }
            return trailingIndex;
        }

        public static void Logging(string message, params object[] list)
        {
            //to call ServiceLocator.GetLogger();
        }
    }
}