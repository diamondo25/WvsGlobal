-- Fix machine ID banning
------- UP -------

ALTER TABLE `machine_ban` 
CHANGE COLUMN `machineid` `machineid` VARCHAR(32) NOT NULL ,
ADD COLUMN `reason` TEXT NULL AFTER `last_try`;

------- DOWN -------

ALTER TABLE `machine_ban` 
CHANGE COLUMN `machineid` `machineid` BIGINT(20) NOT NULL, DROP COLUMN `reason`;
