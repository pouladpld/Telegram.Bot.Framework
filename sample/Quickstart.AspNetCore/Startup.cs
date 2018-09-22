﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quickstart.AspNetCore.Handlers;
using Quickstart.AspNetCore.Services;
using System;
using Telegram.Bot.Framework;
using Telegram.Bot.Framework.Abstractions;

namespace Quickstart.AspNetCore
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<BotOptions<EchoBot>>(Configuration.GetSection("EchoBot"));
            services.AddTransient<EchoBot>();

            services.AddScoped<ExceptionHandler>();
            services.AddScoped<WebhookLogger>();
            services.AddScoped<CallbackQueryHandler>();
            services.AddScoped<TextEchoer>();
            services.AddScoped<PingCommand>();
            services.AddScoped<StartCommand>();
            services.AddScoped<StickerHandler>();
            services.AddScoped<WeatherReporter>();
            services.AddScoped<UpdateMembersList>();

            services.AddScoped<IWeatherService, WeatherService>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                // get bot updates from Telegram via long-polling approach during development
                // this will disable Telegram webhooks
                app.UseTelegramBotLongPolling<EchoBot>(ConfigureBot(), startAfter: TimeSpan.FromSeconds(1));
            }
            else
            {
                app.UseHttpsRedirection();
                app.UseHsts();

                // use Telegram bot webhook middleware in higher environments
                app.UseTelegramBotWebhook<EchoBot>(ConfigureBot());
                // and make sure webhook is enabled
                app.EnsureWebhookSet<EchoBot>(baseUrl: "https://sample-tgbot.herokuapp.com");
            }

            app.Run(async context =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }

        private IBotBuilder ConfigureBot()
        {
            return new BotBuilder()
                .Use<ExceptionHandler>()
                .UseWhen(When.IsWebhook, branch => branch.Use<WebhookLogger>())
                .Map("callback_query", branch => branch.Use<CallbackQueryHandler>())
                .UseWhen(When.NewTextMessage, branch => branch.Use<TextEchoer>())
                .UseCommand<PingCommand>("ping")
                .UseCommand<StartCommand>("start")
                .MapWhen(When.StickerMessage, branch => branch.Use<StickerHandler>())
                .MapWhen(When.LocationMessage, branch => branch.Use<WeatherReporter>())
                .UseWhen(When.MembersChanged, branch => branch.Use<UpdateMembersList>())
                ;
        }
    }
}
