using System;
using CommandLine;

public class Options
{
    [Option('o', "output", Required = false, Default = "./output", HelpText = "Set output path")]
    public string OutputPath { get; set; }

    [Option('w', "workPath", Required = false, HelpText = "Set working path")]
    public string WorkingPath { get; set; } = $"../Tests/Test_{DateTime.Now.ToFileTimeUtc()}";
}