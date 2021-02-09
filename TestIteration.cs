using System;
using System.Collections.Generic;

public class TestIteration
{
    public int Iteration { get; set; }
    public TimeSpan Duration { get; set; }
    public IEnumerable<TestStep> Steps { get; set; }
}