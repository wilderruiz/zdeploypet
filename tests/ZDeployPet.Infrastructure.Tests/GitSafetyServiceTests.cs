using ZDeployPet.Infrastructure;

namespace ZDeployPet.Infrastructure.Tests;

public sealed class GitSafetyServiceTests
{
    [Fact]
    public void UpsertManagedIgnoreBlock_AppendsBlockWithoutRemovingExistingRules()
    {
        string existing = "node_modules/\n.env\n";

        string updated = GitSafetyService.UpsertManagedIgnoreBlock(existing);

        Assert.Contains("node_modules/", updated, StringComparison.Ordinal);
        Assert.Contains(".env", updated, StringComparison.Ordinal);
        Assert.Contains(GitSafetyService.IgnoreBlockStart, updated, StringComparison.Ordinal);
        Assert.Contains(".zdeploypet/private/", updated, StringComparison.Ordinal);
        Assert.Contains("zdeploypet_*_ed25519", updated, StringComparison.Ordinal);
        Assert.DoesNotContain("*.pub", updated, StringComparison.Ordinal);
        Assert.Contains(GitSafetyService.IgnoreBlockEnd, updated, StringComparison.Ordinal);
    }

    [Fact]
    public void UpsertManagedIgnoreBlock_IsIdempotentAndRefreshesManagedSection()
    {
        string first = GitSafetyService.UpsertManagedIgnoreBlock("bin/\n");
        string second = GitSafetyService.UpsertManagedIgnoreBlock(first);

        Assert.Equal(first, second);
        Assert.Equal(1, Count(first, GitSafetyService.IgnoreBlockStart));
        Assert.Equal(1, Count(first, GitSafetyService.IgnoreBlockEnd));
    }

    private static int Count(string value, string token)
    {
        int count = 0;
        int index = 0;
        while ((index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }
        return count;
    }
}
