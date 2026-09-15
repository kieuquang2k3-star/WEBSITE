using Mitech.Web.Helpers;

namespace Mitech.Tests.Helpers;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Công nhân vận hành máy CNC", "cong-nhan-van-hanh-may-cnc")]
    [InlineData("Kiểm tra ngoại quan (VIS)", "kiem-tra-ngoai-quan-vis")]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  spaces  ", "spaces")]
    public void Generate_ProducesExpectedSlug(string input, string expected)
    {
        Assert.Equal(expected, SlugHelper.Generate(input));
    }

    [Fact]
    public void Generate_EmptyInput_ReturnsFallback()
    {
        var result = SlugHelper.Generate(string.Empty);
        Assert.StartsWith("vi-tri-", result);
        Assert.Equal(15, result.Length); // "vi-tri-" + 8 chars
    }

    [Fact]
    public void Generate_SpecialCharsOnly_ReturnsFallback()
    {
        var result = SlugHelper.Generate("!@#$%");
        Assert.StartsWith("vi-tri-", result);
        Assert.Equal(15, result.Length);
    }
}
