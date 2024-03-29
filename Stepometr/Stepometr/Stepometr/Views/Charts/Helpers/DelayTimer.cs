﻿using Stepometer.Controls.Charts.Abstracts;

namespace Stepometer.Controls.Charts.Helpers
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A timer based on Task.Delay.
    /// </summary>
    public class DelayTimer : ITimer
    {
        /// <summary>
        /// Start the loop with specified interval and step action. The loop stops as
        /// soon that the step return false.
        /// </summary>
        /// <param name="interval">The interval.</param>
        /// <param name="step">The step.</param>
        public async void Start(TimeSpan interval, Func<bool> step)
        {
            var shouldContinue = step();

            while (shouldContinue)
            {
                await Task.Delay(interval);
                shouldContinue = step();
            }
        }
    }
}
