using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CsvHelper;
using NLog;
using NLog.Config;

namespace Performer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(async options =>
                {
                    ConfigureLogging(options);

                    var result = await new PerformenceTester(options).Run();


                    using (var writer = new StreamWriter($"{options.OutputPath}\\file.csv"))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(result.Iterations.Select(iteration =>
                        {
                            var x = new ExpandoObject() as IDictionary<string, Object>;
                            x.Add("Iteration Number", iteration.Iteration);
                            x.Add("Total (ms)", iteration.Duration.TotalMilliseconds);

                            var stepNumber = 0;
                            foreach (var step in iteration.Steps)
                            {
                                stepNumber++;
                                x.Add($"Step {stepNumber}: {step.Name} (ms)", step.Duration.TotalMilliseconds);
                            }

                            return x as dynamic;
                        }));
                    }
                });
        }

        private static void ConfigureLogging(Options options)
        {
            var config = new LoggingConfiguration();

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = $"{options.OutputPath}/console.log" };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);

            // Apply config           
            NLog.LogManager.Configuration = config;
        }
    }
}
