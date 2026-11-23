using System.IO;
using MagnumTweaker.Services;

namespace MagnumTweaker.Tests;

public sealed class RestorePointTests
{
    [Fact]
    public async Task CreationIsConfirmedOnlyAfterLookingUpThisRequestsPoint()
    {
        string? command = null;
        var result = await RestorePoint.CreateAsync(script =>
        {
            command = script;
            return Task.FromResult("MAGNUM_RESTORE_POINT_CREATED");
        });

        Assert.Equal(RestorePointResult.Created, result);
        Assert.Matches(@"\$description='Magnum Tweaker [a-f0-9]{32}'", command!);
        Assert.Contains("Checkpoint-Computer -Description $description -RestorePointType MODIFY_SETTINGS", command);
        Assert.Contains("Get-ComputerRestorePoint | Where-Object { $_.Description -eq $description }", command);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Another restore point already exists.")]
    public async Task CreationRemainsUnconfirmedWithoutTheMarker(string output)
    {
        Assert.Equal(RestorePointResult.NotConfirmed, await RestorePoint.CreateAsync(_ => Task.FromResult(output)));
    }

    [Fact]
    public async Task PowerShellFailureKeepsThePointUnconfirmed()
    {
        var result = await RestorePoint.CreateAsync(_ => Task.FromException<string>(new IOException()));
        Assert.Equal(RestorePointResult.NotConfirmed, result);
    }

    [Fact]
    public async Task WorkerRejectsUnexpectedArguments()
    {
        Assert.Equal(RestorePointResult.RequiresAdmin,
            await RestorePoint.RunWorkerAsync([RestorePoint.WorkerCommand, "unexpected"]));
    }
}
