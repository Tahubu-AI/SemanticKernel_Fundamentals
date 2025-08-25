using TahubuAPI_Sample.Models;

namespace TahubuAPI_Sample.Services;

public class EmployeeRepository
{
    private readonly List<Models.Employee> _employees;

    public EmployeeRepository()
    {
        _employees = GenerateEmployees();
    }

    public IEnumerable<Models.Employee> GetAll() => _employees;

    public Models.Employee? GetById(int id) => _employees.FirstOrDefault(c => c.Id == id);

    public IEnumerable<Models.Employee> SearchByName(string companyName) =>
        _employees.Where(c => c.Name.Contains(companyName, StringComparison.OrdinalIgnoreCase));

    public void Upsert(Models.Employee employee)
    {
        if (_employees.Any(c => c.Id == employee.Id))
            Update(employee);
        else
            Add(employee);
    }

    public void Add(Models.Employee employee)
    {
        employee.Id = _employees.Any() ? _employees.Max(c => c.Id) + 1 : 1;
        _employees.Add(employee);
    }

    public bool Update(Models.Employee employee)
    {
        var index = _employees.FindIndex(c => c.Id == employee.Id);
        if (index == -1) return false;

        _employees[index] = employee;
        return true;
    }

    public bool Delete(int id)
    {
        var employee = _employees.FirstOrDefault(c => c.Id == id);
        if (employee == null) return false;

        _employees.Remove(employee);
        return true;
    }

    private List<Models.Employee> GenerateEmployees()
    {
        var employees = new List<Models.Employee>
        {
            new Models.Employee { Id = 1, Name = "Kyle Bunting", Address = "17230 Jackson Creek Pkwy", City = "Monument", State = "CO", Zip = "80132", Email = "kyle@tahubu.com" },
            new Models.Employee { Id = 2, Name = "Joel Hulen", Address = "500 S Battlefield Blvd", City = "Chesapeake", State = "VA", Zip = "23322", Email = "joel@tahubu.com" },
            new Models.Employee { Id = 3, Name = "Lino Tadros", Address = "2451 S Hiawassee Rd", City = "Orlando", State = "FL", Zip = "32835", Email = "lino@tahubu.com" },
        };

        return employees;
    }
}