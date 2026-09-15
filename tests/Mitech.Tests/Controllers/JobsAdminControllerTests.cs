using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers.Admin;
using Mitech.Web.Models;

namespace Mitech.Tests.Controllers;

public class JobsAdminControllerTests
{
    [Fact]
    public async Task Index_ReturnsJobs_OrderedBySortOrder()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.JobPositions.AddRange(
            new JobPosition { TitleVi = "B", SortOrder = 2, Slug = "b" },
            new JobPosition { TitleVi = "A", SortOrder = 1, Slug = "a" });
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new JobsAdminController(ctx));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var jobs = Assert.IsAssignableFrom<IEnumerable<JobPosition>>(view.Model).ToList();
        Assert.Equal("A", jobs[0].TitleVi);
        Assert.Equal("B", jobs[1].TitleVi);
    }

    [Fact]
    public void Create_Get_ReturnsView()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new JobsAdminController(ctx));

        var result = controller.Create();

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<JobPosition>(view.Model);
    }

    [Fact]
    public async Task Create_Post_ValidJob_SavesAndRedirects()
    {
        var dbName = Guid.NewGuid().ToString();
        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new JobsAdminController(ctx));
        var job = new JobPosition { TitleVi = "Test Job", Slug = "test-job", SortOrder = 1 };

        var result = await controller.Create(job);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        using var verify = ControllerTestHelper.CreateInMemoryContext(dbName);
        Assert.Equal(1, await verify.JobPositions.CountAsync());
    }

    [Fact]
    public async Task Create_Post_DuplicateSlug_ReturnsView()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.JobPositions.Add(new JobPosition { TitleVi = "Existing", Slug = "existing", SortOrder = 1 });
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new JobsAdminController(ctx));
        var result = await controller.Create(new JobPosition { TitleVi = "New", Slug = "existing", SortOrder = 2 });

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Edit_Get_ReturnsView_WhenExists()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var seed = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            seed.JobPositions.Add(new JobPosition { TitleVi = "Test", Slug = "test", SortOrder = 1 });
            await seed.SaveChangesAsync();
        }
        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var id = ctx.JobPositions.First().Id;
        var controller = ControllerTestHelper.Create(new JobsAdminController(ctx));

        var result = await controller.Edit(id);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<JobPosition>(view.Model);
    }

    [Fact]
    public async Task Edit_Get_ReturnsNotFound_WhenMissing()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new JobsAdminController(ctx));

        var result = await controller.Edit(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Post_RemovesJob_AndRedirects()
    {
        var dbName = Guid.NewGuid().ToString();
        int jobId;
        using (var seed = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            var j = new JobPosition { TitleVi = "Del", Slug = "del", SortOrder = 1 };
            seed.JobPositions.Add(j);
            await seed.SaveChangesAsync();
            jobId = j.Id;
        }
        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new JobsAdminController(ctx));

        var result = await controller.Delete(jobId);

        Assert.IsType<RedirectToActionResult>(result);
        using var verify = ControllerTestHelper.CreateInMemoryContext(dbName);
        Assert.Equal(0, verify.JobPositions.Count());
    }
}
