CREATE DATABASE  IF NOT EXISTS `wvsbeta` /*!40100 DEFAULT CHARACTER SET latin1 */;
USE `wvsbeta`;
-- MySQL dump 10.13  Distrib 5.7.9, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: wvsbeta
-- ------------------------------------------------------
-- Server version	5.5.5-10.2.9-MariaDB

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `beta_keys`
--

DROP TABLE IF EXISTS `beta_keys`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `beta_keys` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `used` int(20) DEFAULT 0,
  `betakey` varchar(255) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  `updated_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `id_UNIQUE` (`id`),
  UNIQUE KEY `value_UNIQUE` (`betakey`),
  KEY `username_idx` (`used`),
  KEY `redeemed_by_username_idx` (`used`),
  KEY `user_idx` (`used`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `beta_keys`
--

LOCK TABLES `beta_keys` WRITE;
/*!40000 ALTER TABLE `beta_keys` DISABLE KEYS */;
/*!40000 ALTER TABLE `beta_keys` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `buddylist`
--

DROP TABLE IF EXISTS `buddylist`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `buddylist` (
  `charid` int(11) NOT NULL,
  `buddy_charid` int(11) NOT NULL,
  `buddy_charname` varchar(12) NOT NULL,
  UNIQUE KEY `charid` (`charid`,`buddy_charid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `buddylist`
--

