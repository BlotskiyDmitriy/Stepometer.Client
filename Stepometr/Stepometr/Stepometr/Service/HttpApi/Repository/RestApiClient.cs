﻿using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Stepometer.Service.LoggerService;
using Stepometer.Utils;

namespace Stepometer.Service.HttpApi.Repository
{
    public class RestApiClient<TData> : IRestApiClient<TData> where TData : class
    {
        private  ILogService _logService { get; }

        internal HttpClient HttpClient { get; private set; }

        private readonly StringBuilder _baseUrlStringBuilder = new StringBuilder(Constants.Constants.BaseUrlDroid);

        public RestApiClient(HttpClient httpClient)
        {
            HttpClient = httpClient;
            _logService = DependencyResolver.Get<ILogService>();
        }

        public async Task<TData> GetDataAsync(string controllerUrl)
        {
            try
            {
                var response = await HttpClient.GetAsync(_baseUrlStringBuilder.Append(controllerUrl).ToString());

                await _logService.TrackResponseAsync(response);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception();
                }

                return SerializeDeserialize<TData>.ConvertFromJson(await response.Content.ReadAsStringAsync());
            }
            catch (Exception e)
            {
                _logService.TrackException(e, MethodBase.GetCurrentMethod()?.Name);
                throw e;
            }
        }

        public async Task<TData> PostDataAsync(string controllerUrl, TData data)
        {
            try
            {
                var response = await HttpClient.PostAsync(_baseUrlStringBuilder.Append(controllerUrl).ToString(),
                    new StringContent(SerializeDeserialize<TData>.ConvertToJson(data)));

                await _logService.TrackResponseAsync(response);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception();
                }

                return SerializeDeserialize<TData>.ConvertFromJson(await response.Content.ReadAsStringAsync());
            }
            catch (Exception e)
            {
                _logService.TrackException(e, MethodBase.GetCurrentMethod()?.Name);
                throw e;
            }

        }

        public async Task<TData> PutDataAsync(string controllerUrl, TData data)
        {
            try
            {
                var response = await HttpClient.PutAsync(_baseUrlStringBuilder.Append(controllerUrl).ToString(),
                    new StringContent(SerializeDeserialize<TData>.ConvertToJson(data)));

                await _logService.TrackResponseAsync(response);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception();
                }

                return SerializeDeserialize<TData>.ConvertFromJson(await response.Content.ReadAsStringAsync());
            }
            catch (Exception e)
            {
                _logService.TrackException(e, MethodBase.GetCurrentMethod()?.Name);
                throw e;
            }
        }

        public async Task<TData> DeleteDataAsync(string controllerUrl, TData data)
        {
            try
            {
                var response = await HttpClient.DeleteAsync(_baseUrlStringBuilder.Append(controllerUrl).ToString());

                await _logService.TrackResponseAsync(response);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception();
                }

                return SerializeDeserialize<TData>.ConvertFromJson(await response.Content.ReadAsStringAsync());
            }
            catch (Exception e)
            {
                _logService.TrackException(e, MethodBase.GetCurrentMethod()?.Name);
                throw e;
            }
        }
    }
}