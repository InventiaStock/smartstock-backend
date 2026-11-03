namespace SmartStock.Platform.API.Shared.Infrastructure;

// TS13 scenario 2 (referential integrity): "the database rejects an item that references a product that
// does not exist". Each bounded context keeps its own tables, but the database is shared, so these small
// configurations add the foreign keys that cross contexts. Owner: Lorena (integration week).
// (Integridad referencial entre contextos: llaves foráneas hacia products.)

public class SaleItemProductRelation : IEntityTypeConfiguration<InventoryMonitoring.Sales.SaleItem>
{
    public void Configure(EntityTypeBuilder<InventoryMonitoring.Sales.SaleItem> builder) =>
        builder.HasOne<ProductCatalog.Product>().WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
}

public class PurchaseItemProductRelation : IEntityTypeConfiguration<InventoryMonitoring.Procurement.PurchaseItem>
{
    public void Configure(EntityTypeBuilder<InventoryMonitoring.Procurement.PurchaseItem> builder) =>
        builder.HasOne<ProductCatalog.Product>().WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
}

public class StockMovementProductRelation : IEntityTypeConfiguration<InventoryMonitoring.Stock.StockMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMonitoring.Stock.StockMovement> builder) =>
        builder.HasOne<ProductCatalog.Product>().WithMany().HasForeignKey(movement => movement.ProductId).OnDelete(DeleteBehavior.Restrict);
}

public class SensorProductRelation : IEntityTypeConfiguration<IoTDevice.Sensor>
{
    public void Configure(EntityTypeBuilder<IoTDevice.Sensor> builder) =>
        builder.HasOne<ProductCatalog.Product>().WithMany().HasForeignKey(sensor => sensor.ProductId).OnDelete(DeleteBehavior.SetNull);
}

public class AlertProductRelation : IEntityTypeConfiguration<AlertsRestocking.Alert>
{
    public void Configure(EntityTypeBuilder<AlertsRestocking.Alert> builder) =>
        builder.HasOne<ProductCatalog.Product>().WithMany().HasForeignKey(alert => alert.ProductId).OnDelete(DeleteBehavior.Restrict);
}
