using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers;
using Mitech.Web.Models;
using Mitech.Web.Services;
using Moq;

namespace Mitech.Tests.Controllers;

public class RecruitmentControllerTests
{
    private static Mock<ILanguageService> LangMock()
    {
        var m = new Mock<ILanguageService>();
        m.Setup(s => s.GetCurrent(It.IsAny<HttpContext>())).Returns("vi");
        return m;
    }

    private static JobPosition ActiveJob(string slug = "ky-su-co-khi") => new()
    {
        Slug      = slug,
        TitleVi   = "Kỹ sư cơ khí",
        TitleJa   = "機械エンジニア",
        TitleEn   = "Mechanical Engineer",
        SalaryVi  = "15 – 25 triệu VNĐ",
        SortOrder = 1,
        IsActive  = true
    };

    [Fact]
    public async Task Detail_UnknownSlug_ReturnsNotFound()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new RecruitmentController(ctx, LangMock().Object));

        var result = await controller.Detail("khong-ton-tai");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Detail_InactiveJob_ReturnsNotFound()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.JobPositions.Add(new JobPosition
        {
            Slug = "inactive-job", TitleVi = "Ẩn", TitleJa = "非表示", TitleEn = "Hidden",
            SortOrder = 1, IsActive = false
        });
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new RecruitmentController(ctx, LangMock().Object));
        var result = await controller.Detail("inactive-job");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Detail_ActiveJob_ReturnsViewWithJobModel()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.JobPositions.Add(ActiveJob());
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new RecruitmentController(ctx, LangMock().Object));
        var result = await controller.Detail("ky-su-co-khi");

        var view = Assert.IsType<ViewResult>(result);
        var job = Assert.IsType<JobPosition>(view.Model);
        Assert.Equal("ky-su-co-khi", job.Slug);
        Assert.Equal("Kỹ sư cơ khí", job.TitleVi);
        Assert.Equal("機械エンジニア", job.TitleJa);
    }

    [Fact]
    public async Task Detail_ReturnsCorrectJob_WhenMultipleExist()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.JobPositions.AddRange(
            ActiveJob("cong-nhan-cnc"),
            new JobPosition
            {
                Slug = "kiem-tra-vis", TitleVi = "Kiểm tra ngoại quan",
                TitleJa = "外観検査", TitleEn = "Visual Inspection",
                SortOrder = 2, IsActive = true
            }
        );
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new RecruitmentController(ctx, LangMock().Object));
        var result = await controller.Detail("kiem-tra-vis");

        var view = Assert.IsType<ViewResult>(result);
        var job = Assert.IsType<JobPosition>(view.Model);
        Assert.Equal("kiem-tra-vis", job.Slug);
        Assert.Equal("Kiểm tra ngoại quan", job.TitleVi);
    }

    [Fact]
    public async Task Detail_SlugMatchIsExact()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.JobPositions.Add(ActiveJob("ky-su"));
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new RecruitmentController(ctx, LangMock().Object));

        Assert.IsType<NotFoundResult>(await controller.Detail("ky-su-co-khi"));
        Assert.IsType<ViewResult>(await controller.Detail("ky-su"));
    }

    [Fact]
    public async Task Detail_SalaryFields_AreAccessible()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.JobPositions.Add(new JobPosition
        {
            Slug = "job-luong", TitleVi = "Công việc", TitleJa = "仕事", TitleEn = "Job",
            SalaryVi = "10 triệu", SalaryJa = "10百万", SalaryEn = "10M VND",
            SortOrder = 1, IsActive = true
        });
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new RecruitmentController(ctx, LangMock().Object));
        var result = await controller.Detail("job-luong");

        var view = Assert.IsType<ViewResult>(result);
        var job = Assert.IsType<JobPosition>(view.Model);
        Assert.Equal("10 triệu", job.SalaryVi);
        Assert.Equal("10百万", job.SalaryJa);
        Assert.Equal("10M VND", job.SalaryEn);
    }
}
