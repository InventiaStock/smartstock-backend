using SmartStock.Platform.API.InventoryMonitoring.Procurement;
using SmartStock.Platform.API.InventoryMonitoring.Sales;
using SmartStock.Platform.API.ProductCatalog;
using SmartStock.Platform.API.Shared.Domain;
using SmartStock.Platform.API.Shared.Infrastructure;
using Xunit;

namespace SmartStock.Platform.API.Tests;

// Unit tests of the business rules. Names follow: Rule_expected_result_when_condition.
// (Pruebas unitarias de las reglas de negocio. No necesitan base de datos.)

public class SensorRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void R11_sensor_is_online_when_the_last_reading_is_4_minutes_old() =>
        Assert.Equal(SensorStatuses.Online, InventoryRules.SensorStatus(1, Now.AddMinutes(-4), Now));

    [Fact]
    public void R11_sensor_is_online_at_exactly_5_minutes() =>
        Assert.Equal(SensorStatuses.Online, InventoryRules.SensorStatus(1, Now.AddMinutes(-5), Now));

    [Fact]
    public void R11_sensor_is_disconnected_when_the_last_reading_is_6_minutes_old() =>
        Assert.Equal(SensorStatuses.Disconnected, InventoryRules.SensorStatus(1, Now.AddMinutes(-6), Now));

    [Fact]
    public void R11_sensor_without_readings_is_disconnected() =>
        Assert.Equal(SensorStatuses.Disconnected, InventoryRules.SensorStatus(1, null, Now));

    [Fact]
    public void R9_sensor_without_product_is_available() =>
        Assert.Equal(SensorStatuses.Available, InventoryRules.SensorStatus(null, Now, Now));

    [Theory]
    [InlineData(12.36, 1.03, 12)]   // Milk: 12.36 kg / 1.03 kg = 12 units
    [InlineData(3.68, 0.92, 4)]     // Cooking oil
    [InlineData(2.0, 1.0, 2)]       // Rice
    [InlineData(5.0, 0, 0)]         // without unit weight there are no units
    public void R12_weight_is_converted_to_whole_units(double weightKg, double unitWeightKg, int expected) =>
        Assert.Equal(expected, InventoryRules.ToUnits((decimal)weightKg, (decimal)unitWeightKg));

    [Fact]
    public void R20_difference_of_14_percent_is_a_discrepancy()
    {
        var pct = InventoryRules.RawDifferencePct(physicalUnits: 12, registeredUnits: 14);
        Assert.True(Math.Abs(pct) > InventoryRules.DiscrepancyThresholdPct);
        Assert.Equal(-14.3, pct, precision: 1);
    }

    [Fact]
    public void R20_difference_of_exactly_10_percent_is_not_a_discrepancy()
    {
        var pct = InventoryRules.RawDifferencePct(physicalUnits: 9, registeredUnits: 10);
        Assert.False(Math.Abs(pct) > InventoryRules.DiscrepancyThresholdPct);
    }

    [Fact]
    public void Difference_is_zero_when_nothing_is_registered() =>
        Assert.Equal(0.0, InventoryRules.RawDifferencePct(5, 0));

    [Theory]
    [InlineData(5, 5, StockLevels.LowStock)]
    [InlineData(2, 5, StockLevels.LowStock)]
    [InlineData(6, 5, StockLevels.Healthy)]
    public void Stock_level_is_low_when_it_is_at_or_under_the_threshold(int stock, int threshold, string expected) =>
        Assert.Equal(expected, InventoryRules.StockLevel(stock, threshold));
}

public class ProductRulesTests
{
    private static Product NewProduct() =>
        new("Rice 1 kg", "RICE-1KG", "Grocery", 1.0m, 8.2m, 6.5m, "Fresh Foods Co.", 5, 50);

    [Fact]
    public void R17_stock_starts_at_zero() => Assert.Equal(0, NewProduct().RegisteredStock);

    [Fact]
    public void R17_stock_goes_up_and_down_with_changes()
    {
        var product = NewProduct();
        Assert.Equal(10, product.ChangeRegisteredStock(10));
        Assert.Equal(7, product.ChangeRegisteredStock(-3));
    }

