using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Users.Api.Contracts;
using Users.Api.Controllers;
using Users.Api.Mappers;
using Users.Api.Models;
using Users.Api.Services;
using Xunit;

namespace Users.Api.Tests.Unit.ApiLayer;

public class UserControllerTests
{
    private readonly UserController _sut;
    private readonly IUserService _userService = Substitute.For<IUserService>();

    public UserControllerTests()
    {
        _sut = new UserController(_userService);
    }

    [Fact]
    public async Task GetById_ReturnOkAndObject_WhenUserExists()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Nick Chapsas"
        };
        _userService.GetByIdAsync(user.Id).Returns(user);
        var userResponse = user.ToUserResponse();

        // Act
        var result = (OkObjectResult)await _sut.GetById(user.Id);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Value.Should().BeEquivalentTo(userResponse);
    }

    [Fact]
    public async Task GetById_ReturnNotFound_WhenUserDoesntExists()
    {
        // Arrange
        _userService.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        // Act
        var result = (NotFoundResult)await _sut.GetById(Guid.NewGuid());

        // Assert
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAll_ShouldReturnEmptyList_WhenNoUsersExist()
    {
        // Arrange
        _userService.GetAllAsync().Returns(Enumerable.Empty<User>());

        // Act
        var result = (OkObjectResult)await _sut.GetAll();

        // Assert
        result.StatusCode.Should().Be(200);
        result.Value.As<IEnumerable<UserResponse>>().Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ShouldReturnUsersResponse_WhenUsersExist()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Nick Chapsas"
        };
        var users = new[] { user };
        var usersResponse = users.Select(x => x.ToUserResponse());
        _userService.GetAllAsync().Returns(users);

        // Act
        var result = (OkObjectResult)await _sut.GetAll();

        // Assert
        result.StatusCode.Should().Be(200);
        result.Value.As<IEnumerable<UserResponse>>().Should().BeEquivalentTo(usersResponse);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAndObject_WhenUserIsCreated()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Michael Piarulli"
        };
        var createUserRequest = new CreateUserRequest
        {
            FullName = user.FullName
        };
        
        _userService.CreateAsync(Arg.Do<User>(u => user = u)).Returns(true);        

        // Act
        var result = (CreatedAtActionResult)await _sut.Create(createUserRequest);

        // Assert
        var userResponse = user.ToUserResponse();
        result.StatusCode.Should().Be(201);
        result.Value.As<UserResponse>().Should().BeEquivalentTo(userResponse);
        result.RouteValues!["id"].Should().BeEquivalentTo(user.Id);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenUserIsNotCreated()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Michael Piarulli"
        };
        var createUserRequest = new CreateUserRequest
        {
            FullName = user.FullName
        };
        var userResponse = user.ToUserResponse();

        _userService.CreateAsync(Arg.Any<User>()).Returns(false);

        // Act
        var result = (BadRequestResult)await _sut.Create(createUserRequest);

        // Assert
        result.StatusCode.Should().Be(400);        
    }

    [Fact]
    public async Task DeleteById_ReturnOk_WhenUserIsDeleted()
    {
        // Arrange
        var id = Guid.NewGuid();
        
        _userService.DeleteByIdAsync(Arg.Any<Guid>()).Returns(true);        

        // Act
        var result = (OkResult)await _sut.DeleteById(id);

        // Assert
        result.StatusCode.Should().Be(200);        
    }

    [Fact]
    public async Task DeleteById_ReturnNotFound_WhenUserDoesntExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        _userService.DeleteByIdAsync(Arg.Any<Guid>()).Returns(false);

        // Act
        var result = (NotFoundResult)await _sut.DeleteById(id);

        // Assert
        result.StatusCode.Should().Be(404);
    }
}
