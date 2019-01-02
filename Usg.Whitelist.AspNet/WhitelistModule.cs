﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Usg.Whitelist
{
    public class StaticWhitelistModule : IHttpModule
    {
        ISet<IPAddress> _whitelist;

        void beginRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;

            if (_whitelist == null)
            {
                // Have to do this here becase MapPath() isn't allowed in
                // Init().
                string path = application.Server.MapPath(
                    ConfigurationManager.AppSettings["whitelist:Path"]);
                string data = File.ReadAllText(path);

                _whitelist = WhitelistParser.Parse(data);
            }

            var request = application.Context.Request;
            var ip = IPAddress.Parse(request.UserHostAddress);

            if (!_whitelist.Contains(ip))
            {
                var response = application.Context.Response;
                response.StatusCode = 401;
                response.End();
            }
        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += beginRequest;
        }

        public void Dispose() { }
    }

    public class HostedWhitelistModule : IHttpModule
    {
        HttpClient _client;
        string _url;

        async Task beginRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var request = application.Context.Request;
            var ip = IPAddress.Parse(request.UserHostAddress);

            var data = await _client.GetStringAsync(_url);
            var whitelist = WhitelistParser.Parse(data);

            if (!whitelist.Contains(ip))
            {
                var response = application.Context.Response;
                response.StatusCode = 401;
                response.End();
            };
        }

        public void Init(HttpApplication context)
        {
            _client = new HttpClient();
            _url = ConfigurationManager.AppSettings["whitelist:Url"];

            var handler = new EventHandlerTaskAsyncHelper(beginRequest);

            context.AddOnBeginRequestAsync(
                handler.BeginEventHandler,
                handler.EndEventHandler);
        }

        public void Dispose() { }
    }
}
