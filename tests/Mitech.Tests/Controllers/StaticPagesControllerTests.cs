using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers;
using Mitech.Web.Models;
using Mitech.Web.Services;
using Moq;

namespace Mitech.Tests.Controllers;

public class StaticPagesControllerTests
{
    private static Mock<ILanguageService> LangMock()
    {
        var m = new Mock<ILanguageService>();
        m.Setup(s => s.GetCurrent(It.IsAny<HttpContext>())).Returns("vi");
        return m;
    }

    [Fact]
    public void Purchase_ReturnsView()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new StaticPagesController(LangMock().Object, ctx));

        Assert.IsType<ViewResult>(controller.Purchase());
    }

    [Fact]
    public void ActionPlan_ReturnsView()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new StaticPagesController(LangMock().Object, ctx));

        Assert.IsType<ViewResult>(controller.ActionPlan());
    }

    [Fact]
    public void Privacy_ReturnsView()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new StaticPagesController(LangMock().Object, ctx));

        Assert.IsType<ViewResult>(controller.Privacy());
    }

    [Fact]
    public void Security_ReturnsView()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new StaticPagesController(LangMock().Object, ctx));

        Assert.IsType<ViewResult>(controller.Security());
    }

    [Fact]
    public void Sitemap_ReturnsView()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new StaticPagesController(LangMock().Object, ctx));

        Assert.IsType<ViewResult>(controller.Sitemap());
    }

    [Fact]
    public async Task TuyenDung_ReturnsOnlyActiveJobs()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.JobPositions.AddRange(
            new JobPosition { Slug = "active", TitleVi = "Công việc A", TitleJa = "仕事A", TitleEn = "Job A", SortOrder = 1, IsActive = true },
            new JobPosition { Slug = "inactive", TitleVi = "Công việc B", TitleJa = "仕事B", TitleEn = "Job B", SortOrder = 2, IsActive = false }
        );
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new StaticPagesController(LangMock().Object, ctx));
        var result = await controller.TuyenDung();

        var view = Assert.IsType<ViewResult>(result);
        var jobs = Assert.IsAssignableFrom<IEnumerable<JobPosition>>(view.ViewData["Jobs"]).ToList();
        Assert.Single(jobs);
        Assert.Equal("active", jobs[0].Slug);
    }

    [Fact]
    public async Task TuyenDung_JobsOrderedBySortOrder()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.JobPositions.AddRange(
            new JobPosition { Slug = "b", TitleVi = "Công việc B", TitleJa = "仕事B", TitleEn = "Job B", SortOrder = 2, IsActive = true },
            new JobPosition { Slug = "a", TitleVi = "Công việc A", TitleJa = "仕事A", TitleEn = "Job A", SortOrder = 1, IsActive = true },
            new JobPosition { Slug = "c", TitleVi = "Công việc C", TitleJa = "仕事C", TitleEn = "Job C", SortOrder = 3, IsActive = true }
        );
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new StaticPagesController(LangMock().Object, ctx));
        var result = await controller.TuyenDung();

        var view = Assert.IsType<ViewResult>(result);
        var jobs = Assert.IsAssignableFrom<IEnumerable<JobPosition>>(view.ViewData["Jobs"]).ToList();
        Assert.Equal("a", jobs[0].Slug);
        Assert.Equal("b", jobs[1].Slug);
        Assert.Equal("c", jobs[2].Slug);
    }

    [Fact]
    public async Task TuyenDung_EmptyDatabase_ReturnsEmptyJobList()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();

        var controller = ControllerTestHelper.Create(new StaticPagesController(LangMock().Object, ctx));
        var result = await controller.TuyenDung();

        var view = Assert.IsType<ViewResult>(result);
        var jobs = Assert.IsAssignableFrom<IEnumerable<JobPosition>>(view.ViewData["Jobs"]).ToList();
        Assert.Empty(jobs);
    }
}
