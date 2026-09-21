using Backseat.Core;

namespace Backseat.Core.Tests;

public sealed class TargetDescriptorTests
{
    [Fact]
    public void ProcessId_Is_Required()
    {
        var descriptor = new TargetDescriptor { ProcessId = 1234 };

        Assert.Equal(1234u, descriptor.ProcessId);
    }

    [Fact]
    public void ZeroProcessId_Is_Rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TargetDescriptor { ProcessId = 0 });
    }

    [Fact]
    public void WindowId_IsOptional()
    {
        var processOnly = new TargetDescriptor { ProcessId = 42 };
        var windowScoped = new TargetDescriptor { ProcessId = 42, WindowId = 99 };

        Assert.Null(processOnly.WindowId);
        Assert.Equal(99ul, windowScoped.WindowId);
    }

    [Fact]
    public void Metadata_Is_Preserved()
    {
        var descriptor = new TargetDescriptor
        {
            ProcessId = 42,
            WindowId = 99,
            ExecutablePath = @"C:\Windows\System32\notepad.exe",
            Title = "Untitled - Notepad",
        };

        Assert.Equal(@"C:\Windows\System32\notepad.exe", descriptor.ExecutablePath);
        Assert.Equal("Untitled - Notepad", descriptor.Title);
    }
}
