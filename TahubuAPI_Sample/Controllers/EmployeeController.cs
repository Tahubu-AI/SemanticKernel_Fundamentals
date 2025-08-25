using Microsoft.AspNetCore.Mvc;
using TahubuAPI_Sample.Models;
using TahubuAPI_Sample.Services;
using System.ComponentModel;

namespace TahubuAPI_Sample.Controllers;

/// <summary>
/// Manages Employee information and operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EmployeeController : ControllerBase
{
    private readonly EmployeeRepository _repository;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(EmployeeRepository repository, ILogger<EmployeeController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all Employees
    /// </summary>
    /// <returns>A list of all Employees</returns>
    /// <response code="200">Returns the list of Employees</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Models.Employee>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        return Ok(_repository.GetAll());
    }

    /// <summary>
    /// Retrieves a specific Employee by ID
    /// </summary>
    /// <param name="id">The ID of the Employee to retrieve</param>
    /// <returns>The requested Employee</returns>
    /// <response code="200">Returns the requested Employee</response>
    /// <response code="404">If the Employee is not found</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Models.Employee), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(int id)
    {
        var employee = _repository.GetById(id);
        if (employee == null) return NotFound();
        return Ok(employee);
    }

    /// <summary>
    /// Searches for Employees by company name
    /// </summary>
    /// <param name="companyName">The company name to search for</param>
    /// <returns>A list of matching Employees</returns>
    /// <response code="200">Returns the list of matching employees</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<Models.Employee>), StatusCodes.Status200OK)]
    public IActionResult Search([FromQuery] string companyName)
    {
        return Ok(_repository.SearchByName(companyName));
    }

    /// <summary>
    /// Creates or updates an Employee
    /// </summary>
    /// <param name="Employee">The Employee information to create</param>
    /// <returns>The created Employee</returns>
    /// <response code="201">Returns the newly created Employee</response>
    /// <response code="400">If the Employee data is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(Models.Employee), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Create(Models.Employee employee)
    {
        _repository.Upsert(employee);
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
    }


    /// <summary>
    /// Deletes a specific Employee
    /// </summary>
    /// <param name="id">The ID of the Employee to delete</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the Employee was successfully deleted</response>
    /// <response code="404">If the Employee is not found</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id)
    {
        var success = _repository.Delete(id);
        if (!success) return NotFound();

        return NoContent();
    }
}