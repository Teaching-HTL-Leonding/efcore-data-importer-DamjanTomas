using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using EFCoreWriting;
using Microsoft.EntityFrameworkCore;




var factory = new ApplicationDataContextFactory();
var context = factory.CreateDbContext(args: []);

/*await using (var transaction = await context.Database.BeginTransactionAsync())
{
    Customer? c1 = null;
    try
    {
        await context.OrderHeaders.ExecuteDeleteAsync();
        await context.Customers.ExecuteDeleteAsync();

        c1 = new Customer(0, "Hogward", "XY", "Scotland");
        var c2 = new Customer(0, "Hogward", "XYZ", "Scotland");
        context.Customers.AddRange(c1, c2);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch (Exception ex)
    {
        System.Console.WriteLine(ex.Message);
    }
    System.Console.WriteLine($"Customer 1: {c1!.ID}");
}*/


await context.OrderLines.ExecuteDeleteAsync();
await context.OrderHeaders.ExecuteDeleteAsync();
await context.Customers.ExecuteDeleteAsync();

var input = await File.ReadAllLinesAsync("C:/Users/damja/OneDrive/Dokumente/Schule HTL/4.Klasse/PROG/efcore-data-importer-DamjanTomas/data.txt");

static List<Customer> ParseInput(string[] input)
{
    List<Customer> customers = new();
    Customer? currentCustomer = null;
    OrderHeader? currentOrderHeader = null;

    foreach (var line in input)
    {
        string[] parts = line.Split('|');
        if (parts.Length < 2) continue; // Sicherstellen, dass genug Daten vorhanden sind

        switch (parts[0])
        {
            case "CUS":
                currentCustomer = new Customer(0, parts[1], parts[2], parts[3])
                {
                    Orders = new List<OrderHeader>()
                };
                customers.Add(currentCustomer);
                break;
            case "OH":
                if (currentCustomer == null)
                {
                    Console.Error.WriteLine("Fehler: OrderHeader ohne zugehörigen Kunden gefunden.");
                    continue;
                }
                currentOrderHeader = new OrderHeader(0, 0, currentCustomer, DateOnly.Parse(parts[1]), parts[2], parts[3], parts[4])
                {
                    OrderLines = new List<OrderLine>()
                };
                currentCustomer.Orders.Add(currentOrderHeader);
                break;
            case "OL":
                if (currentOrderHeader == null)
                {
                    Console.Error.WriteLine("Fehler: OrderLine ohne zugehörigen OrderHeader gefunden.");
                    continue;
                }
                var orderLine = new OrderLine(0, 0, currentOrderHeader, parts[1], int.Parse(parts[2]), decimal.Parse(parts[3], CultureInfo.InvariantCulture));
                currentOrderHeader.OrderLines.Add(orderLine);
                break;
        }
    }
    return customers;
}

var customers = ParseInput(input);

await using var transaction = await context.Database.BeginTransactionAsync();
try
{
    foreach (var customer in customers)
    {
        if (customer.CountryIsoCode.Length != 2)
        {
            Console.Error.WriteLine($"Skipping customer '{customer.CompanyName}' due to invalid CountryIsoCode '{customer.CountryIsoCode}'");
            continue;
        }
        context.Customers.Add(customer);
    }

    await context.SaveChangesAsync();
    await transaction.CommitAsync();
    Console.WriteLine("Transaction committed successfully for all customers.");
}
catch (DbUpdateException ex)
{
    await transaction.RollbackAsync();
    Console.Error.WriteLine($"Database error: {ex.InnerException?.Message ?? ex.Message}. Rolling back transaction.");
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    Console.Error.WriteLine($"Unexpected error: {ex.Message}. Rolling back transaction.");
}



/*var customer = new Customer(0, "Hogward", "UK", "Scotland");
Console.WriteLine($"Customer ID: {customer.ID}");
Console.WriteLine($"Customer Hashcode: {RuntimeHelpers.GetHashCode(customer)}");
Console.WriteLine($"Customer tracking state: {context.Entry(customer).State}");
context.Customers.Add(customer);
Console.WriteLine($"Customer ID (after Add): {customer.ID}");
Console.WriteLine($"Customer Hashcode: {RuntimeHelpers.GetHashCode(customer)}");
Console.WriteLine($"Customer tracking state: {context.Entry(customer).State}");
await context.SaveChangesAsync();
Console.WriteLine($"Customer ID (after SaveChanges): {customer.ID}");
Console.WriteLine($"Customer Hashcode: {RuntimeHelpers.GetHashCode(customer)}");
Console.WriteLine($"Customer tracking state: {context.Entry(customer).State}");

var reReadCustomer = await context.Customers.FindAsync(customer.ID);
Console.WriteLine($"Customer ID (after re-read): {customer.ID}");
Console.WriteLine($"Customer Hashcode: {RuntimeHelpers.GetHashCode(customer)}");

customer.CompanyName = "Foo";
Console.WriteLine($"Customer Companyname: {reReadCustomer!.CompanyName}");
Console.WriteLine($"Customer tracking state: {context.Entry(customer).State}");

using (var connection = context.Database.GetDbConnection())
{
    await connection.OpenAsync();
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT * FROM Customers";
    using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        Console.WriteLine(reader["CompanyName"]);
    }
}

context.Customers.Remove(customer);
await context.SaveChangesAsync();*/