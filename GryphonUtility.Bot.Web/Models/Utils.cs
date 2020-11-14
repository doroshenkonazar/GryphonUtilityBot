﻿using System;
using System.IO;

namespace GryphonUtility.Bot.Web.Models
{
    internal static class Utils
    {
        public static void LogException(Exception ex)
        {
            File.AppendAllText(ExceptionsLogPath, $"{ex}{Environment.NewLine}");
        }

        private const string ExceptionsLogPath = "errors.txt";
    }
}
