using MuuqWear.Model.DTO.HelpCenterDTO;
using Xunit;

namespace MuuqWearApi.Tests.Help;

public class HelpArticleHelpersTests
{
    [Fact]
    public void VoteNormalize_AcceptsLikeAndDislike_CaseInsensitive()
    {
        Assert.Equal(HelpArticleVoteValue.Like, HelpArticleVoteValue.Normalize("LIKE"));
        Assert.Equal(HelpArticleVoteValue.Dislike, HelpArticleVoteValue.Normalize("Dislike"));
        Assert.Null(HelpArticleVoteValue.Normalize("meh"));
        Assert.Null(HelpArticleVoteValue.Normalize(" "));
    }

    [Fact]
    public void ArticleStatusNormalize_DefaultsDraft()
    {
        Assert.Equal(HelpArticleStatus.Draft, HelpArticleStatus.Normalize(null));
        Assert.Equal(HelpArticleStatus.Published, HelpArticleStatus.Normalize("Published"));
        Assert.Equal(HelpArticleStatus.Draft, HelpArticleStatus.Normalize("weird"));
    }

    [Fact]
    public void Categories_ContainExpectedValues()
    {
        Assert.Contains("Product Info", HelpArticleCategory.All);
        Assert.Contains("Orders", HelpArticleCategory.All);
    }
}
