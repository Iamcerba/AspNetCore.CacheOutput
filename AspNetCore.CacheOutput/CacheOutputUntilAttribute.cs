using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AspNetCore.CacheOutput.Time;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace AspNetCore.CacheOutput
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class CacheOutputUntilAttribute : CacheOutputBaseAttribute
    {
        public string ServerTimes { get; set; }
        public string ClientTimes { get; set; }

        protected override void ResetCacheTimeQuery()
        {
            var serverTimes = ServerTimes.Split(",").Select(x => TimeSpan.Parse(ServerTimes)).ToList();

            var clientTimes = ServerTimes.Split(",").Select(x => TimeSpan.Parse(ClientTimes)).ToList();
            
            var serverTimeSpan = CalculateTimeSpan(serverTimes);
            var clientTimeSpan = CalculateTimeSpan(clientTimes);

            CacheTimeQuery = new ShortTime(serverTimeSpan, clientTimeSpan, null);
        }

        //protected override void EnsureCacheTimeQuery(bool isExpired = false)
        //{
        //    if (isExpired || CacheTimeQuery == null)
        //    {
        //        ResetCacheTimeQuery();
        //    }
        //}

        private static int CalculateTimeSpan(List<TimeSpan> configuredTimes)
        {
            var timeSpan = 0;
            var timeOfDay = DateTime.Now.TimeOfDay;
            var nextClosestTime = GetNextClosestTime(configuredTimes, timeOfDay);

            if (nextClosestTime != default)
            {
                if (nextClosestTime > timeOfDay)
                {
                    timeSpan = (int)nextClosestTime.Subtract(timeOfDay).TotalSeconds;
                }
                else
                {
                    timeSpan = (int)TimeSpan.FromHours(24).Subtract(timeOfDay.Subtract(nextClosestTime)).TotalSeconds;
                }
            }

            return timeSpan;
        }

        private static TimeSpan GetNextClosestTime(List<TimeSpan> configuredTimes, TimeSpan timeOfDay)
        {
            var orderedTimes = configuredTimes.OrderBy(x => x).ToList();
            var count = orderedTimes.Count;
            TimeSpan nextClosestTime = orderedTimes[0];

            if (count == 1 || timeOfDay < orderedTimes[0] || timeOfDay >= orderedTimes[count - 1])
            {
                nextClosestTime = orderedTimes[0];
            }
            else
            {
                for (int i = 0; i < count - 1; i++)
                {
                    if (timeOfDay >= orderedTimes[i] && timeOfDay < orderedTimes[i + 1])
                    {
                        nextClosestTime = orderedTimes[i];
                        break;
                    }
                }
            }

            return nextClosestTime;
        }
    }
}
