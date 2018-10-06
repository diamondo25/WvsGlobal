-- This is the base SQL file, you can put comments in the top.
-- Try to prevent too destructive queries and keep your queries
-- as generic as possible (eg do not refer to a database).
-- Make sure your Evolution works both back and forth (rename
-- the file to a non-sql file so it will do the DOWN logic).
------- UP -------

DROP TABLE IF EXISTS `cashitem_bundle`;
DROP TABLE IF EXISTS `cashitem_eqp`;
DROP TABLE IF EXISTS `cashitem_pet`;
DROP TABLE IF EXISTS itemlocker;
DROP TABLE IF EXISTS inventory_eqp;
DROP TABLE IF EXISTS inventory_bundle;
DROP TABLE IF EXISTS storage_eqp;
DROP TABLE IF EXISTS storage_bundle;


CREATE TABLE `cashitem_bundle` (
  `userid` int(11) NOT NULL,
  `itemid` int(11) NOT NULL,
  `amount` smallint(6) NOT NULL DEFAULT '1',
  `cashid` bigint(20) NOT NULL,
  `expiration` bigint(20) NOT NULL DEFAULT '150842304000000000',
  PRIMARY KEY (`cashid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;

CREATE TABLE `cashitem_eqp` (
  `userid` int(11) NOT NULL,
  `itemid` int(11) NOT NULL,
  `slots` tinyint(4) NOT NULL DEFAULT 7,
  `scrolls` tinyint(4) NOT NULL DEFAULT 0,
  `istr` smallint(6) NOT NULL DEFAULT 0,
  `idex` smallint(6) NOT NULL DEFAULT 0,
  `iint` smallint(6) NOT NULL DEFAULT 0,
  `iluk` smallint(6) NOT NULL DEFAULT 0,
  `ihp` smallint(6) NOT NULL DEFAULT 0,
  `imp` smallint(6) NOT NULL DEFAULT 0,
  `iwatk` smallint(6) NOT NULL DEFAULT 0,
  `imatk` smallint(6) NOT NULL DEFAULT 0,
  `iwdef` smallint(6) NOT NULL DEFAULT 0,
  `imdef` smallint(6) NOT NULL DEFAULT 0,
  `iacc` smallint(6) NOT NULL DEFAULT 0,
  `iavo` smallint(6) NOT NULL DEFAULT 0,
  `ihand` smallint(6) NOT NULL DEFAULT 0,
  `ispeed` smallint(6) NOT NULL DEFAULT 0,
  `ijump` smallint(6) NOT NULL DEFAULT 0,
  `cashid` bigint(20) NOT NULL,
  `expiration` bigint(20) NOT NULL DEFAULT '150842304000000000',
  PRIMARY KEY (`cashid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;

CREATE TABLE `cashitem_pet` (
  `userid` int(11) NOT NULL,
  `charid` int(11) NOT NULL,
  `cashid` bigint(20) NOT NULL,
  `itemid` int(11) NOT NULL,
  `name` varchar(12) COLLATE latin1_general_ci NOT NULL,
  `level` tinyint(3) NOT NULL,
  `closeness` smallint(6) NOT NULL,
  `fullness` tinyint(3) NOT NULL,
  `expiration` bigint(20) NOT NULL,
  `deaddate` bigint(20) NOT NULL,
  PRIMARY KEY (`cashid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;

CREATE TABLE `itemlocker` (
  `cashid` bigint(20) NOT NULL,
  `slot` smallint(6) NOT NULL,
  `userid` int(11) NOT NULL,
  `characterid` int(11) NOT NULL,
  `itemid` int(11) NOT NULL,
  `commodity_id` int(11) NOT NULL,
  `amount` smallint(6) NOT NULL,
  `buycharactername` varchar(13) NOT NULL,
  `expiration` bigint(20) NOT NULL,
  `discount_rate` int(11) NOT NULL,
  `worldid` tinyint(4) NOT NULL,
  PRIMARY KEY (`cashid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;


CREATE TABLE `inventory_eqp` (
  `charid` int(11) NOT NULL,
  `inv` tinyint(4) NOT NULL, -- Will be dropped
  `slot` smallint(6) NOT NULL,
  `itemid` int(11) NOT NULL,
  `amount` smallint(11) NOT NULL DEFAULT 1, -- Will be dropped
  `slots` tinyint(4) NOT NULL DEFAULT 7,
  `scrolls` tinyint(4) NOT NULL DEFAULT 0,
  `istr` smallint(6) NOT NULL DEFAULT 0,
  `idex` smallint(6) NOT NULL DEFAULT 0,
  `iint` smallint(6) NOT NULL DEFAULT 0,
  `iluk` smallint(6) NOT NULL DEFAULT 0,
  `ihp` smallint(6) NOT NULL DEFAULT 0,
  `imp` smallint(6) NOT NULL DEFAULT 0,
  `iwatk` smallint(6) NOT NULL DEFAULT 0,
  `imatk` smallint(6) NOT NULL DEFAULT 0,
  `iwdef` smallint(6) NOT NULL DEFAULT 0,
  `imdef` smallint(6) NOT NULL DEFAULT 0,
  `iacc` smallint(6) NOT NULL DEFAULT 0,
  `iavo` smallint(6) NOT NULL DEFAULT 0,
  `ihand` smallint(6) NOT NULL DEFAULT 0,
  `ispeed` smallint(6) NOT NULL DEFAULT 0,
  `ijump` smallint(6) NOT NULL DEFAULT 0,
  `name` varchar(12) COLLATE latin1_general_ci NOT NULL,
  `cashid` bigint(20) NOT NULL DEFAULT 0,
  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000,
  PRIMARY KEY (`charid`,`slot`),
  KEY `charid` (`charid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;

CREATE TABLE `inventory_bundle` (
  `charid` int(11) NOT NULL,
  `inv` tinyint(4) NOT NULL,
  `slot` smallint(6) NOT NULL,
  `itemid` int(11) NOT NULL,
  `amount` int(11) NOT NULL DEFAULT 1,
  `cashid` bigint(20) NOT NULL DEFAULT 0,
  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000,
  PRIMARY KEY (`charid`,`inv`,`slot`),
  KEY `charid` (`charid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;

INSERT INTO inventory_eqp SELECT * FROM items i WHERE i.inv = 1;

INSERT INTO inventory_bundle SELECT i.charid, i.inv, i.slot, i.itemid, i.amount, i.cashid, i.expiration FROM items i WHERE i.inv <> 1;


-- Storage

CREATE TABLE `storage_eqp` (
  `userid` int(11) NOT NULL,
  `world_id` int(11) NOT NULL,
  `slot` smallint(6) NOT NULL,
  `itemid` int(11) NOT NULL,
  `amount` smallint(11) NOT NULL DEFAULT 1, -- Will be dropped
  `slots` tinyint(4) NOT NULL DEFAULT 7,
  `scrolls` tinyint(4) NOT NULL DEFAULT 0,
  `istr` smallint(6) NOT NULL DEFAULT 0,
  `idex` smallint(6) NOT NULL DEFAULT 0,
  `iint` smallint(6) NOT NULL DEFAULT 0,
  `iluk` smallint(6) NOT NULL DEFAULT 0,
  `ihp` smallint(6) NOT NULL DEFAULT 0,
  `imp` smallint(6) NOT NULL DEFAULT 0,
  `iwatk` smallint(6) NOT NULL DEFAULT 0,
  `imatk` smallint(6) NOT NULL DEFAULT 0,
  `iwdef` smallint(6) NOT NULL DEFAULT 0,
  `imdef` smallint(6) NOT NULL DEFAULT 0,
  `iacc` smallint(6) NOT NULL DEFAULT 0,
  `iavo` smallint(6) NOT NULL DEFAULT 0,
  `ihand` smallint(6) NOT NULL DEFAULT 0,
  `ispeed` smallint(6) NOT NULL DEFAULT 0,
  `ijump` smallint(6) NOT NULL DEFAULT 0,
  `name` varchar(12) COLLATE latin1_general_ci NOT NULL,
  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000,
  KEY `userid_worldid` (`userid`, `world_id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;

CREATE TABLE `storage_bundle` (
  `userid` int(11) NOT NULL,
  `world_id` int(11) NOT NULL,
  `inv` tinyint(4) NOT NULL,
  `slot` smallint(6) NOT NULL,
  `itemid` int(11) NOT NULL,
  `amount` smallint(11) NOT NULL DEFAULT 1,
  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000,
  KEY `userid_worldid` (`userid`, `world_id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;

INSERT INTO storage_eqp SELECT * FROM storage_items i WHERE FLOOR(i.itemid / 1000000) = 1;

INSERT INTO storage_bundle SELECT i.userid, i.world_id, FLOOR(i.itemid / 1000000), i.slot, i.itemid, i.amount, i.expiration FROM storage_items i WHERE FLOOR(i.itemid / 1000000) <> 1;

ALTER TABLE `storage_eqp` ADD COLUMN `cashid` BIGINT(20) NOT NULL AFTER `ijump`;
ALTER TABLE `storage_bundle` ADD COLUMN `cashid` BIGINT(20) NOT NULL AFTER `amount`;

ALTER TABLE inventory_eqp DROP COLUMN amount, DROP COLUMN inv, DROP COLUMN `name`;
ALTER TABLE storage_eqp DROP COLUMN amount, DROP COLUMN `name`;


ALTER TABLE characters ADD COLUMN pet_cash_id BIGINT(20) NOT NULL DEFAULT 0;

------- DOWN -------

-- Put your down queries here, such as DROP TABLE, etc.