LOCK TABLES `buddylist` WRITE;
/*!40000 ALTER TABLE `buddylist` DISABLE KEYS */;
/*!40000 ALTER TABLE `buddylist` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `buddylist_pending`
--

DROP TABLE IF EXISTS `buddylist_pending`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `buddylist_pending` (
  `charid` int(11) NOT NULL,
  `inviter_charid` int(11) NOT NULL,
  `inviter_charname` varchar(13) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `buddylist_pending`
--

LOCK TABLES `buddylist_pending` WRITE;
/*!40000 ALTER TABLE `buddylist_pending` DISABLE KEYS */;
/*!40000 ALTER TABLE `buddylist_pending` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cashitem_bundle`
--

DROP TABLE IF EXISTS `cashitem_bundle`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `cashitem_bundle` (
  `userid` int(11) NOT NULL,
  `itemid` int(11) NOT NULL,
  `amount` smallint(6) NOT NULL DEFAULT 1,
  `cashid` bigint(20) NOT NULL,
  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000,
  PRIMARY KEY (`cashid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cashitem_bundle`
--

LOCK TABLES `cashitem_bundle` WRITE;
/*!40000 ALTER TABLE `cashitem_bundle` DISABLE KEYS */;
/*!40000 ALTER TABLE `cashitem_bundle` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cashitem_eqp`
--

DROP TABLE IF EXISTS `cashitem_eqp`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
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
  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000,
  PRIMARY KEY (`cashid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cashitem_eqp`
--

LOCK TABLES `cashitem_eqp` WRITE;
/*!40000 ALTER TABLE `cashitem_eqp` DISABLE KEYS */;
/*!40000 ALTER TABLE `cashitem_eqp` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cashitem_pet`
--

DROP TABLE IF EXISTS `cashitem_pet`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `cashitem_pet` (
  `userid` int(11) NOT NULL,
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
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cashitem_pet`
--

LOCK TABLES `cashitem_pet` WRITE;
/*!40000 ALTER TABLE `cashitem_pet` DISABLE KEYS */;
/*!40000 ALTER TABLE `cashitem_pet` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cashshop_coupon_codes`
--

DROP TABLE IF EXISTS `cashshop_coupon_codes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `cashshop_coupon_codes` (
  `serial` varchar(22) NOT NULL,
  `maplepoints` int(11) NOT NULL DEFAULT 0,
  `nxcredit` int(11) NOT NULL DEFAULT 0,
  `nxprepaid` int(11) NOT NULL DEFAULT 0,
  `mesos` int(11) NOT NULL DEFAULT 0,
  `used` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`serial`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cashshop_coupon_codes`
--

LOCK TABLES `cashshop_coupon_codes` WRITE;
/*!40000 ALTER TABLE `cashshop_coupon_codes` DISABLE KEYS */;
/*!40000 ALTER TABLE `cashshop_coupon_codes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cashshop_coupon_item_rewards`
--

DROP TABLE IF EXISTS `cashshop_coupon_item_rewards`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `cashshop_coupon_item_rewards` (
  `serial` varchar(22) NOT NULL,
  `itemid` int(11) NOT NULL,
  `amount` int(11) NOT NULL DEFAULT 1,
  `days_usable` int(5) NOT NULL DEFAULT 0
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cashshop_coupon_item_rewards`
--

LOCK TABLES `cashshop_coupon_item_rewards` WRITE;
/*!40000 ALTER TABLE `cashshop_coupon_item_rewards` DISABLE KEYS */;
/*!40000 ALTER TABLE `cashshop_coupon_item_rewards` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cashshop_limit_sell`
--

DROP TABLE IF EXISTS `cashshop_limit_sell`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `cashshop_limit_sell` (
  `serial` int(11) NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cashshop_limit_sell`
--

LOCK TABLES `cashshop_limit_sell` WRITE;
/*!40000 ALTER TABLE `cashshop_limit_sell` DISABLE KEYS */;
/*!40000 ALTER TABLE `cashshop_limit_sell` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cashshop_modified_items`
--

DROP TABLE IF EXISTS `cashshop_modified_items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `cashshop_modified_items` (
  `serial` int(11) NOT NULL,
  `discount_price` int(11) DEFAULT NULL,
  `mark` enum('sale','hot','event','new') DEFAULT NULL,
  `showup` tinyint(1) DEFAULT NULL
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cashshop_modified_items`
--

LOCK TABLES `cashshop_modified_items` WRITE;
/*!40000 ALTER TABLE `cashshop_modified_items` DISABLE KEYS */;
/*!40000 ALTER TABLE `cashshop_modified_items` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cashshop_sell_log`
--

DROP TABLE IF EXISTS `cashshop_sell_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `cashshop_sell_log` (
  `id` int(11) unsigned NOT NULL AUTO_INCREMENT,
  `userid` int(11) NOT NULL,
  `charid` int(11) NOT NULL,
  `packageid` int(11) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cashshop_sell_log`
--

LOCK TABLES `cashshop_sell_log` WRITE;
/*!40000 ALTER TABLE `cashshop_sell_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `cashshop_sell_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `character_cashshop_gifts`
--

DROP TABLE IF EXISTS `character_cashshop_gifts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `character_cashshop_gifts` (
  `charid` int(11) NOT NULL,
  `cashid` int(11) NOT NULL,
  `itemid` int(11) NOT NULL,
  `sender` varchar(13) NOT NULL,
  `message` varchar(73) NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `character_cashshop_gifts`
--

LOCK TABLES `character_cashshop_gifts` WRITE;
/*!40000 ALTER TABLE `character_cashshop_gifts` DISABLE KEYS */;
/*!40000 ALTER TABLE `character_cashshop_gifts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `character_quest_mobs`
--

DROP TABLE IF EXISTS `character_quest_mobs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `character_quest_mobs` (
  `id` int(11) NOT NULL,
  `mobid` int(11) NOT NULL,
  `killed` int(11) NOT NULL,
  `needed` int(11) NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `character_quest_mobs`
--

LOCK TABLES `character_quest_mobs` WRITE;
/*!40000 ALTER TABLE `character_quest_mobs` DISABLE KEYS */;
/*!40000 ALTER TABLE `character_quest_mobs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `character_quests`
--

DROP TABLE IF EXISTS `character_quests`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `character_quests` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `charid` int(11) NOT NULL,
  `questid` int(16) NOT NULL,
  `data` varchar(40) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `character_quests`
--

LOCK TABLES `character_quests` WRITE;
/*!40000 ALTER TABLE `character_quests` DISABLE KEYS */;
/*!40000 ALTER TABLE `character_quests` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `character_variables`
--

DROP TABLE IF EXISTS `character_variables`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `character_variables` (
  `charid` int(11) NOT NULL,
  `key` varchar(255) NOT NULL,
  `value` varchar(255) NOT NULL,
  UNIQUE KEY `charid_2` (`charid`,`key`),
  KEY `charid` (`charid`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `character_variables`
--

LOCK TABLES `character_variables` WRITE;
/*!40000 ALTER TABLE `character_variables` DISABLE KEYS */;
/*!40000 ALTER TABLE `character_variables` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `character_wishlist`
--

DROP TABLE IF EXISTS `character_wishlist`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `character_wishlist` (
  `charid` int(11) NOT NULL,
  `serial` int(11) NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `character_wishlist`
--

LOCK TABLES `character_wishlist` WRITE;
/*!40000 ALTER TABLE `character_wishlist` DISABLE KEYS */;
/*!40000 ALTER TABLE `character_wishlist` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `characters`
--

DROP TABLE IF EXISTS `characters`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `characters` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(12) COLLATE latin1_general_ci NOT NULL,
  `userid` int(11) NOT NULL,
  `world_id` tinyint(1) unsigned NOT NULL,
  `level` tinyint(3) unsigned NOT NULL DEFAULT 1,
  `job` smallint(6) NOT NULL DEFAULT 0,
  `str` smallint(6) NOT NULL DEFAULT 4,
  `dex` smallint(6) NOT NULL DEFAULT 4,
  `int` smallint(6) NOT NULL DEFAULT 4,
  `luk` smallint(6) NOT NULL DEFAULT 4,
  `chp` smallint(6) NOT NULL DEFAULT 50,
  `mhp` smallint(6) NOT NULL DEFAULT 50,
  `cmp` smallint(6) NOT NULL DEFAULT 50,
  `mmp` smallint(6) NOT NULL DEFAULT 50,
  `hpmp_ap` int(11) NOT NULL DEFAULT 0,
  `ap` smallint(6) NOT NULL DEFAULT 0,
  `sp` smallint(6) NOT NULL DEFAULT 0,
  `exp` int(11) NOT NULL DEFAULT 0,
  `fame` smallint(6) NOT NULL DEFAULT 0,
  `map` int(11) NOT NULL DEFAULT 0,
  `pos` smallint(6) NOT NULL DEFAULT 0,
  `gender` tinyint(1) NOT NULL,
  `skin` tinyint(4) NOT NULL,
  `eyes` int(11) NOT NULL,
  `hair` int(11) NOT NULL,
  `mesos` int(11) NOT NULL DEFAULT 0,
  `equip_slots` int(11) NOT NULL DEFAULT 24,
  `use_slots` int(11) NOT NULL DEFAULT 24,
  `setup_slots` int(11) NOT NULL DEFAULT 24,
  `etc_slots` int(11) NOT NULL DEFAULT 24,
  `cash_slots` int(11) NOT NULL DEFAULT 48,
  `buddylist_size` int(3) unsigned NOT NULL DEFAULT 20,
  `online` tinyint(1) NOT NULL DEFAULT 0,
  `time_level` datetime NOT NULL DEFAULT '2012-01-09 12:37:00',
  `overall_cpos` int(11) NOT NULL DEFAULT 0,
  `overall_opos` int(11) NOT NULL DEFAULT 0,
  `world_cpos` int(11) NOT NULL DEFAULT 0,
  `world_opos` int(11) NOT NULL DEFAULT 0,
  `job_cpos` int(11) NOT NULL DEFAULT 0,
  `job_opos` int(11) NOT NULL DEFAULT 0,
  `fame_cpos` int(11) NOT NULL DEFAULT 0,
  `fame_opos` int(11) NOT NULL DEFAULT 0,
  `event` datetime NOT NULL DEFAULT '2012-01-09 12:37:00',
  `eventmap` int(11) unsigned NOT NULL DEFAULT 0,
  `party` int(11) NOT NULL DEFAULT -1,
  `last_savepoint` datetime DEFAULT '2012-01-09 12:37:00',
  `hash` varchar(50) COLLATE latin1_general_ci DEFAULT NULL,
  `rankbanned` tinyint(1) NOT NULL DEFAULT 0 COMMENT '0 false 1 true',
  `pet_cash_id` bigint(20) NOT NULL DEFAULT 0,
  PRIMARY KEY (`ID`),
  KEY `userid` (`userid`),
  KEY `world_id` (`world_id`),
  KEY `name` (`name`),
  KEY `job` (`job`),
  KEY `level` (`level`),
  KEY `fame` (`fame`),
  KEY `online` (`online`),
  KEY `userid_worldid` (`userid`,`world_id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `characters`
--

LOCK TABLES `characters` WRITE;
/*!40000 ALTER TABLE `characters` DISABLE KEYS */;
/*!40000 ALTER TABLE `characters` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `completed_quests`
--

DROP TABLE IF EXISTS `completed_quests`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `completed_quests` (
  `charid` int(11) NOT NULL,
  `questid` smallint(6) NOT NULL,
  `endtime` bigint(20) DEFAULT 0,
  PRIMARY KEY (`charid`,`questid`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `completed_quests`
--

LOCK TABLES `completed_quests` WRITE;
/*!40000 ALTER TABLE `completed_quests` DISABLE KEYS */;
/*!40000 ALTER TABLE `completed_quests` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `connections`
--

DROP TABLE IF EXISTS `connections`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `connections` (
  `when` datetime NOT NULL,
  `howmany` varchar(255) NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `connections`
--

LOCK TABLES `connections` WRITE;
/*!40000 ALTER TABLE `connections` DISABLE KEYS */;
/*!40000 ALTER TABLE `connections` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cooldowns`
--

DROP TABLE IF EXISTS `cooldowns`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `cooldowns` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `charid` int(11) DEFAULT NULL,
  `skillid` int(11) DEFAULT 0,
  `timeleft` smallint(6) DEFAULT 0,
  PRIMARY KEY (`id`),
  KEY `charid` (`charid`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cooldowns`
--

LOCK TABLES `cooldowns` WRITE;
/*!40000 ALTER TABLE `cooldowns` DISABLE KEYS */;
/*!40000 ALTER TABLE `cooldowns` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `data_ids`
--

DROP TABLE IF EXISTS `data_ids`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `data_ids` (
  `objectid` int(10) NOT NULL,
  `objectname` varchar(150) NOT NULL,
  `objecttype` enum('item','map','mob','script','npc','skill') NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `data_ids`
--

LOCK TABLES `data_ids` WRITE;
/*!40000 ALTER TABLE `data_ids` DISABLE KEYS */;
/*!40000 ALTER TABLE `data_ids` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `fame_log`
--

DROP TABLE IF EXISTS `fame_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `fame_log` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `from` int(11) NOT NULL,
  `to` int(11) NOT NULL,
  `time` datetime NOT NULL,
  PRIMARY KEY (`id`),
  KEY `from` (`from`,`to`,`time`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `fame_log`
--

LOCK TABLES `fame_log` WRITE;
/*!40000 ALTER TABLE `fame_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `fame_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `gamestats`
--

DROP TABLE IF EXISTS `gamestats`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `gamestats` (
  `id` int(11) NOT NULL DEFAULT 0,
  `omokwins` int(11) NOT NULL DEFAULT 0,
  `omoklosses` int(11) NOT NULL DEFAULT 0,
  `omokties` int(11) NOT NULL DEFAULT 0,
  `matchcardwins` int(11) NOT NULL DEFAULT 0,
  `matchcardties` int(11) NOT NULL DEFAULT 0,
  `matchcardlosses` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `gamestats`
--

LOCK TABLES `gamestats` WRITE;
/*!40000 ALTER TABLE `gamestats` DISABLE KEYS */;
/*!40000 ALTER TABLE `gamestats` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `inc_table`
--

DROP TABLE IF EXISTS `inc_table`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `inc_table` (
  `last_cash_serial` bigint(20) unsigned NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `inc_table`
--

LOCK TABLES `inc_table` WRITE;
/*!40000 ALTER TABLE `inc_table` DISABLE KEYS */;
/*!40000 ALTER TABLE `inc_table` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `inventory_bundle`
--

DROP TABLE IF EXISTS `inventory_bundle`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
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
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `inventory_bundle`
--

LOCK TABLES `inventory_bundle` WRITE;
/*!40000 ALTER TABLE `inventory_bundle` DISABLE KEYS */;
/*!40000 ALTER TABLE `inventory_bundle` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `inventory_eqp`
--

DROP TABLE IF EXISTS `inventory_eqp`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `inventory_eqp` (
  `charid` int(11) NOT NULL,
  `slot` smallint(6) NOT NULL,
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
  `cashid` bigint(20) NOT NULL DEFAULT 0,
  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000,
  PRIMARY KEY (`charid`,`slot`),
  KEY `charid` (`charid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `inventory_eqp`
--

LOCK TABLES `inventory_eqp` WRITE;
/*!40000 ALTER TABLE `inventory_eqp` DISABLE KEYS */;
/*!40000 ALTER TABLE `inventory_eqp` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ipbans`
--

DROP TABLE IF EXISTS `ipbans`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ipbans` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `ip` varchar(15) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ipbans`
--

LOCK TABLES `ipbans` WRITE;
/*!40000 ALTER TABLE `ipbans` DISABLE KEYS */;
/*!40000 ALTER TABLE `ipbans` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `itemlocker`
--

DROP TABLE IF EXISTS `itemlocker`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
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
  `gift_unread` tinyint(4) NOT NULL DEFAULT 0,
  `worldid` tinyint(4) NOT NULL,
  PRIMARY KEY (`cashid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `itemlocker`
--

LOCK TABLES `itemlocker` WRITE;
/*!40000 ALTER TABLE `itemlocker` DISABLE KEYS */;
/*!40000 ALTER TABLE `itemlocker` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `items`
--

DROP TABLE IF EXISTS `items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `items` (
  `charid` int(11) NOT NULL,
  `inv` smallint(6) NOT NULL,
  `slot` smallint(6) NOT NULL,
  `itemid` int(11) NOT NULL,
  `amount` int(11) NOT NULL DEFAULT 1,
  `slots` tinyint(4) DEFAULT 7,
  `scrolls` smallint(6) DEFAULT 0,
  `istr` smallint(6) DEFAULT 0,
  `idex` smallint(6) DEFAULT 0,
  `iint` smallint(6) DEFAULT 0,
  `iluk` smallint(6) DEFAULT 0,
  `ihp` smallint(6) DEFAULT 0,
  `imp` smallint(6) DEFAULT 0,
  `iwatk` smallint(6) DEFAULT 0,
  `imatk` smallint(6) DEFAULT 0,
  `iwdef` smallint(6) DEFAULT 0,
  `imdef` smallint(6) DEFAULT 0,
  `iacc` smallint(6) DEFAULT 0,
  `iavo` smallint(6) DEFAULT 0,
  `ihand` smallint(6) DEFAULT 0,
  `ispeed` smallint(6) DEFAULT 0,
  `ijump` smallint(6) DEFAULT 0,
  `name` varchar(12) COLLATE latin1_general_ci NOT NULL,
  `cashid` int(11) NOT NULL DEFAULT 0,
  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000,
  PRIMARY KEY (`charid`,`inv`,`slot`),
  KEY `charid` (`charid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `items`
--

LOCK TABLES `items` WRITE;
/*!40000 ALTER TABLE `items` DISABLE KEYS */;
/*!40000 ALTER TABLE `items` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `jobs`
--

DROP TABLE IF EXISTS `jobs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `jobs` (
  `id` varchar(5) NOT NULL,
  `name` varchar(255) NOT NULL,
  UNIQUE KEY `id` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `jobs`
--

LOCK TABLES `jobs` WRITE;
/*!40000 ALTER TABLE `jobs` DISABLE KEYS */;
/*!40000 ALTER TABLE `jobs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `machine_ban`
--

DROP TABLE IF EXISTS `machine_ban`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `machine_ban` (
  `machineid` varchar(32) NOT NULL,
  `last_username` varchar(13) NOT NULL,
  `last_ip` varchar(15) NOT NULL,
  `last_try` datetime NOT NULL,
  `reason` text DEFAULT NULL,
  `last_unique_id` varchar(26) DEFAULT '',
  UNIQUE KEY `machineid` (`machineid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `machine_ban`
--

LOCK TABLES `machine_ban` WRITE;
/*!40000 ALTER TABLE `machine_ban` DISABLE KEYS */;
/*!40000 ALTER TABLE `machine_ban` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `password_resets`
--

DROP TABLE IF EXISTS `password_resets`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `password_resets` (
  `email` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `token` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `password_resets`
--

LOCK TABLES `password_resets` WRITE;
/*!40000 ALTER TABLE `password_resets` DISABLE KEYS */;
/*!40000 ALTER TABLE `password_resets` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `pets`
--

DROP TABLE IF EXISTS `pets`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `pets` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `index` tinyint(3) NOT NULL DEFAULT -1,
  `name` varchar(12) NOT NULL,
  `level` tinyint(3) NOT NULL DEFAULT 1,
  `closeness` smallint(6) NOT NULL DEFAULT 0,
  `fullness` tinyint(3) NOT NULL DEFAULT 100,
  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000,
  UNIQUE KEY `id` (`id`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pets`
--

LOCK TABLES `pets` WRITE;
/*!40000 ALTER TABLE `pets` DISABLE KEYS */;
/*!40000 ALTER TABLE `pets` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `rings`
--

DROP TABLE IF EXISTS `rings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `rings` (
  `ringid` int(10) NOT NULL,
  `itemid` int(15) NOT NULL,
  `charid` int(10) NOT NULL,
  `partnerid` int(10) NOT NULL,
  `equipped` int(1) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `rings`
--

LOCK TABLES `rings` WRITE;
/*!40000 ALTER TABLE `rings` DISABLE KEYS */;
/*!40000 ALTER TABLE `rings` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `server_evolutions_center`
--

DROP TABLE IF EXISTS `server_evolutions_center`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `server_evolutions_center` (
  `id` int(11) NOT NULL,
  `script_up` longtext NOT NULL,
  `script_down` longtext NOT NULL,
  `file_hash` varchar(128) NOT NULL,
  `apply_date` datetime NOT NULL,
  `state` varchar(45) NOT NULL,
  `last_error` longtext NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `server_evolutions_center`
--

LOCK TABLES `server_evolutions_center` WRITE;
/*!40000 ALTER TABLE `server_evolutions_center` DISABLE KEYS */;
INSERT INTO `server_evolutions_center` VALUES (1,'CREATE TABLE `servers` (\r\n   `configname` varchar(45) NOT NULL,\r\n   `world_id` tinyint(4) NOT NULL,\r\n   `private_ip` varchar(45) DEFAULT NULL,\r\n   PRIMARY KEY (`world_id`,`configname`)\r\n ) ENGINE=InnoDB DEFAULT CHARSET=latin1;','DROP TABLE IF EXISTS `servers`;','445e89e2742cc5369d4c5226f46ad496832defbc8b29a66e48febb4ad15703b3e87780747bca111d715d06488b4f9a9e580a13e9c19c6578b6a0879147b6c245','2017-08-21 00:49:56','ApplyingUp','System.Exception: [8/21/2017 12:49:56 AM][DB LIB] Got exception @ MySQL_Connection::RunQuery(CREATE TABLE `servers` (\r\n   `configname` varchar(45) NOT NULL,\r\n   `world_id` tinyint(4) NOT NULL,\r\n   `private_ip` varchar(45) DEFAULT NULL,\r\n   PRIMARY KEY (`world_id`,`configname`)\r\n ) ENGINE=InnoDB DEFAULT CHARSET=latin1;) : MySql.Data.MySqlClient.MySqlException (0x80004005): Table \'servers\' already exists\r\n   at MySql.Data.MySqlClient.MySqlStream.ReadPacket()\r\n   at MySql.Data.MySqlClient.NativeDriver.GetResult(Int32& affectedRow, Int64& insertedId)\r\n   at MySql.Data.MySqlClient.Driver.NextResult(Int32 statementId, Boolean force)\r\n   at MySql.Data.MySqlClient.MySqlDataReader.NextResult()\r\n   at MySql.Data.MySqlClient.MySqlCommand.ExecuteReader(CommandBehavior behavior)\r\n   at MySql.Data.MySqlClient.MySqlCommand.ExecuteNonQuery()\r\n   at WvsBeta.Database.MySQL_Connection.ExecuteAndReturnPossibleReader(String pQuery)\r\n   at WvsBeta.Database.MySQL_Connection.RunQuery(String pQuery)\r\n   at WvsBeta.Database.MySQL_Connection.RunQuery(String pQuery)\r\n   at WvsBeta.Database.DataBasePatcher.ApplyPatchRoute(PatchRouteElem[] routeElements)'),(2,'ALTER TABLE `characters` \r\nADD COLUMN `last_savepoint` DATETIME NULL DEFAULT \'2012-01-09 12:37:00\' AFTER `party`;','ALTER TABLE `characters` \r\nDROP COLUMN `last_savepoint`;','9bf083d723cdb7074b48ae62246882d77303afe9d149efd0317d05ef9a1eff3572ba6051192b2c0ff89b69443567330b6f104cbc1e249cceea1242c2b5cb6631','2017-09-03 20:56:23','AppliedUp',''),(3,'DROP TABLE IF EXISTS `cashitem_bundle`;\r\nDROP TABLE IF EXISTS `cashitem_eqp`;\r\nDROP TABLE IF EXISTS `cashitem_pet`;\r\nDROP TABLE IF EXISTS itemlocker;\r\nDROP TABLE IF EXISTS inventory_eqp;\r\nDROP TABLE IF EXISTS inventory_bundle;\r\nDROP TABLE IF EXISTS storage_eqp;\r\nDROP TABLE IF EXISTS storage_bundle;\r\n\r\n\r\nCREATE TABLE `cashitem_bundle` (\r\n  `userid` int(11) NOT NULL,\r\n  `itemid` int(11) NOT NULL,\r\n  `amount` smallint(6) NOT NULL DEFAULT \'1\',\r\n  `cashid` bigint(20) NOT NULL,\r\n  `expiration` bigint(20) NOT NULL DEFAULT \'150842304000000000\',\r\n  PRIMARY KEY (`cashid`)\r\n) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;\r\n\r\nCREATE TABLE `cashitem_eqp` (\r\n  `userid` int(11) NOT NULL,\r\n  `itemid` int(11) NOT NULL,\r\n  `slots` tinyint(4) NOT NULL DEFAULT 7,\r\n  `scrolls` tinyint(4) NOT NULL DEFAULT 0,\r\n  `istr` smallint(6) NOT NULL DEFAULT 0,\r\n  `idex` smallint(6) NOT NULL DEFAULT 0,\r\n  `iint` smallint(6) NOT NULL DEFAULT 0,\r\n  `iluk` smallint(6) NOT NULL DEFAULT 0,\r\n  `ihp` smallint(6) NOT NULL DEFAULT 0,\r\n  `imp` smallint(6) NOT NULL DEFAULT 0,\r\n  `iwatk` smallint(6) NOT NULL DEFAULT 0,\r\n  `imatk` smallint(6) NOT NULL DEFAULT 0,\r\n  `iwdef` smallint(6) NOT NULL DEFAULT 0,\r\n  `imdef` smallint(6) NOT NULL DEFAULT 0,\r\n  `iacc` smallint(6) NOT NULL DEFAULT 0,\r\n  `iavo` smallint(6) NOT NULL DEFAULT 0,\r\n  `ihand` smallint(6) NOT NULL DEFAULT 0,\r\n  `ispeed` smallint(6) NOT NULL DEFAULT 0,\r\n  `ijump` smallint(6) NOT NULL DEFAULT 0,\r\n  `cashid` bigint(20) NOT NULL,\r\n  `expiration` bigint(20) NOT NULL DEFAULT \'150842304000000000\',\r\n  PRIMARY KEY (`cashid`)\r\n) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;\r\n\r\nCREATE TABLE `cashitem_pet` (\r\n  `userid` int(11) NOT NULL,\r\n  `charid` int(11) NOT NULL,\r\n  `cashid` bigint(20) NOT NULL,\r\n  `itemid` int(11) NOT NULL,\r\n  `name` varchar(12) COLLATE latin1_general_ci NOT NULL,\r\n  `level` tinyint(3) NOT NULL,\r\n  `closeness` smallint(6) NOT NULL,\r\n  `fullness` tinyint(3) NOT NULL,\r\n  `expiration` bigint(20) NOT NULL,\r\n  `deaddate` bigint(20) NOT NULL,\r\n  PRIMARY KEY (`cashid`)\r\n) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;\r\n\r\nCREATE TABLE `itemlocker` (\r\n  `cashid` bigint(20) NOT NULL,\r\n  `slot` smallint(6) NOT NULL,\r\n  `userid` int(11) NOT NULL,\r\n  `characterid` int(11) NOT NULL,\r\n  `itemid` int(11) NOT NULL,\r\n  `commodity_id` int(11) NOT NULL,\r\n  `amount` smallint(6) NOT NULL,\r\n  `buycharactername` varchar(13) NOT NULL,\r\n  `expiration` bigint(20) NOT NULL,\r\n  `discount_rate` int(11) NOT NULL,\r\n  `worldid` tinyint(4) NOT NULL,\r\n  PRIMARY KEY (`cashid`)\r\n) ENGINE=InnoDB DEFAULT CHARSET=latin1;\r\n\r\n\r\nCREATE TABLE `inventory_eqp` (\r\n  `charid` int(11) NOT NULL,\r\n  `inv` tinyint(4) NOT NULL, -- Will be dropped\r\n  `slot` smallint(6) NOT NULL,\r\n  `itemid` int(11) NOT NULL,\r\n  `amount` smallint(11) NOT NULL DEFAULT 1, -- Will be dropped\r\n  `slots` tinyint(4) NOT NULL DEFAULT 7,\r\n  `scrolls` tinyint(4) NOT NULL DEFAULT 0,\r\n  `istr` smallint(6) NOT NULL DEFAULT 0,\r\n  `idex` smallint(6) NOT NULL DEFAULT 0,\r\n  `iint` smallint(6) NOT NULL DEFAULT 0,\r\n  `iluk` smallint(6) NOT NULL DEFAULT 0,\r\n  `ihp` smallint(6) NOT NULL DEFAULT 0,\r\n  `imp` smallint(6) NOT NULL DEFAULT 0,\r\n  `iwatk` smallint(6) NOT NULL DEFAULT 0,\r\n  `imatk` smallint(6) NOT NULL DEFAULT 0,\r\n  `iwdef` smallint(6) NOT NULL DEFAULT 0,\r\n  `imdef` smallint(6) NOT NULL DEFAULT 0,\r\n  `iacc` smallint(6) NOT NULL DEFAULT 0,\r\n  `iavo` smallint(6) NOT NULL DEFAULT 0,\r\n  `ihand` smallint(6) NOT NULL DEFAULT 0,\r\n  `ispeed` smallint(6) NOT NULL DEFAULT 0,\r\n  `ijump` smallint(6) NOT NULL DEFAULT 0,\r\n  `name` varchar(12) COLLATE latin1_general_ci NOT NULL,\r\n  `cashid` bigint(20) NOT NULL DEFAULT 0,\r\n  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000,\r\n  PRIMARY KEY (`charid`,`slot`),\r\n  KEY `charid` (`charid`)\r\n) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;\r\n\r\nCREATE TABLE `inventory_bundle` (\r\n  `charid` int(11) NOT NULL,\r\n  `inv` tinyint(4) NOT NULL,\r\n  `slot` smallint(6) NOT NULL,\r\n  `itemid` int(11) NOT NULL,\r\n  `amount` int(11) NOT NULL DEFAULT 1,\r\n  `cashid` bigint(20) NOT NULL DEFAULT 0,\r\n  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000,\r\n  PRIMARY KEY (`charid`,`inv`,`slot`),\r\n  KEY `charid` (`charid`)\r\n) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;\r\n\r\nINSERT INTO inventory_eqp SELECT * FROM items i WHERE i.inv = 1;\r\n\r\nINSERT INTO inventory_bundle SELECT i.charid, i.inv, i.slot, i.itemid, i.amount, i.cashid, i.expiration FROM items i WHERE i.inv <> 1;\r\n\r\n\r\n-- Storage\r\n\r\nCREATE TABLE `storage_eqp` (\r\n  `userid` int(11) NOT NULL,\r\n  `world_id` int(11) NOT NULL,\r\n  `slot` smallint(6) NOT NULL,\r\n  `itemid` int(11) NOT NULL,\r\n  `amount` smallint(11) NOT NULL DEFAULT 1, -- Will be dropped\r\n  `slots` tinyint(4) NOT NULL DEFAULT 7,\r\n  `scrolls` tinyint(4) NOT NULL DEFAULT 0,\r\n  `istr` smallint(6) NOT NULL DEFAULT 0,\r\n  `idex` smallint(6) NOT NULL DEFAULT 0,\r\n  `iint` smallint(6) NOT NULL DEFAULT 0,\r\n  `iluk` smallint(6) NOT NULL DEFAULT 0,\r\n  `ihp` smallint(6) NOT NULL DEFAULT 0,\r\n  `imp` smallint(6) NOT NULL DEFAULT 0,\r\n  `iwatk` smallint(6) NOT NULL DEFAULT 0,\r\n  `imatk` smallint(6) NOT NULL DEFAULT 0,\r\n  `iwdef` smallint(6) NOT NULL DEFAULT 0,\r\n  `imdef` smallint(6) NOT NULL DEFAULT 0,\r\n  `iacc` smallint(6) NOT NULL DEFAULT 0,\r\n  `iavo` smallint(6) NOT NULL DEFAULT 0,\r\n  `ihand` smallint(6) NOT NULL DEFAULT 0,\r\n  `ispeed` smallint(6) NOT NULL DEFAULT 0,\r\n  `ijump` smallint(6) NOT NULL DEFAULT 0,\r\n  `name` varchar(12) COLLATE latin1_general_ci NOT NULL,\r\n  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000,\r\n  KEY `userid_worldid` (`userid`, `world_id`)\r\n) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;\r\n\r\nCREATE TABLE `storage_bundle` (\r\n  `userid` int(11) NOT NULL,\r\n  `world_id` int(11) NOT NULL,\r\n  `inv` tinyint(4) NOT NULL,\r\n  `slot` smallint(6) NOT NULL,\r\n  `itemid` int(11) NOT NULL,\r\n  `amount` smallint(11) NOT NULL DEFAULT 1,\r\n  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000,\r\n  KEY `userid_worldid` (`userid`, `world_id`)\r\n) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;\r\n\r\nINSERT INTO storage_eqp SELECT * FROM storage_items i WHERE FLOOR(i.itemid / 1000000) = 1;\r\n\r\nINSERT INTO storage_bundle SELECT i.userid, i.world_id, FLOOR(i.itemid / 1000000), i.slot, i.itemid, i.amount, i.expiration FROM storage_items i WHERE FLOOR(i.itemid / 1000000) <> 1;\r\n\r\nALTER TABLE `storage_eqp` ADD COLUMN `cashid` BIGINT(20) NOT NULL AFTER `ijump`;\r\nALTER TABLE `storage_bundle` ADD COLUMN `cashid` BIGINT(20) NOT NULL AFTER `amount`;\r\n\r\nALTER TABLE inventory_eqp DROP COLUMN amount, DROP COLUMN inv, DROP COLUMN `name`;\r\nALTER TABLE storage_eqp DROP COLUMN amount, DROP COLUMN `name`;\r\n\r\n\r\nALTER TABLE characters ADD COLUMN pet_cash_id BIGINT(20) NOT NULL DEFAULT 0;','-- Put your down queries here, such as DROP TABLE, etc.','f821631845141590077076d338e469031e7d44426362946254768cad2ad1c5282d8cf596d204e302756d6a8c7d47d6a70140d90540933548d4cf4307c81f7029','2017-11-05 23:54:38','AppliedUp',''),(4,'ALTER TABLE itemlocker DROP COLUMN `discount_rate`, ADD COLUMN `gift_unread` tinyint NOT NULL default 0 AFTER `expiration`;','ALTER TABLE itemlocker DROP COLUMN `gift_unread`, ADD COLUMN `discount_rate` int(11) NOT NULL AFTER `expiration`;','cf9f478f05808f7405473ae9ff328c36af5f019afd331028fffb8c3848994c6ed874c219140d21ba8efb8158cfb49a3a2d842ef5e947e0899ec373d50c7107e1','2018-01-18 22:49:19','AppliedUp',''),(5,'ALTER TABLE cashitem_pet DROP COLUMN charid;','ALTER TABLE cashitem_pet ADD COLUMN charid INT(11) NOT NULL AFTER userid;','05945bf66c819a1d39f0be3420d248d87001ca0cec580b7e4d4a0958d1b732a87c3dfd12dcda19e7774a2b24ac9daaa79b64808cda4329675495250af9d8b2bb','2018-01-18 22:49:19','AppliedUp','');
/*!40000 ALTER TABLE `server_evolutions_center` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `server_evolutions_login`
--

DROP TABLE IF EXISTS `server_evolutions_login`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `server_evolutions_login` (
  `id` int(11) NOT NULL,
  `script_up` longtext NOT NULL,
  `script_down` longtext NOT NULL,
  `file_hash` varchar(128) NOT NULL,
  `apply_date` datetime NOT NULL,
  `state` varchar(45) NOT NULL,
  `last_error` longtext NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `server_evolutions_login`
--

LOCK TABLES `server_evolutions_login` WRITE;
/*!40000 ALTER TABLE `server_evolutions_login` DISABLE KEYS */;
INSERT INTO `server_evolutions_login` VALUES (1,'ALTER TABLE `machine_ban` \r\nCHANGE COLUMN `machineid` `machineid` VARCHAR(32) NOT NULL ,\r\nADD COLUMN `reason` TEXT NULL AFTER `last_try`;','ALTER TABLE `machine_ban` \r\nCHANGE COLUMN `machineid` `machineid` BIGINT(20) NOT NULL, DROP COLUMN `reason`;','666b174bb78b2ac4ce97cec1e806ad5485e4c9dfc67284c6ebb42fa19a26481b8f82cbcfb848fdbbb9e7cec72085ad57c40e03b2c5899750b3c5933d6e9f6885','2017-08-21 00:50:08','AppliedUp',''),(2,'ALTER TABLE `users` \r\nADD COLUMN `last_machine_id` VARCHAR(32) NULL AFTER `last_ip`;','ALTER TABLE `users` DROP COLUMN `last_machine_id`;','372e3c27fd475a1cf9e43758633555954feb07f57c1b4394e3f314b5cef6fff5f08904a819fc2ab16c2e5168b39fc98231f747df4544a902ca793e7812169e0c','2017-08-21 00:50:08','AppliedUp',''),(3,'ALTER TABLE users ADD COLUMN `confirmed_eula` TINYINT(1) NOT NULL DEFAULT \'0\';','ALTER TABLE users DROP COLUMN `confirmed_eula`;','4323b2f52a98e516b6d92bae148dcd593ddffc87264143edc86e5f053be7e2b18e5855f4878b69e9ef9b2ca68ed2886dc93540a3f1daa03a2c9c308f9b2461f0','2017-09-08 17:56:28','AppliedUp',''),(4,'ALTER TABLE users ADD last_unique_id VARCHAR(26) DEFAULT \'\';\r\nALTER TABLE users CHANGE donator donator TINYINT(1) DEFAULT 0;\r\n\r\nALTER TABLE machine_ban ADD last_unique_id VARCHAR(26) DEFAULT \'\';\r\n\r\nUPDATE users SET donator = 0;','ALTER TABLE users DROP last_unique_id;\r\nALTER TABLE users CHANGE donator donator INT(10) DEFAULT 1;\r\n\r\nALTER TABLE machine_ban DROP last_unique_id;\r\n\r\nUPDATE users SET donator = 1;','bee1054c551697b4d7e37727d8709944a302ce803c12ba51482a883c59a1c6cc6e2cb3f099c236c3eec0f36c47df827176025beb8683f55d93af4e5c2e56e9a7','2017-12-09 18:44:21','AppliedUp',''),(5,'ALTER TABLE users \r\nADD COLUMN max_unique_id_ban_count TINYINT(1) DEFAULT 5,\r\nADD COLUMN max_ip_ban_count TINYINT(1) DEFAULT 3,\r\nADD COLUMN banned_by VARCHAR(13) DEFAULT NULL AFTER ban_reason_message,\r\nADD COLUMN banned_at DATETIME DEFAULT NULL AFTER banned_by;','ALTER TABLE users\r\nDROP COLUMN max_unique_id_ban_count, \r\nDROP COLUMN max_ip_ban_count, \r\nDROP COLUMN banned_by, \r\nDROP COLUMN banned_at;','a7c9db699a68195e477082d57d4bf67cbb16d2f930a0b3e1f87b9a19ddefec4704675e3097f363a4a93bd910723c2d87f3ce13893cd01e30dc048be563418fe9','2018-01-18 22:53:02','AppliedUp',''),(6,'-- Put your up queries here, such as CREATE TABLE','-- Put your down queries here, such as DROP TABLE, etc.','c9c608ec4f1ac6075a26e11017823c7a80b9d2bf93b1854b0ae19e3bbb0dcd0e1e73e9508128cb27ec6fb24177f15ab824170c62714aff5a090513c6ff0b8019','2018-01-31 18:04:49','AppliedDown','');
/*!40000 ALTER TABLE `server_evolutions_login` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `servers`
--

DROP TABLE IF EXISTS `servers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `servers` (
  `configname` varchar(15) DEFAULT NULL,
  `world_id` tinyint(4) DEFAULT NULL,
  `private_ip` varchar(15) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `servers`
--

LOCK TABLES `servers` WRITE;
/*!40000 ALTER TABLE `servers` DISABLE KEYS */;
/*!40000 ALTER TABLE `servers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `skills`
--

DROP TABLE IF EXISTS `skills`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `skills` (
  `charid` int(11) NOT NULL,
  `skillid` int(11) NOT NULL,
  `points` smallint(6) NOT NULL DEFAULT 1,
  UNIQUE KEY `charid_2` (`charid`,`skillid`),
  KEY `charid` (`charid`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `skills`
--

LOCK TABLES `skills` WRITE;
/*!40000 ALTER TABLE `skills` DISABLE KEYS */;
/*!40000 ALTER TABLE `skills` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `storage`
--

DROP TABLE IF EXISTS `storage`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `storage` (
  `userid` int(11) NOT NULL,
  `world_id` int(11) NOT NULL,
  `slots` smallint(6) NOT NULL DEFAULT 4,
  `mesos` int(11) NOT NULL DEFAULT 0,
  `char_slots` int(11) NOT NULL DEFAULT 3,
  `credit_nx` int(11) NOT NULL DEFAULT 0,
  `maplepoints` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`userid`,`world_id`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `storage`
--

LOCK TABLES `storage` WRITE;
/*!40000 ALTER TABLE `storage` DISABLE KEYS */;
/*!40000 ALTER TABLE `storage` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `storage_bundle`
--

DROP TABLE IF EXISTS `storage_bundle`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `storage_bundle` (
  `userid` int(11) NOT NULL,
  `world_id` int(11) NOT NULL,
  `inv` tinyint(4) NOT NULL,
  `slot` smallint(6) NOT NULL,
  `itemid` int(11) NOT NULL,
  `amount` smallint(11) NOT NULL DEFAULT 1,
  `cashid` bigint(20) NOT NULL,
  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000,
  KEY `userid_worldid` (`userid`,`world_id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `storage_bundle`
--

LOCK TABLES `storage_bundle` WRITE;
/*!40000 ALTER TABLE `storage_bundle` DISABLE KEYS */;
/*!40000 ALTER TABLE `storage_bundle` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `storage_cash`
--

DROP TABLE IF EXISTS `storage_cash`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `storage_cash` (
  `id` int(11) unsigned NOT NULL AUTO_INCREMENT,
  `userid` int(11) NOT NULL,
  `world_id` int(11) NOT NULL,
  `bought_userid` int(11) NOT NULL,
  `sn` int(11) NOT NULL,
  `itemid` int(11) NOT NULL,
  `amount` int(3) NOT NULL DEFAULT 1,
  `from` varchar(13) NOT NULL DEFAULT '',
  `expires` bigint(20) NOT NULL DEFAULT 150842304000000000,
  PRIMARY KEY (`id`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `storage_cash`
--

LOCK TABLES `storage_cash` WRITE;
/*!40000 ALTER TABLE `storage_cash` DISABLE KEYS */;
/*!40000 ALTER TABLE `storage_cash` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `storage_cashshop`
--

DROP TABLE IF EXISTS `storage_cashshop`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `storage_cashshop` (
  `userid` int(11) NOT NULL,
  `world_id` int(2) NOT NULL,
  `itemid` int(11) NOT NULL,
  `amount` int(11) NOT NULL DEFAULT 1,
  `slots` tinyint(4) DEFAULT 7,
  `scrolls` smallint(6) DEFAULT 0,
  `istr` smallint(6) DEFAULT 0,
  `idex` smallint(6) DEFAULT 0,
  `iint` smallint(6) DEFAULT 0,
  `iluk` smallint(6) DEFAULT 0,
  `ihp` smallint(6) DEFAULT 0,
  `imp` smallint(6) DEFAULT 0,
  `iwatk` smallint(6) DEFAULT 0,
  `imatk` smallint(6) DEFAULT 0,
  `iwdef` smallint(6) DEFAULT 0,
  `imdef` smallint(6) DEFAULT 0,
  `iacc` smallint(6) DEFAULT 0,
  `iavo` smallint(6) DEFAULT 0,
  `ihand` smallint(6) DEFAULT 0,
  `ispeed` smallint(6) DEFAULT 0,
  `ijump` smallint(6) DEFAULT 0,
  `name` varchar(12) COLLATE latin1_general_ci NOT NULL,
  `cashid` int(11) NOT NULL DEFAULT 0,
  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000
) ENGINE=MyISAM DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `storage_cashshop`
--

LOCK TABLES `storage_cashshop` WRITE;
/*!40000 ALTER TABLE `storage_cashshop` DISABLE KEYS */;
/*!40000 ALTER TABLE `storage_cashshop` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `storage_eqp`
--

DROP TABLE IF EXISTS `storage_eqp`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `storage_eqp` (
  `userid` int(11) NOT NULL,
  `world_id` int(11) NOT NULL,
  `slot` smallint(6) NOT NULL,
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
  `expiration` bigint(20) NOT NULL DEFAULT 150842304000000000,
  KEY `userid_worldid` (`userid`,`world_id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `storage_eqp`
--

LOCK TABLES `storage_eqp` WRITE;
/*!40000 ALTER TABLE `storage_eqp` DISABLE KEYS */;
/*!40000 ALTER TABLE `storage_eqp` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `storage_items`
--

DROP TABLE IF EXISTS `storage_items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `storage_items` (
  `userid` int(11) NOT NULL,
  `world_id` int(11) NOT NULL,
  `slot` smallint(6) NOT NULL,
  `itemid` int(11) NOT NULL,
  `amount` int(11) NOT NULL DEFAULT 1,
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
  `name` varchar(12) NOT NULL,
  `expiration` bigint(18) NOT NULL DEFAULT 150842304000000000,
  PRIMARY KEY (`userid`,`world_id`,`slot`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `storage_items`
--

LOCK TABLES `storage_items` WRITE;
/*!40000 ALTER TABLE `storage_items` DISABLE KEYS */;
/*!40000 ALTER TABLE `storage_items` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `teleport_rock_locations`
--

DROP TABLE IF EXISTS `teleport_rock_locations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `teleport_rock_locations` (
  `charid` int(11) NOT NULL,
  `mapindex` tinyint(3) NOT NULL,
  `mapid` int(11) NOT NULL DEFAULT 999999999,
  PRIMARY KEY (`charid`,`mapindex`),
  KEY `charid` (`charid`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `teleport_rock_locations`
--

LOCK TABLES `teleport_rock_locations` WRITE;
/*!40000 ALTER TABLE `teleport_rock_locations` DISABLE KEYS */;
/*!40000 ALTER TABLE `teleport_rock_locations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_point_transactions`
--

DROP TABLE IF EXISTS `user_point_transactions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `user_point_transactions` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `userid` int(11) NOT NULL,
  `amount` mediumint(9) NOT NULL,
  `date` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `note` text NOT NULL,
  `pointtype` enum('maplepoints','nx') NOT NULL,
  PRIMARY KEY (`id`),
  KEY `uid_pointtype` (`userid`,`pointtype`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_point_transactions`
--

LOCK TABLES `user_point_transactions` WRITE;
/*!40000 ALTER TABLE `user_point_transactions` DISABLE KEYS */;
/*!40000 ALTER TABLE `user_point_transactions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `users` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(20) COLLATE latin1_general_ci NOT NULL,
  `password` char(130) COLLATE latin1_general_ci NOT NULL,
  `salt` char(10) COLLATE latin1_general_ci DEFAULT NULL,
  `email` varchar(255) COLLATE latin1_general_ci NOT NULL,
  `pin` int(4) unsigned DEFAULT NULL,
  `gender` tinyint(1) unsigned DEFAULT NULL,
  `gm` tinyint(4) NOT NULL DEFAULT 0,
  `admin` tinyint(1) NOT NULL DEFAULT 0,
  `char_delete_password` int(8) unsigned NOT NULL DEFAULT 11111111,
  `online` int(5) NOT NULL DEFAULT 0,
  `ban_expire` datetime NOT NULL DEFAULT '2012-01-09 12:37:00',
  `ban_reason` tinyint(2) unsigned NOT NULL DEFAULT 0,
  `ban_reason_message` varchar(255) COLLATE latin1_general_ci NOT NULL DEFAULT '',
  `banned_by` varchar(13) COLLATE latin1_general_ci DEFAULT NULL,
  `banned_at` datetime DEFAULT NULL,
  `last_ip` varchar(45) COLLATE latin1_general_ci DEFAULT NULL,
  `last_machine_id` varchar(32) COLLATE latin1_general_ci DEFAULT NULL,
  `last_login` datetime NOT NULL DEFAULT '2012-01-09 12:37:00',
  `quiet_ban_expire` datetime NOT NULL DEFAULT '2012-01-09 12:37:00',
  `quiet_ban_reason` tinyint(3) NOT NULL DEFAULT 0,
  `creation_date` datetime NOT NULL DEFAULT '2012-01-09 12:37:00',
  `donator` tinyint(1) DEFAULT 0,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `remember_token` varchar(255) COLLATE latin1_general_ci NOT NULL DEFAULT '',
  `superadmin` tinyint(1) NOT NULL DEFAULT 0,
  `beta` tinyint(1) NOT NULL DEFAULT 0,
  `confirmed_eula` tinyint(1) NOT NULL DEFAULT 0,
  `web_admin` tinyint(4) NOT NULL DEFAULT 0,
  `last_unique_id` varchar(26) COLLATE latin1_general_ci DEFAULT NULL,
  `max_unique_id_ban_count` tinyint(1) DEFAULT 5,
  `max_ip_ban_count` tinyint(1) DEFAULT 3,
  PRIMARY KEY (`ID`),
  KEY `username` (`username`),
  KEY `online` (`online`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping events for database 'wvsbeta'
--

--
-- Dumping routines for database 'wvsbeta'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2018-10-06 15:49:38
