using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Events_Tenant.Common.Interfaces;
using Events_Tenant.Common.Models;
using Events_TenantUserApp.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace Events_TenantUserApp.Tests.ControllerTests
{
    [TestClass]
    public class FindSeatsControllerTests
    {
        private readonly FindSeatsController _findSeatsController;

        public FindSeatsControllerTests(IStringLocalizer<FindSeatsController> localizer, IStringLocalizer<BaseController> baseLocalizer, ILogger<FindSeatsController> logger, IConfiguration configuration)
        {
            var mockTenantRepo = new Mock<ITenantRepository>();
            var eventSections = GetEventSections();
            mockTenantRepo.Setup(repo => repo.GetEvent(1, 12345)).Returns(GetEventModel());
            mockTenantRepo.Setup(repo => repo.GetEventSections(1, 12345)).Returns(eventSections);

            var mockCatalogRepo = new Mock<ICatalogRepository>();

            var seatSectionIds = eventSections.Result.ToList().Select(i => i.SectionId).ToList();
            mockTenantRepo.Setup(r => r.GetSections(seatSectionIds, 12345)).Returns(GetSeatSections());
            mockTenantRepo.Setup(r => r.GetSection(1, 12345)).Returns(GetSection());
            mockTenantRepo.Setup(r => r.GetTicketsSold(1, 1, 12345)).Returns(GetNumberOfTicketPurchased());
            mockTenantRepo.Setup(r => r.AddTickets(GetTicketModels(), 12345)).Returns(GetBooleanValue());
            mockTenantRepo.Setup(r => r.AddTicketPurchase(GetTicketPurchaseModel(), 12345)).Returns(GetTicketId());

            var mockUtilities = new Mock<IUtilities>();
            var mockLookupClient = new Mock<DnsClient.ILookupClient>();

            _findSeatsController = new FindSeatsController(mockTenantRepo.Object, mockCatalogRepo.Object, localizer, baseLocalizer, logger, configuration, mockLookupClient.Object);
        }

        [Fact]
        public void FindSeatsTests_EventId_Null()
        {
            var result = _findSeatsController.FindSeats("tenantName", 0);

            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.NotNull(redirectToActionResult.ControllerName);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Events", redirectToActionResult.ControllerName);
        }

        [Fact]
        public void FindSeatsTests_EventId_NotNull()
        {
            var result = _findSeatsController.FindSeats("tenantName", 1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<EventModel>>(viewResult.Model);
            Assert.Equal(1, model.Count());
        }

        [Fact]
        public void GetAvailableSeatsTest()
        {
            var result = _findSeatsController.GetAvailableSeats("tenantName", 1, 1);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("290", contentResult.Content);
        }

        [Fact]
        public void PurchaseTicketsTests()
        {
            var result = _findSeatsController.PurchaseTickets("tenantName", 1, 5, 100, 2, 1);

            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.NotNull(redirectToActionResult.ControllerName);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Events", redirectToActionResult.ControllerName);
        }

        private async Task<EventModel> GetEventModel()
        {
            return new EventModel
            {
                Date = DateTime.Now,
                EventId = 1,
                EventName = "String Serenades",
                SubTitle = "Contoso Chamber Orchestra"
            };
        }

        private async Task<int> GetTicketId()
        {
            return 11;
        }

        private async Task<bool> GetBooleanValue()
        {
            return true;
        }

        private async Task<int> GetNumberOfTicketPurchased()
        {
            return 10;
        }

        private async Task<List<EventSectionModel>> GetEventSections()
        {
            return new List<EventSectionModel>
            {
                new EventSectionModel
                {
                    EventId = 1,
                    Price = 100,
                    SectionId = 1
                },
                new EventSectionModel
                {
                    EventId = 2,
                    Price = 1500,
                    SectionId = 1
                }
            };
        }

        private async Task<List<SectionModel>> GetSeatSections()
        {
            return new List<SectionModel>
            {
                new SectionModel
                {
                    SeatRows = 10,
                    SectionId = 1,
                    SeatsPerRow = 30,
                    SectionName = "Main Auditorium Stage",
                    StandardPrice = 100
                },
                new SectionModel
                {
                    SeatRows = 10,
                    SectionId = 2,
                    SeatsPerRow = 30,
                    SectionName = "Main Auditorium Middle",
                    StandardPrice = 80
                }
            };
        }

        private async Task<SectionModel> GetSection()
        {
            return new SectionModel
            {
                SeatRows = 10,
                SectionId = 1,
                SeatsPerRow = 30,
                SectionName = "Main Auditorium Stage",
                StandardPrice = 100
            };
        }

        private TicketPurchaseModel GetTicketPurchaseModel()
        {
            return new TicketPurchaseModel
            {
                CustomerId = 5,
                PurchaseTotal = 100
            };
        }

        private List<TicketModel> GetTicketModels()
        {
            List<TicketModel> ticketModels = new List<TicketModel>();
            ticketModels.Add(
                new TicketModel
                {
                    SectionId = 1,
                    EventId = 1,
                    RowNumber = 1000,
                    SeatNumber = 1001
                }
            );
            return ticketModels;
        }
    }
}