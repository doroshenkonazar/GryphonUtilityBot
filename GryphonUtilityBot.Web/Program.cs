﻿using System;
using System.Threading.Tasks;
using AbstractBot;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace GryphonUtilityBot.Web;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        Utils.LogManager.SetTimeZone(SystemTimeZoneId);
        Utils.LogManager.LogMessage();

        Utils.LogManager.LogTimedMessage("Startup");
        Utils.LogManager.DeleteExceptionLog();
        try
        {
            await CreateWebHostBuilder(args).Build().RunAsync();
        }
        catch (Exception ex)
        {
            Utils.LogManager.LogException(ex);
        }
    }

    private static IHostBuilder CreateWebHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
                   .ConfigureWebHostDefaults(builder => builder.UseStartup<Startup>());
    }

    private const string SystemTimeZoneId = "Arabian Standard Time";
}