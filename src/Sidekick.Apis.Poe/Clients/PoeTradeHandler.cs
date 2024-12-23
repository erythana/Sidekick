﻿using System.Net;
using Microsoft.Extensions.Logging;
using Sidekick.Apis.Poe.CloudFlare;
using Sidekick.Common.Exceptions;
using Sidekick.Common.Game.Languages;
using Sidekick.Common.Settings;

namespace Sidekick.Apis.Poe.Clients;

public class PoeTradeHandler
(
    ILogger<PoeTradeHandler> logger,
    ICloudflareService cloudflareService,
    ISettingsService settingsService,
    IGameLanguageProvider gameLanguageProvider
) : DelegatingHandler
{
    public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.TryAddWithoutValidation("X-Powered-By", "Sidekick");

        // First try with existing cookies
        await cloudflareService.AddCookieToRequest(request);
        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        if (response.StatusCode == HttpStatusCode.Moved ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.RedirectKeepVerb)
        {
            response = await HandleRedirect(request, response, cancellationToken);
        }

        if (response.StatusCode == HttpStatusCode.Moved || response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.RedirectKeepVerb)
        {
            logger.LogWarning("[PoeTradeHandler] Received redirect response.");

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (responseContent.Contains("<center>cloudflare</center>"))
            {
                var useInvariantTradeResults = await settingsService.GetBool(SettingKeys.UseInvariantTradeResults);
                var isChinese = gameLanguageProvider.IsChinese();
                if (isChinese && !useInvariantTradeResults)
                {
                    logger.LogWarning("[PoeTradeHandler] Invalid chinese settings. Throwing exception.");
                    throw new SidekickException("Sidekick failed to communicate with the trade API.", "The trade website requires authentication, which Sidekick does not support currently.", "Try using a different game language and/or force to search using English only in the settings.");
                }

                logger.LogWarning("[PoeTradeHandler] Received a cloudflare redirect. Letting the handler continue.");
            }
        }

        // Sidekick does not support authentication yet.
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new SidekickException("Sidekick failed to communicate with the trade API.", "The trade website requires authentication, which Sidekick does not support currently.", "Try using a different game language and/or force to search using English only in the settings.");
        }

        // 403 probably means a cloudflare issue.
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            logger.LogInformation("[PoeTradeHandler] Received 403 response, attempting to handle Cloudflare challenge");

            // Show WebView2 window and wait for challenge completion
            var success = await cloudflareService.StartCaptchaChallenge(request.RequestUri!, cancellationToken);
            if (!success)
            {
                logger.LogWarning("[PoeTradeHandler] Failed to complete Cloudflare challenge");
                return response;
            }

            // Retry the request with new cookies
            await cloudflareService.AddCookieToRequest(request);
            var retryResponse = await base.SendAsync(request, cancellationToken);
            if (retryResponse.IsSuccessStatusCode)
            {
                logger.LogInformation("[PoeTradeHandler] Successfully completed Cloudflare challenge and retried request");
            }
            else
            {
                logger.LogWarning("[PoeTradeHandler] Request still failed after completing Cloudflare challenge: {StatusCode}, {RequestHeaders}", retryResponse.StatusCode, request.Headers.ToString());
            }

            return retryResponse;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        string? body = null;
        if (request.Content != null)
        {
            body = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        logger.LogWarning("[PoeTradeHandler] Query Failed: {responseCode} {responseMessage}", response.StatusCode, content);
        logger.LogWarning("[PoeTradeHandler] Uri: {uri}", request.RequestUri);
        logger.LogWarning("[PoeTradeHandler] Body: {uri}", body);
        throw new ApiErrorException();
    }

    private async Task<HttpResponseMessage> HandleRedirect(HttpRequestMessage request, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        // Get redirect URL from the "Location" header
        var redirectUri = response.Headers.Location;
        logger.LogInformation("[PoeTradeHandler] Redirection status code detected.");
        if (redirectUri == null)
        {
            return response;
        }

        logger.LogInformation("[PoeTradeHandler] Redirecting to {redirectUri}.", redirectUri);

        request.RequestUri = redirectUri;

        // Retry the request with the new URI
        return await base.SendAsync(request, cancellationToken);
    }
}