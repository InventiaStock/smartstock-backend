CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `businesses` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `name` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `business_type` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `created_at` datetime(6) NOT NULL,
        CONSTRAINT `PK_businesses` PRIMARY KEY (`id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `notification_channels` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `business_id` bigint NOT NULL,
        `channel` varchar(15) CHARACTER SET utf8mb4 NOT NULL,
        `destination` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
        `active` tinyint(1) NOT NULL,
        CONSTRAINT `PK_notification_channels` PRIMARY KEY (`id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `products` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `business_id` bigint NOT NULL,
        `name` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `sku` varchar(60) CHARACTER SET utf8mb4 NOT NULL,
        `category` varchar(60) CHARACTER SET utf8mb4 NOT NULL,
        `unit_weight_kg` decimal(10,3) NOT NULL,
        `sale_price` decimal(10,2) NULL,
        `purchase_cost` decimal(10,2) NULL,
        `usual_supplier` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `registered_stock` int NOT NULL,
        `min_threshold` int NOT NULL,
        `max_capacity` int NOT NULL,
        `created_at` datetime(6) NOT NULL,
        CONSTRAINT `PK_products` PRIMARY KEY (`id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `sales` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `business_id` bigint NOT NULL,
        `number` int NOT NULL,
        `date` date NOT NULL,
        `status` varchar(15) CHARACTER SET utf8mb4 NOT NULL,
        `total` decimal(12,2) NOT NULL,
        `created_at` datetime(6) NOT NULL,
        CONSTRAINT `PK_sales` PRIMARY KEY (`id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `suppliers` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `business_id` bigint NOT NULL,
        `name` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `phone` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `email` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
        `created_at` datetime(6) NOT NULL,
        CONSTRAINT `PK_suppliers` PRIMARY KEY (`id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `users` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `business_id` bigint NOT NULL,
        `email` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
        `password_hash` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `created_at` datetime(6) NOT NULL,
        CONSTRAINT `PK_users` PRIMARY KEY (`id`),
        CONSTRAINT `FK_users_businesses_business_id` FOREIGN KEY (`business_id`) REFERENCES `businesses` (`id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `alerts` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `business_id` bigint NOT NULL,
        `type` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `product_id` bigint NOT NULL,
        `reference_stock` int NOT NULL,
        `source` varchar(15) CHARACTER SET utf8mb4 NOT NULL,
        `status` varchar(15) CHARACTER SET utf8mb4 NOT NULL,
        `created_at` datetime(6) NOT NULL,
        `resolved_at` datetime(6) NULL,
        `registered_stock` int NULL,
        `physical_stock` int NULL,
        `difference_pct` double NULL,
        CONSTRAINT `PK_alerts` PRIMARY KEY (`id`),
        CONSTRAINT `FK_alerts_products_product_id` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `sensors` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `business_id` bigint NOT NULL,
        `code` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `product_id` bigint NULL,
        `weight_kg` decimal(10,3) NOT NULL,
        `reference_weight_kg` decimal(10,3) NULL,
        `last_reading_at` datetime(6) NULL,
        CONSTRAINT `PK_sensors` PRIMARY KEY (`id`),
        CONSTRAINT `FK_sensors_products_product_id` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `stock_movements` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `business_id` bigint NOT NULL,
        `product_id` bigint NOT NULL,
        `type` varchar(10) CHARACTER SET utf8mb4 NOT NULL,
        `origin` varchar(15) CHARACTER SET utf8mb4 NOT NULL,
        `origin_code` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `quantity` int NOT NULL,
        `date` date NOT NULL,
        `created_at` datetime(6) NOT NULL,
        CONSTRAINT `PK_stock_movements` PRIMARY KEY (`id`),
        CONSTRAINT `FK_stock_movements_products_product_id` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `sale_items` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `sale_id` bigint NOT NULL,
        `product_id` bigint NOT NULL,
        `quantity` int NOT NULL,
        `unit_price` decimal(10,2) NOT NULL,
        CONSTRAINT `PK_sale_items` PRIMARY KEY (`id`),
        CONSTRAINT `FK_sale_items_products_product_id` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_sale_items_sales_sale_id` FOREIGN KEY (`sale_id`) REFERENCES `sales` (`id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `purchases` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `business_id` bigint NOT NULL,
        `number` int NOT NULL,
        `supplier_id` bigint NOT NULL,
        `date` date NOT NULL,
        `status` varchar(15) CHARACTER SET utf8mb4 NOT NULL,
        `total` decimal(12,2) NOT NULL,
        `restocking_need_id` bigint NULL,
        `created_at` datetime(6) NOT NULL,
        CONSTRAINT `PK_purchases` PRIMARY KEY (`id`),
        CONSTRAINT `FK_purchases_suppliers_supplier_id` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `password_resets` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `user_id` bigint NOT NULL,
        `token_hash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
        `issued_at` datetime(6) NOT NULL,
        `expires_at` datetime(6) NOT NULL,
        `used_at` datetime(6) NULL,
        CONSTRAINT `PK_password_resets` PRIMARY KEY (`id`),
        CONSTRAINT `FK_password_resets_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `restocking_needs` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `business_id` bigint NOT NULL,
        `product_id` bigint NOT NULL,
        `alert_id` bigint NOT NULL,
        `status` varchar(15) CHARACTER SET utf8mb4 NOT NULL,
        `purchase_code` varchar(20) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_restocking_needs` PRIMARY KEY (`id`),
        CONSTRAINT `FK_restocking_needs_alerts_alert_id` FOREIGN KEY (`alert_id`) REFERENCES `alerts` (`id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `sent_notifications` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `business_id` bigint NOT NULL,
        `alert_id` bigint NOT NULL,
        `channel` varchar(15) CHARACTER SET utf8mb4 NOT NULL,
        `destination` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
        `sent_at` datetime(6) NOT NULL,
        `error` varchar(500) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_sent_notifications` PRIMARY KEY (`id`),
        CONSTRAINT `FK_sent_notifications_alerts_alert_id` FOREIGN KEY (`alert_id`) REFERENCES `alerts` (`id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `sensor_readings` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `sensor_id` bigint NOT NULL,
        `weight_kg` decimal(10,3) NOT NULL,
        `read_at` datetime(6) NOT NULL,
        CONSTRAINT `PK_sensor_readings` PRIMARY KEY (`id`),
        CONSTRAINT `FK_sensor_readings_sensors_sensor_id` FOREIGN KEY (`sensor_id`) REFERENCES `sensors` (`id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE TABLE `purchase_items` (
        `id` bigint NOT NULL AUTO_INCREMENT,
        `purchase_id` bigint NOT NULL,
        `product_id` bigint NOT NULL,
        `quantity` int NOT NULL,
        `unit_cost` decimal(10,2) NOT NULL,
        CONSTRAINT `PK_purchase_items` PRIMARY KEY (`id`),
        CONSTRAINT `FK_purchase_items_products_product_id` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_purchase_items_purchases_purchase_id` FOREIGN KEY (`purchase_id`) REFERENCES `purchases` (`id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_alerts_business_id_product_id_type_status` ON `alerts` (`business_id`, `product_id`, `type`, `status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_alerts_product_id` ON `alerts` (`product_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_password_resets_user_id` ON `password_resets` (`user_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_products_business_id_name` ON `products` (`business_id`, `name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_purchase_items_product_id` ON `purchase_items` (`product_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_purchase_items_purchase_id` ON `purchase_items` (`purchase_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_purchases_business_id_date` ON `purchases` (`business_id`, `date`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_purchases_business_id_number` ON `purchases` (`business_id`, `number`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_purchases_supplier_id` ON `purchases` (`supplier_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_restocking_needs_alert_id` ON `restocking_needs` (`alert_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_sale_items_product_id` ON `sale_items` (`product_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_sale_items_sale_id` ON `sale_items` (`sale_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_sales_business_id_date` ON `sales` (`business_id`, `date`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_sales_business_id_number` ON `sales` (`business_id`, `number`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_sensor_readings_sensor_id_read_at` ON `sensor_readings` (`sensor_id`, `read_at`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_sensors_business_id_code` ON `sensors` (`business_id`, `code`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_sensors_product_id` ON `sensors` (`product_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_sent_notifications_alert_id` ON `sent_notifications` (`alert_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_stock_movements_business_id_date` ON `stock_movements` (`business_id`, `date`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_stock_movements_origin_origin_code` ON `stock_movements` (`origin`, `origin_code`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_stock_movements_product_id` ON `stock_movements` (`product_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_suppliers_business_id_name_phone` ON `suppliers` (`business_id`, `name`, `phone`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE INDEX `IX_users_business_id` ON `users` (`business_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_users_email` ON `users` (`email`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261104000837_InitialCreate') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261104000837_InitialCreate', '9.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