    [Fact]
    public void R13_stock_never_goes_below_zero()
    {
        var product = NewProduct();
        product.ChangeRegisteredStock(2);

        var error = Assert.Throws<DomainException>(() => product.ChangeRegisteredStock(-3));

        Assert.Equal("INSUFFICIENT_STOCK", error.Code);
        Assert.Equal(422, error.StatusCode);
        Assert.Equal(2, product.RegisteredStock);   // the stock did not change
    }

    [Fact]
    public void Editing_the_details_does_not_change_the_stock()
    {
        var product = NewProduct();
        product.ChangeRegisteredStock(8);

        product.UpdateDetails("Rice 5 kg", "RICE-5KG", "Grocery", 5.0m, 30m, 25m, "Fresh Foods Co.", 3, 40);

        Assert.Equal(8, product.RegisteredStock);
        Assert.Equal("Rice 5 kg", product.Name);
    }
}

public class SalesAndPurchasesRulesTests
{
    [Fact]
    public void Sale_total_is_the_sum_of_quantity_times_price()
    {
        var sale = new Sale(41, new DateOnly(2026, 11, 2),
        [
            new SaleItem(3, 3, 5.5m),
            new SaleItem(1, 2, 8.2m),
            new SaleItem(4, 4, 4.8m),
        ]);

        Assert.Equal(52.10m, sale.Total);
        Assert.Equal("R-00041", sale.Code);
    }

    [Fact]
    public void R15_purchase_starts_pending()
    {
        var purchase = new Purchase(31, 1, new DateOnly(2026, 11, 2), null, [new PurchaseItem(1, 10, 6.5m), new PurchaseItem(2, 8, 8.5m)]);

        Assert.Equal(PurchaseStatuses.Pending, purchase.Status);
        Assert.Equal(133.00m, purchase.Total);
        Assert.Equal("PO-00031", purchase.Code);
    }

    [Fact]
    public void R16_a_purchase_can_be_received_only_once()
    {
        var purchase = new Purchase(31, 1, new DateOnly(2026, 11, 2), null, [new PurchaseItem(1, 10, 6.5m)]);

        purchase.MarkReceived();
        var error = Assert.Throws<DomainException>(() => purchase.MarkReceived());

        Assert.Equal(PurchaseStatuses.Received, purchase.Status);
        Assert.Equal("ALREADY_RECEIVED", error.Code);
        Assert.Equal(409, error.StatusCode);
    }

    [Fact]
    public void R8_supplier_with_same_name_and_phone_is_a_duplicate_ignoring_case_and_spaces()
    {
        var supplier = new Supplier("Distribuidora Sol", "987 654 321", null);

        Assert.True(supplier.IsSameAs("distribuidora sol", "987654321"));
        Assert.False(supplier.IsSameAs("Distribuidora Sol", "999 000 111"));
    }
}

public class SharedKernelTests
{
    [Theory]
    [InlineData("R", 42, "R-00042")]
    [InlineData("PO", 31, "PO-00031")]
    public void Codes_are_formatted_with_five_digits(string prefix, int number, string expected) =>
        Assert.Equal(expected, CodeFormat.Format(prefix, number));

    [Fact]
    public void Codes_are_parsed_back_to_their_number()
    {
        Assert.True(CodeFormat.TryParse("R", "R-00042", out var number));
        Assert.Equal(42, number);
        Assert.False(CodeFormat.TryParse("R", "PO-00042", out _));
        Assert.False(CodeFormat.TryParse("R", "basura", out _));
    }

    [Theory]
    [InlineData("Name", "nombre")]
    [InlineData("UnitWeight", "pesoUnitario")]
    [InlineData("OriginCode", "origenId")]
    [InlineData("Token", "token")]   // not in the table: only camelCase
    public void Json_names_follow_the_contract_of_the_frontend(string property, string expected) =>
        Assert.Equal(expected, new WireNamingPolicy().ConvertName(property));

    [Fact]
    public void Error_messages_are_returned_in_english_by_default_and_in_spanish_on_request()
    {
        Assert.Equal("There is not enough stock.", ErrorMessages.Get("INSUFFICIENT_STOCK", null));
        Assert.Equal("No hay stock suficiente.", ErrorMessages.Get("INSUFFICIENT_STOCK", "es-419,es;q=0.9"));
        Assert.Equal("UNKNOWN_CODE", ErrorMessages.Get("UNKNOWN_CODE", "en-US"));
    }
}
