using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NexusGear.Controllers;
using NexusGear.Models;
using NexusGear.ViewModels;
using Xunit;

namespace NexusGear.Tests;

public class PreviewAndErrorTests
{
    [Fact]
    public void CheckoutController_Index_ReturnsRoadmapPreviewWithA5AndA6Requirements()
    {
        // Arrange
        var controller = new CheckoutController();

        // Act
        var result = controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("~/Views/Shared/RoadmapPreview.cshtml", viewResult.ViewName);
        var model = Assert.IsType<RoadmapPreviewViewModel>(viewResult.Model);

        Assert.Equal("Checkout & Stripe Sandbox Payments", model.ModuleTitle);
        Assert.Contains(model.RequirementTags, t => t.Contains("A5"));
        Assert.Contains(model.RequirementTags, t => t.Contains("A6"));
        Assert.NotEmpty(model.PlannedFeatures);
    }

    [Fact]
    public void OrdersController_Index_ReturnsRoadmapPreviewWithA8Requirement()
    {
        // Arrange
        var controller = new OrdersController();

        // Act
        var result = controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("~/Views/Shared/RoadmapPreview.cshtml", viewResult.ViewName);
        var model = Assert.IsType<RoadmapPreviewViewModel>(viewResult.Model);

        Assert.Equal("Customer Order History & Tracking", model.ModuleTitle);
        Assert.Contains(model.RequirementTags, t => t.Contains("A8"));
    }

    [Fact]
    public void AdminController_Dashboard_ReturnsRoadmapPreviewWithA9AndA10Requirements()
    {
        // Arrange
        var controller = new AdminController();

        // Act
        var result = controller.Dashboard();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("~/Views/Shared/RoadmapPreview.cshtml", viewResult.ViewName);
        var model = Assert.IsType<RoadmapPreviewViewModel>(viewResult.Model);

        Assert.Equal("Custom Administration Panel & Analytics", model.ModuleTitle);
        Assert.Contains(model.RequirementTags, t => t.Contains("A9"));
        Assert.Contains(model.RequirementTags, t => t.Contains("A10"));
    }

    [Fact]
    public void HomeController_Error_With404StatusCode_SetsAppropriateTitleAndDescription()
    {
        // Arrange
        var controller = new HomeController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = controller.Error(404);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ErrorViewModel>(viewResult.Model);

        Assert.Equal(404, model.StatusCode);
        Assert.Equal("Page Not Found", model.Title);
        Assert.Contains("hardware component", model.Description);
    }

    [Fact]
    public void HomeController_Error_Default_SetsExpectedErrorModel()
    {
        // Arrange
        var controller = new HomeController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = controller.Error(500);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ErrorViewModel>(viewResult.Model);

        Assert.Equal(500, model.StatusCode);
        Assert.Equal("An Unexpected Error Occurred", model.Title);
    }
}

