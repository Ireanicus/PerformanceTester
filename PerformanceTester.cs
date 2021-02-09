using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CliWrap;
using NLog;

public class PerformanceTester
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public string OutputPath { get; }
    public string WorkingPath { get; }
    public int IterationCount { get; } = 100;

    public PerformanceTester(Options options)
    {
        OutputPath = options.OutputPath;
        WorkingPath = options.WorkingPath;
    }

    public async Task<TestResult> Run()
    {
        Logger.Info("Test started");
        var timer = Stopwatch.StartNew();

        EnsureOutputDirectoryCreated();
        EnsureWorkingDirectoryCreated();

        var iterationResults = new List<TestIteration>();

        for (int i = 0; i < IterationCount; i++)
            iterationResults.Add(await ExecuteIteration(i + 1));



        Logger.Info($"Test ended. Total duration {timer.Elapsed.ToString()}");

        return new TestResult { Iterations = iterationResults };
    }

    private async Task<TestIteration> ExecuteIteration(int iterationNumber)
    {
        var timer = Stopwatch.StartNew();
        var stepsResults = await ExecuteSteps(iterationNumber);

        return new TestIteration
        {
            Iteration = iterationNumber,
            Duration = timer.Elapsed,
            Steps = stepsResults
        };
    }

    private async Task<IEnumerable<TestStep>> ExecuteSteps(int iterationNumber)
    {
        return new[] 
        {
            await ExecuteStep("git", "init"),
            await ExecuteStep("dotnet", "new mvc --force"),
            await ExecuteStep("dotnet", "restore"),
            await ExecuteStep("dotnet", "build"),
            await ExecuteStep("git", "status"),
            await ExecuteStep("git", "add *"),
            await ExecuteStep("git", $"commit -m \"Iteration {iterationNumber} add dotnet\""),
            await ExecuteStep("git", "rm *"),
            await ExecuteStep("git", "status"),
            await ExecuteStep("git", $"commit -m \"Iteration {iterationNumber} remove dotnet\""),
            await ExecuteStep("ng", "new test-app --defaults"),
            await ExecuteStep("ng", "build", $"{WorkingPath}/test-app"),
            await ExecuteStep("git", $"reset --hard"),
            await ExecuteStep("git", $"clean -fdx"),
            await ExecuteStep("git", $"checkout HEAD~1"),
            await ExecuteStep("git", $"checkout master")
        };
    }

    private async Task<TestStep> ExecuteStep(string executable, string arguments, string workingPath = null)
    {

        Logger.Info($"Executing: {executable} {arguments}");

        var result = await Cli.Wrap(executable)
            .WithWorkingDirectory(workingPath ?? WorkingPath)
            .WithArguments(arguments)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(text => Logger.Info($"> {text}")))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(text => Logger.Error($"> {text}")))
            .ExecuteAsync();

        Logger.Info($"Command run time: {result.RunTime.ToString()}");
        return new TestStep { Name = $"{executable} {arguments}", Duration = result.RunTime };
    }

    private void EnsureOutputDirectoryCreated()
    {
        EnsureDirectoryCreated(OutputPath);
    }

    private void EnsureWorkingDirectoryCreated()
    {
        EnsureDirectoryCreated(WorkingPath);
    }

    private static void EnsureDirectoryCreated(string directoryPath)
    {
        Logger.Debug($"Determining if {directoryPath} is present");

        if (!Directory.Exists(directoryPath))
        {
            Logger.Debug($"Directory not exists. Attempting to create {directoryPath}");
            Directory.CreateDirectory(directoryPath);
        }
        else
        {
            Logger.Debug($"Directory {directoryPath} already exists");
        }
    }
}