﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GryphonUtilities;
using GryphonUtilities.Time;
using Notion.Client;

namespace GryphonUtilityBot.Web.Models.Calendar.Notion;

internal sealed class Provider
{
    public Provider(INotionClient client, Config config, BotSingleton botSingleton)
    {
        _client = client;
        _clock = botSingleton.Bot.Clock;
        _updatePeriod = TimeSpan.FromSeconds(config.NotionUpdatesPerSecondLimit);
        _logger = botSingleton.Bot.Logger;
    }

    public Task<RequestResult<PageInfo>> TryGetPageAsync(string id) => Wrapper(GetPageAsync, id);

    public async Task UpdateEventDataAsync(PageInfo page, string eventId, Uri? eventUri)
    {
        Dictionary<string, PropertyValue> toUpdate = new();

        if (!page.GoogleEventId.Equals(eventId, StringComparison.Ordinal))
        {
            toUpdate["Google Event Id"] = CreateTextValue(eventId);
        }

        if (page.GoogleEvent != eventUri)
        {
            toUpdate["Google Event"] = eventUri is null
                ? new UrlPropertyValue()
                : new UrlPropertyValue { Url = eventUri.AbsoluteUri };
        }

        if (toUpdate.Count == 0)
        {
            return;
        }

        DelayIfNeeded();
        await _client.Pages.UpdatePropertiesAsync(page.Page.Id, toUpdate);
    }

    public Task ClearEventDataAsync(PageInfo page) => UpdateEventDataAsync(page, string.Empty, null);

    private async Task<PageInfo> GetPageAsync(string id)
    {
        DelayIfNeeded();
        Page page = await _client.Pages.RetrieveAsync(id);
        return new PageInfo(page, _clock);
    }

    private async Task<RequestResult<TResult>> Wrapper<TParam, TResult>(Func<TParam, Task<TResult>> method,
        TParam param)
        where TResult : class
    {
        try
        {
            TResult result = await method(param);
            return new RequestResult<TResult>(result);
        }
        catch (NotionApiException ex) when (ex.NotionAPIErrorCode == NotionAPIErrorCode.ObjectNotFound)
        {
            _logger.LogError($"Method with parameter {param} resulted with ObjectNotFound");
            _logger.LogException(ex);
            return new RequestResult<TResult>(true);
        }
        catch (NotionApiException ex) when (ex.NotionAPIErrorCode.HasValue)
        {
            _logger.LogError($"Method with parameter {param} resulted with NotionApiException with NotionAPIErrorCode {ex.NotionAPIErrorCode} and StatusCode {ex.StatusCode}");
            _logger.LogException(ex);
            return new RequestResult<TResult>(false);
        }
        catch (NotionApiException ex)
        {
            _logger.LogError($"Method with parameter {param} resulted with NotionApiException with unspecified NotionAPIErrorCode and StatusCode {ex.StatusCode}");
            _logger.LogException(ex);
            return new RequestResult<TResult>(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"Method with parameter {param} resulted with HttpRequestException with HttpRequestError {ex.HttpRequestError}");
            _logger.LogException(ex);
            return new RequestResult<TResult>(false);
        }
    }

    private void DelayIfNeeded()
    {
        lock (_delayLocker)
        {
            DateTimeFull now = DateTimeFull.CreateUtcNow();

            TimeSpan? beforeUpdate = Clock.GetDelayUntil(_lastUpdate, _updatePeriod, now);
            if (beforeUpdate.HasValue)
            {
                Task.Delay(beforeUpdate.Value).Wait();
                now += beforeUpdate.Value;
            }
            _lastUpdate = now;
        }
    }

    private static RichTextPropertyValue CreateTextValue(string content)
    {
        RichTextPropertyValue result = new()
        {
            RichText = new List<RichTextBase>()
        };
        Text text = new() { Content = content };
        RichTextText item = new() { Text = text };
        result.RichText.Add(item);
        return result;
    }

    private readonly INotionClient _client;
    private readonly Clock _clock;
    private readonly object _delayLocker = new();
    private readonly TimeSpan _updatePeriod;
    private DateTimeFull? _lastUpdate;
    private readonly Logger _logger;
}