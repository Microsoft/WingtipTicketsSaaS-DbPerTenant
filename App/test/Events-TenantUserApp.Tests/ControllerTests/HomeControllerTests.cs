using System.Collections.Generic;
using System.Threading.Tasks;
using Events_Tenant.Common.Interfaces;
using Events_Tenant.Common.Models;
using Events_TenantUserApp.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace Events_TenantUserApp.Tests.ControllerTests
{
    //https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/testing
    //https://docs.microsoft.com/en-us/dotnet/articles/core/testing/unit-testing-with-dotnet-test
    //https://github.com/aspnet/Tooling/issues/664
    //http://stackoverflow.com/questions/40190679/how-to-reference-an-asp-net-core-project-from-a-full-net-framework-test-project

    [TestClass]
    public class HomeControllerTests
    {
        private readonly HomeController _homeController;

        public HomeControllerTests(ILogger<HomeController> logger)
        {
            // Arrange
            var mockCatalogRepo = new Mock<ICatalogRepository>();
            var mockTenantRepo = new Mock<ITenantRepository>();

            mockCatalogRepo.Setup(repo => repo.GetAllTenants()).Returns(GetTenants());
            mockTenantRepo.Setup(repo => repo.GetVenueDetails(1234646)).Returns(GetVenueDetails());

            var mockUtilities = new Mock<IUtilities>();

            _homeController = new HomeController(mockCatalogRepo.Object, mockTenantRepo.Object, logger, mockUtilities.Object);
        }

        [Fact]
        public void Index_GetAllTenantDetails()
        {
            //Act
            var result = _homeController.Index();

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.NotNull(redirectToActionResult.ControllerName);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }

        private async Task<VenueModel> GetVenueDetails()
        {
            return new VenueModel
            {
                AdminEmail = "adminEmail",
                AdminPassword = "Password",
                CountryCode = "USA",
                PostalCode = "123",
                VenueName = "Venue Name",
                VenueType = "classic"
            };
        }

        private async Task<List<TenantModel>> GetTenants()
        {
            return new List<TenantModel>
            {
                new TenantModel
                {
                    TenantName = "dogwooddojo",
                    VenueName = "Dogwood Dojo",
                    ServicePlan = "Standard"
                },
                new TenantModel
                {
                    TenantName = "contosoconcerthall",
                    VenueName = "Contoso Concert Hall",
                    ServicePlan = "Standard"
                },
                new TenantModel
                {
                    TenantName = "fabrikamjazzclub",
                    VenueName = "Fabrikam Jazz Club",
                    ServicePlan = "Standard"
                }
            };
        }
    }
}