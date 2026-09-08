using DigitalBank.Application.DTOs;
using DigitalBank.Application.Interfaces;
using DigitalBank.Application.Services;
using DigitalBank.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace DigitalBank.UnitTests.Services;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        _sut = new CustomerService(_customerRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithNewEmail_CreatesCustomer()
    {
        var dto = new CreateCustomerDto("Karima", "Belkhatir", "karima@example.com", "514-555-0100", new DateTime(1995, 1, 1));
        _customerRepoMock.Setup(r => r.ExistsByEmailAsync(dto.Email, default)).ReturnsAsync(false);

        var result = await _sut.CreateAsync(dto);

        result.FirstName.Should().Be("Karima");
        result.Email.Should().Be("karima@example.com");
        _customerRepoMock.Verify(r => r.AddAsync(It.IsAny<Customer>(), default), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        var dto = new CreateCustomerDto("Karima", "Belkhatir", "karima@example.com", "514-555-0100", new DateTime(1995, 1, 1));
        _customerRepoMock.Setup(r => r.ExistsByEmailAsync(dto.Email, default)).ReturnsAsync(true);

        var act = async () => await _sut.CreateAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _customerRepoMock.Verify(r => r.AddAsync(It.IsAny<Customer>(), default), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerDoesNotExist_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _customerRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Customer?)null);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeNull();
    }
}
